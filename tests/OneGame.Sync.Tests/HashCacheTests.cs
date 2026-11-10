namespace OneGame.Sync.Tests;

/// <summary>The per-entity hash cache must never change a hash: every number equals what a fresh copy of the same state computes.</summary>
public class HashCacheTests
{
    private static EntityRegistry Registry()
    {
        var registry = Arena.Registry();
        registry.Add(new Marker().TypeId, "Marker", static () => new Marker());
        return registry;
    }

    private static World Fresh(World source, WorldRole role)
    {
        var w = new SyncWriter(4096);
        source.WriteSnapshot(w);
        var copy = new World(Arena.Seed, Registry(), role);
        copy.AddTable(new SecretRowTable());
        Assert.Equal(SyncError.None, copy.ReadSnapshot(w.Written));
        return copy;
    }

    private static readonly Audience[] Audiences = [new(1, 10), new(2, 20), new(3, 10), new(0, 0), new(2, 10)];

    private static void AssertSame(World cached, World fresh, InterestSet interest, string step)
    {
        Assert.True(cached.ComputeHash() == fresh.ComputeHash(), "world hash at " + step);
        foreach (var who in Audiences)
        {
            foreach (var include in new[] { true, false })
            {
                Assert.True(cached.ComputeAudienceHash(who, include) == fresh.ComputeAudienceHash(who, include), $"audience {who} at {step}");
                Assert.True(cached.ComputeAudienceHash(who, include, interest) == fresh.ComputeAudienceHash(who, include, interest), $"interest audience {who} at {step}");
            }

            var a = new StateDigest();
            var b = new StateDigest();
            var scratch = new SyncWriter(256);
            StateDigest.CaptureView(cached, a, scratch, 100_000, who, true, interest);
            StateDigest.CaptureView(fresh, b, scratch, 100_000, who, true, interest);
            Assert.Equal(b.WorldHash, a.WorldHash);
            Assert.Equal(b.Entries.Count, a.Entries.Count);
            for (var i = 0; i < a.Entries.Count; i++)
            {
                Assert.Equal(b.Entries[i], a.Entries[i]);
            }
        }
    }

    [Fact]
    public void CachedHashesEqualFreshHashesOverRandomMutations()
    {
        var world = Arena.Match();
        for (uint c = 1; c <= 6; c++)
        {
            world.Add(new Marker { Cell = c, Tag = "m" + c });
        }

        var interest = new InterestSet();
        interest.Replace([1, 3, 5]);
        var rng = new Random(11);
        var replica = Fresh(world, WorldRole.Replica);
        for (var step = 0; step < 400; step++)
        {
            var entities = world.Entities.ToArray();
            var e = entities[rng.Next(entities.Length)];
            switch (rng.Next(9))
            {
                case 0 when e is Hero h: h.Gold += rng.Next(5); break;
                case 1 when e is Hero h: h.Position += new FVec2(rng.Next(3), 1); break;
                case 2 when e is Hero h: h.Hand.Add(rng.Next(52)); break;
                case 3 when e is Hero h && h.Hand.Count > 0: h.Hand.RemoveAt(0); break;
                case 4 when e is Hero h: h.Team = (uint)(10 * (1 + rng.Next(2))); break; // changes who may see it
                case 5 when e is Marker m: m.Cell = (uint)rng.Next(7); break;
                case 6 when e is Marker m: m.Tag = "t" + rng.Next(4); break;
                case 7: world.Step(); break;
                case 8: world.Add(new Marker { Cell = (uint)rng.Next(7), Tag = "n" }); break;
            }

            if (step % 3 == 0)
            {
                // Warm every cache first, then compare against a copy that has never hashed anything.
                world.ComputeHash();
                AssertSame(world, Fresh(world, WorldRole.Authority), interest, "step " + step);
            }

            if (step % 50 == 25)
            {
                interest.Replace([(uint)rng.Next(1, 7), (uint)rng.Next(1, 7)]);
            }
        }

        // ReadState and ApplyDelta on a replica whose caches are warm.
        var writer = new SyncWriter(1024);
        foreach (var e in world.Entities.ToArray())
        {
            var twin = replica.Find(e.Id);
            if (twin is null)
            {
                continue;
            }

            Assert.NotEqual(0UL, twin.ComputeHash());
            writer.Reset();
            e.WriteState(writer);
            var r = new SyncReader(writer.Written);
            twin.ReadState(ref r);
            Assert.Equal(e.ComputeHash(), twin.ComputeHash());
        }

        var hero = world.Entities.OfType<Hero>().First();
        var twinHero = (Hero)replica.Find(hero.Id)!;
        twinHero.ComputeHash();
        world.ComputeHash();
        hero.ClearDirty();
        hero.Gold += 7;
        hero.Hand.Add(9);
        writer.Reset();
        hero.WriteDirtyDelta(writer);
        var rd = new SyncReader(writer.Written);
        twinHero.ApplyDelta(ref rd);
        Assert.Equal(hero.ComputeHash(), twinHero.ComputeHash());
    }
}

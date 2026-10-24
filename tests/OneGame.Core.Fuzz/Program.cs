using System.Diagnostics;
using System.Text;
using OneGame.Core.Tests.Fuzzing;

// A seeded, time-boxed mutation fuzzer for the decoders that read untrusted bytes. It has no coverage feedback: it mutates
// real encoded inputs (snapshots, entity state, deltas, commands, tables) and checks that every decoder returns, stays within an
// allocation budget and never throws. Findings are written as files so they can be committed as regression inputs.
//
//   dotnet run -c Release --project tests/OneGame.Core.Fuzz -- --seconds 300 --seed 1 --out <dir> [--target name] [--seeds <dir>]

var seconds = 60;
ulong seed = 1;
string? outDir = null;
string? only = null;
string? writeSeeds = null;
for (var i = 0; i < args.Length - 1; i++)
{
    switch (args[i])
    {
        case "--seconds": seconds = int.Parse(args[++i], System.Globalization.CultureInfo.InvariantCulture); break;
        case "--seed": seed = ulong.Parse(args[++i], System.Globalization.CultureInfo.InvariantCulture); break;
        case "--out": outDir = args[++i]; break;
        case "--target": only = args[++i]; break;
        case "--write-seeds": writeSeeds = args[++i]; break;
    }
}

if (writeSeeds is not null)
{
    foreach (var t in FuzzTargets.All)
    {
        var n = 0;
        foreach (var s in t.Seeds())
        {
            var dir = Path.Combine(writeSeeds, t.Name);
            Directory.CreateDirectory(dir);
            File.WriteAllBytes(Path.Combine(dir, $"seed-{n++}.bin"), s);
        }
    }

    return 0;
}

var targets = FuzzTargets.All.Where(t => only is null || t.Name == only).ToList();
var seeds = targets.ToDictionary(t => t.Name, t => t.Seeds().ToList());
var rng = new Random(unchecked((int)seed));
var stats = targets.ToDictionary(t => t.Name, _ => new long[3]); // executions, accepted, findings
var seen = new HashSet<string>();
var findings = new List<string>();
var clock = Stopwatch.StartNew();
var iteration = 0L;

while (clock.Elapsed.TotalSeconds < seconds)
{
    var target = targets[(int)(iteration++ % targets.Count)];
    var pool = seeds[target.Name];
    var input = Mutate(pool[rng.Next(pool.Count)], pool, rng);
    var before = GC.GetAllocatedBytesForCurrentThread();
    var started = Stopwatch.GetTimestamp();
    string? problem = null;
    bool ok = false;
    try
    {
        ok = target.Run(input);
    }
    catch (Exception e)
    {
        problem = $"exception {e.GetType().Name}: {e.Message} at {e.StackTrace?.Split('\n').FirstOrDefault()?.Trim()}";
    }

    var allocated = GC.GetAllocatedBytesForCurrentThread() - before;
    var elapsedMs = Stopwatch.GetElapsedTime(started).TotalMilliseconds;
    if (problem is null && elapsedMs > 1000)
    {
        problem = $"slow: {elapsedMs:F0} ms";
    }

    // Decoding may build objects proportional to the input, never far beyond it.
    if (problem is null && allocated > (200L * input.Length) + (4L << 20))
    {
        problem = $"allocation: {allocated} bytes for {input.Length} input bytes";
    }

    var s = stats[target.Name];
    s[0]++;
    if (ok)
    {
        s[1]++;
    }

    if (problem is not null)
    {
        s[2]++;
        var key = target.Name + "|" + problem[..Math.Min(problem.Length, 120)];
        if (seen.Add(key))
        {
            var name = $"{target.Name}-{FuzzTargets.Digest(input)}.bin";
            findings.Add($"{name}: {problem}");
            if (outDir is not null)
            {
                Directory.CreateDirectory(outDir);
                File.WriteAllBytes(Path.Combine(outDir, name), input);
            }
        }
    }
}

var json = new StringBuilder();
json.Append("{\"seconds\":").Append(seconds).Append(",\"seed\":").Append(seed).Append(",\"iterations\":").Append(iteration).Append(",\"targets\":{");
json.Append(string.Join(",", targets.Select(t => $"\"{t.Name}\":{{\"executions\":{stats[t.Name][0]},\"accepted\":{stats[t.Name][1]},\"findings\":{stats[t.Name][2]}}}")));
json.Append("},\"distinctFindings\":[").Append(string.Join(",", findings.Select(f => "\"" + f.Replace("\\", "\\\\").Replace("\"", "\\\"") + "\""))).Append("]}");
Console.WriteLine(json);
return findings.Count == 0 ? 0 : 2;

static byte[] Mutate(byte[] original, List<byte[]> pool, Random rng)
{
    var data = new List<byte>(original);
    byte[] interesting = [0x00, 0x01, 0x7F, 0x80, 0xFF, 0xFE, 0x40, 0x02];
    for (var round = 1 + rng.Next(6); round > 0; round--)
    {
        if (data.Count == 0)
        {
            data.Add((byte)rng.Next(256));
            continue;
        }

        var at = rng.Next(data.Count);
        switch (rng.Next(9))
        {
            case 0: data[at] ^= (byte)(1 << rng.Next(8)); break;
            case 1: data[at] = interesting[rng.Next(interesting.Length)]; break;
            case 2: data[at] = (byte)rng.Next(256); break;
            case 3: data.RemoveRange(at, Math.Min(data.Count - at, 1 + rng.Next(8))); break;
            case 4: data.Insert(at, (byte)rng.Next(256)); break;
            case 5: data.RemoveRange(at, data.Count - at); break; // truncate
            case 6: // a run of continuation bytes: an over-long or over-large varint
                data.InsertRange(at, Enumerable.Repeat((byte)0xFF, 1 + rng.Next(11)));
                break;
            case 7: // splice a piece of another input
                var other = pool[rng.Next(pool.Count)];
                if (other.Length > 0)
                {
                    var from = rng.Next(other.Length);
                    data.InsertRange(at, other.Skip(from).Take(1 + rng.Next(24)));
                }

                break;
            default: // overwrite with a huge length-like value
                data.RemoveRange(at, Math.Min(data.Count - at, 3));
                data.InsertRange(at, [(byte)0xFF, 0xFF, 0x7F]);
                break;
        }
    }

    return [.. data];
}

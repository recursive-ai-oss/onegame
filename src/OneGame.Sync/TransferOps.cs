namespace OneGame.Sync;

/// <summary>
/// The changes a transfer makes to a world outside a tick: taking entities out into the log, putting them back, installing a bundle,
/// and moving log records along. Both the live scope and <see cref="Replayer"/> run them through <see cref="Apply"/>, so a recording
/// that logged them replays to the same hashes. They are logged as commands with entity id 0.
/// </summary>
internal static class TransferOps
{
    public const uint Begin = 0xFFFF_FF01;
    public const uint Commit = 0xFFFF_FF02;
    public const uint Abort = 0xFFFF_FF03;
    public const uint Done = 0xFFFF_FF04;
    public const uint Install = 0xFFFF_FF05;

    public static bool IsOp(ulong entity, uint commandId) => entity == 0 && commandId is >= Begin and <= Install;

    // ---- bundles --------------------------------------------------------------------------------------------------

    /// <summary>Serializes entities: count, then for each the type, the id it has now, its schema version and its state.</summary>
    public static byte[] WriteBundle(IReadOnlyList<Entity> entities)
    {
        var w = new SyncWriter(1024);
        var scratch = new SyncWriter(256);
        w.WriteVarUInt((ulong)entities.Count);
        foreach (var e in entities)
        {
            scratch.Reset();
            e.WriteState(scratch);
            w.WriteFixed32(e.TypeId);
            w.WriteVarUInt(e.Id.Value);
            w.WriteVarUInt(e.SchemaVersion);
            w.WriteBytes(scratch.Written);
        }

        return w.ToArray();
    }

    /// <summary>Decodes a bundle from untrusted bytes into detached entities, checking every limit first. Nothing is added to a world.</summary>
    public static SyncError ReadBundle(World world, ReadOnlySpan<byte> bytes, List<Entity> entities, List<ulong> ids)
    {
        if (bytes.Length > TransferLimits.MaxBundleBytes)
        {
            return SyncError.LimitExceeded;
        }

        var r = new SyncReader(bytes);
        var count = r.ReadCount(TransferLimits.MaxEntities);
        for (var i = 0; i < count && r.Ok; i++)
        {
            var typeId = r.ReadFixed32();
            var id = r.ReadVarUInt();
            var schema = r.ReadVarUInt();
            var body = r.ReadBytes(int.MaxValue);
            if (!r.Ok)
            {
                break;
            }

            if (id == 0 || schema > uint.MaxValue)
            {
                r.Fail(SyncError.InvalidValue);
                break;
            }

            var entity = world.Registry.Create(typeId);
            if (entity is null)
            {
                r.Fail(SyncError.UnknownType);
                break;
            }

            var sub = new SyncReader(body);
            entity.ReadState((uint)schema, ref sub);
            sub.ExpectEnd();
            if (!sub.Ok)
            {
                r.Fail(sub.Error);
                break;
            }

            entity.ClearDirty();
            entities.Add(entity);
            ids.Add(id);
        }

        r.ExpectEnd();
        return r.Ok ? SyncError.None : r.Error;
    }

    // ---- operations -----------------------------------------------------------------------------------------------

    /// <summary>Payload of <see cref="Begin"/>: transfer id, destination scope, the ids to take out.</summary>
    public static byte[] BeginPayload(ulong id, uint dest, IReadOnlyList<ulong> ids)
    {
        var w = new SyncWriter(64);
        w.WriteVarUInt(id);
        w.WriteVarUInt(dest);
        w.WriteVarUInt((ulong)ids.Count);
        foreach (var e in ids)
        {
            w.WriteVarUInt(e);
        }

        return w.ToArray();
    }

    public static byte[] IdPayload(ulong id)
    {
        var w = new SyncWriter(16);
        w.WriteVarUInt(id);
        return w.ToArray();
    }

    public static byte[] InstallPayload(ulong id, ReadOnlySpan<byte> bundle)
    {
        var w = new SyncWriter(bundle.Length + 16);
        w.WriteVarUInt(id);
        w.WriteBytes(bundle);
        return w.ToArray();
    }

    /// <summary>Applies one operation to a world. Returns false when it changed nothing (no log, unknown transfer, already installed).</summary>
    /// <param name="world">An authority world.</param>
    /// <param name="op">One of the operation ids.</param>
    /// <param name="payload">The operation's payload.</param>
    /// <param name="newIds">For <see cref="Install"/>, receives the ids the entities got.</param>
    public static bool Apply(World world, uint op, ReadOnlySpan<byte> payload, List<ulong>? newIds = null)
    {
        var log = TransferLog.Of(world);
        if (log is null)
        {
            return false;
        }

        var r = new SyncReader(payload);
        var tid = r.ReadVarUInt();
        if (!r.Ok)
        {
            return false;
        }

        switch (op)
        {
            case Begin:
            {
                var dest = r.ReadVarUInt();
                var count = r.ReadCount(TransferLimits.MaxEntities);
                var ids = new ulong[count];
                for (var i = 0; i < count && r.Ok; i++)
                {
                    ids[i] = r.ReadVarUInt();
                }

                if (!r.Ok || dest > uint.MaxValue || log.FindOpen(tid) is not null || log.OpenCount >= TransferLimits.MaxOpen)
                {
                    return false;
                }

                var list = new List<Entity>(count);
                foreach (var id in ids)
                {
                    if (world.Find(new EntityId(id)) is not { } e)
                    {
                        return false;
                    }

                    list.Add(e);
                }

                var bundle = WriteBundle(list);
                foreach (var id in ids)
                {
                    world.Remove(new EntityId(id));
                }

                log.AddOpen(new TransferLog.OutRecord(tid, (uint)dest, false, world.Tick, ids, bundle));
                return true;
            }

            case Commit:
            {
                if (log.FindOpen(tid) is not { } record)
                {
                    return false;
                }

                record.Committed = true;
                return true;
            }

            case Abort:
            {
                if (log.FindOpen(tid) is not { } record)
                {
                    return false;
                }

                var entities = new List<Entity>();
                var ids = new List<ulong>();
                if (ReadBundle(world, record.Bundle, entities, ids) == SyncError.None)
                {
                    for (var i = 0; i < entities.Count; i++)
                    {
                        try
                        {
                            world.AddWithId(entities[i], new EntityId(ids[i]));
                        }
                        catch (InvalidOperationException)
                        {
                            // The id was taken meanwhile; ids are never reused, so this means the world was rebuilt. Skip it.
                        }
                    }
                }

                log.RemoveOpen(tid);
                return true;
            }

            case Done:
                return log.RemoveOpen(tid);

            case Install:
            {
                var bundle = r.ReadBytes(TransferLimits.MaxBundleBytes);
                if (!r.Ok || log.IsDone(tid))
                {
                    return false;
                }

                var entities = new List<Entity>();
                var ids = new List<ulong>();
                if (ReadBundle(world, bundle, entities, ids) != SyncError.None)
                {
                    return false;
                }

                var fresh = new ulong[entities.Count];
                for (var i = 0; i < entities.Count; i++)
                {
                    world.Add(entities[i]);
                    fresh[i] = entities[i].Id.Value;
                }

                log.AddDone(new TransferLog.DoneRecord(tid, fresh));
                newIds?.AddRange(fresh);
                return true;
            }

            default:
                return false;
        }
    }
}

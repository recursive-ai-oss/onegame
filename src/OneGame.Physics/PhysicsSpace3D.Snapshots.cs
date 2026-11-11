using RecursiveAI.OnePhysics;
using RecursiveAI.OnePhysics.Physics3D;

namespace OneGame.Physics;

public sealed partial class PhysicsSpace3D
{
    /// <inheritdoc />
    public string Name => _name;
    /// <inheritdoc />
    public int Count => BodyCount;
    /// <inheritdoc />
    public Visibility Visibility => Visibility.Hidden;
    /// <inheritdoc />
    public void Clear() { _world = new(_settings); _warm = false; }

    private void EnsureScratch()
    {
        var size = _world.GetSnapshotSizeBound();
        if (size > MaxSnapshotBytes) throw new InvalidOperationException("Physics snapshot exceeds the 64 MiB table limit.");
        if (_scratch.Length < size) _scratch = new byte[size];
    }
    private ReadOnlySpan<byte> Saved()
    {
        EnsureScratch();
        if (!_world.TrySnapshot(_scratch, out var written)) throw new InvalidOperationException("Physics snapshot did not fit its advertised bound.");
        return _scratch.AsSpan(0, written);
    }
    /// <inheritdoc />
    public void Write(SyncWriter writer)
    {
        ArgumentNullException.ThrowIfNull(writer);
        writer.WriteByte(1); writer.WriteInt32(_rate);
        var snapshot = Saved();
        writer.Reserve(_scratch.Length + 16);
        writer.WriteBytes(snapshot);
    }
    /// <inheritdoc />
    public void Read(ref SyncReader reader)
    {
        var version = reader.ReadByte(); var rate = reader.ReadInt32(); var bytes = reader.ReadBytes(MaxSnapshotBytes);
        if (!reader.Ok) return;
        if (version != 1) { reader.Fail(SyncError.UnsupportedVersion); return; }
        if (rate != _rate) { reader.Fail(SyncError.Mismatch); return; }
        try { _world.Restore(bytes); _warm = false; }
        catch (PhysicsException) { reader.Fail(SyncError.InvalidValue); }
        catch (ArgumentException) { reader.Fail(SyncError.InvalidValue); }
    }
    /// <inheritdoc />
    public void CopyFrom(IHotTable source)
    {
        if (source is not PhysicsSpace3D other || other._rate != _rate) throw new ArgumentException("Copy requires a 3D space at the same tick rate.", nameof(source));
        _world.Restore(other.Saved()); _warm = false;
    }
    /// <inheritdoc />
    public void WriteJson(JsonOut json)
    {
        ArgumentNullException.ThrowIfNull(json);
        json.BeginObject(); json.Name("steps"); json.Value(StepCount); json.Name("hash"); json.Value(Hash);
        json.Name("origin"); Origin.WriteJson(json); json.Name("bodies"); json.BeginArray();
        foreach (var body in _world.Bodies)
        {
            json.BeginObject(); json.Name("ref"); Ref(body.RawHandle).WriteJson(json);
            json.Name("position"); From(body.Position).WriteJson(json); json.Name("velocity"); From(body.LinearVelocity).WriteJson(json); json.EndObject();
        }
        json.EndArray(); json.EndObject();
    }

    /// <summary>Captures an independent full physics snapshot (not the table envelope).</summary>
    public byte[] Snapshot() => Saved().ToArray();
    /// <summary>Restores a full physics snapshot; invalid input leaves the space unchanged.</summary>
    [MutatesState]
    public void Restore(ReadOnlySpan<byte> snapshot)
    {
        CheckSize(snapshot); _world.Restore(snapshot); _warm = false;
    }
    /// <summary>Creates a delta against a full physics snapshot. Tooling operation; allocates.</summary>
    public byte[] SnapshotDelta(ReadOnlySpan<byte> baseline) { CheckSize(baseline); return _world.SnapshotDelta(baseline); }
    /// <summary>Restores a delta with a 64 MiB decoded size limit.</summary>
    [MutatesState]
    public void RestoreDelta(ReadOnlySpan<byte> baseline, ReadOnlySpan<byte> delta)
    {
        CheckSize(baseline); CheckSize(delta); _world.RestoreDelta(baseline, delta, MaxSnapshotBytes); _warm = false;
    }
    /// <summary>Creates a compressed full physics snapshot. Tooling operation; allocates.</summary>
    public byte[] SnapshotCompressed() => _world.SnapshotCompressed();
    /// <summary>Restores a compressed snapshot with a 64 MiB decoded size limit.</summary>
    [MutatesState]
    public void RestoreCompressed(ReadOnlySpan<byte> compressed)
    {
        CheckSize(compressed); _world.RestoreCompressed(compressed, MaxSnapshotBytes); _warm = false;
    }
    /// <summary>Captures bodies in inclusive local bounds and their constraint closure. Tooling operation; allocates.</summary>
    public byte[] SnapshotRegion(FVec3 minimum, FVec3 maximum) => _world.SnapshotRegion(Point(minimum), Point(maximum));
    /// <summary>Restores a region at the same tick, origin and topology. Invalid input leaves the space unchanged.</summary>
    [MutatesState]
    public void RestoreRegion(ReadOnlySpan<byte> region)
    {
        CheckSize(region); _world.RestoreRegion(region); _warm = false;
    }
    /// <summary>Discards broadphase history in canonical shape order. Call between steps on both peers after a region repair before comparing full hashes. Allocates tooling buffers.</summary>
    [MutatesState]
    public void CanonicalizeBroadphase() => _world.CanonicalizeBroadphase();

    /// <summary>Returns a named SnapshotDiffReport summary for drift/replay tooling, including body slots, generations and fields.</summary>
    public string FindDifferences(PhysicsSpace3D other)
    {
        ArgumentNullException.ThrowIfNull(other); return _world.FindDifferences(other._world).ToString();
    }
    /// <summary>Returns named differences against a physics snapshot; invalid data produces an unavailable message.</summary>
    public string DescribeDifferences(ReadOnlySpan<byte> expected)
    {
        CheckSize(expected);
        try { return SnapshotDesync.Compare(expected, Saved()).ToString(); }
        catch (PhysicsException) { return "Physics diagnostic snapshot is invalid or incompatible."; }
        catch (ArgumentException) { return "Physics diagnostic snapshot is invalid or incompatible."; }
    }
    private static void CheckSize(ReadOnlySpan<byte> data)
    {
        if (data.Length > MaxSnapshotBytes) throw new ArgumentOutOfRangeException(nameof(data), "Physics data exceeds 64 MiB.");
    }
}

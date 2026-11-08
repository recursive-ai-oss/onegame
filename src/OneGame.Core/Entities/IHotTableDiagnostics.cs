namespace OneGame.Core;

/// <summary>Optional tooling diagnostics for a table whose binary state has named fields. Not used by the tick or replica wire protocol.</summary>
public interface IHotTableDiagnostics
{
    /// <summary>Captures an independent diagnostic snapshot for a recording checkpoint. May allocate; callers opt in explicitly.</summary>
    byte[] CaptureDiagnosticSnapshot();

    /// <summary>Describes differences from a captured snapshot, naming rows and fields. The snapshot must come from the same table implementation.</summary>
    string DescribeDifferences(ReadOnlySpan<byte> expected);
}

namespace OneGame.Control;

public static partial class ControlOps
{
    private static ControlOp[] AssetOps =>
    [
        new("assets",
            "The files the game loaded through its asset service, and what happened to them, as JSON: assets [{path, kind, version, references, state, digest, file, error}], events [{sequence, type, path, kind, version, message}] (loaded, reloaded, failed, pending, applied, released), lastSequence, dataDigest and changed. Development builds read edited files again on each call (check=false skips that) and swap them in under the same handle: textures, SVG, sounds and models at once; YAML data only stays 'pending' until apply_data=true, because sim code reads it. A file that fails to load keeps the old content and shows state 'error' with the reason. reload=<path> rereads one asset now. since=N returns only events after sequence N.",
            """{"type":"object","properties":{"check":{"type":"boolean"},"reload":{"type":"string"},"apply_data":{"type":"boolean"},"since":{"type":"integer","minimum":0}}}""", false),
    ];
}

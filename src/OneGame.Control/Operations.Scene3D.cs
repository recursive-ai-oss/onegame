namespace OneGame.Control;

/// <summary>Result of <c>camera</c>: the camera the 3D view is drawn with.</summary>
/// <param name="Position">World position.</param>
/// <param name="Forward">Unit vector the camera looks along.</param>
/// <param name="Up">Unit vector pointing up in the image.</param>
/// <param name="FovDegrees">Vertical field of view (perspective).</param>
/// <param name="OrthoHeight">Visible world height of an orthographic camera; 0 for perspective.</param>
/// <param name="Near">Near plane distance.</param>
/// <param name="Far">Far plane distance.</param>
/// <param name="Entity">The entity that offered the camera (<c>Type#id</c>), or null for the default or an override.</param>
/// <param name="Overridden">True while a <c>camera</c> call has replaced the entities' camera.</param>
public sealed record CameraResult(
    double[] Position, double[] Forward, double[] Up, double FovDegrees, double OrthoHeight, double Near, double Far, string? Entity, bool Overridden);

/// <summary>Result of <c>pick</c>: what is under a pixel, or along a ray.</summary>
/// <param name="Hit">True when a pickable model was hit.</param>
/// <param name="Entity">The entity as <c>Type#id</c>, when hit.</param>
/// <param name="Id">The entity id, when hit.</param>
/// <param name="Address">The entity as <c>Type#n</c>, n counting from 1 among entities of its type: the form other operations and scenarios take.</param>
/// <param name="Model">The model name, when hit.</param>
/// <param name="Distance">Distance from the ray origin, when hit.</param>
/// <param name="Point">The hit position in the world, when hit.</param>
/// <param name="Origin">The ray origin.</param>
/// <param name="Direction">The ray direction (unit length).</param>
/// <param name="Tick">The tick the answer is for.</param>
public sealed record PickResult(bool Hit, string? Entity, ulong? Id, string? Address, string? Model, double? Distance, double[]? Point, double[] Origin, double[] Direction, long Tick);

/// <summary>Result of <c>project</c>: where a world point lands on the screen.</summary>
/// <param name="OnScreen">True when the point is in front of the camera, between its planes, and inside the target.</param>
/// <param name="InFront">True when the point is between the near and far planes (it may still be off to the side).</param>
/// <param name="X">Pixels from the left.</param>
/// <param name="Y">Pixels from the top.</param>
/// <param name="Depth">Distance in front of the camera.</param>
/// <param name="Width">Target width used.</param>
/// <param name="Height">Target height used.</param>
public sealed record ProjectResult(bool OnScreen, bool InFront, double X, double Y, double Depth, int Width, int Height);

public static partial class ControlOps
{
    private static ControlOp[] Scene3DOps =>
    [
        new("camera",
            "Read or move the camera of a game that draws entities in 3D. With no arguments: the camera in use. position [x,y,z], target [x,y,z] (look at it), fovDegrees, near, far and orthoHeight replace parts of it as a view-only override (simulation state is untouched); reset=true follows the entities' camera again.",
            """{"type":"object","properties":{"position":{"type":"array","items":{"type":"number"},"minItems":3,"maxItems":3},"target":{"type":"array","items":{"type":"number"},"minItems":3,"maxItems":3},"fovDegrees":{"type":"number"},"near":{"type":"number"},"far":{"type":"number"},"orthoHeight":{"type":"number"},"reset":{"type":"boolean"}}}""", false),
        new("pick",
            "Which entity's model is under a pixel (x, y from the top left of a target of 'size', default the window) or along a ray (origin [x,y,z] and direction [x,y,z]). Reports the entity, model, distance and world point; hit=false when nothing pickable is there.",
            """{"type":"object","properties":{"x":{"type":"number"},"y":{"type":"number"},"size":{"type":"string"},"origin":{"type":"array","items":{"type":"number"},"minItems":3,"maxItems":3},"direction":{"type":"array","items":{"type":"number"},"minItems":3,"maxItems":3}}}""", false),
        new("project",
            "Where a world point [x,y,z] lands on the screen of a target of 'size' (default the window): pixel x and y from the top left, depth, and whether it is visible.",
            """{"type":"object","properties":{"point":{"type":"array","items":{"type":"number"},"minItems":3,"maxItems":3},"size":{"type":"string"}},"required":["point"]}""", false),
    ];
}

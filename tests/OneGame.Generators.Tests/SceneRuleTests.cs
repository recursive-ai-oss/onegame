namespace OneGame.Generators.Tests;

/// <summary>The rules as they apply to 3D transforms: the synced types compile clean, a view cannot write them, floats cannot be synced.</summary>
public class SceneRuleTests
{
    private const string Body = "[Synced] public partial Transform3 Transform { get; set; }\n[Synced] public partial FVec3 Home { get; set; }\n[Synced] public partial FQuat Facing { get; set; }\n";

    private static string Entity(string members, string extra = "") => Harness.Entity(Body + members, extra);

    [Fact]
    public async Task SyncedTransformTypesGenerateCodecsAndCompile()
    {
        var diagnostics = await Harness.Diagnose(Entity(
            "[Synced] public partial Timed<Transform3> Glide { get; set; } [Synced] public SyncList<FVec3> Path { get; } = new(); [Command] public void MoveTo(FVec3 p, FQuat q) { Transform = Transform.WithPosition(p).WithRotation(q); }\n"
            + "protected override void OnTick() { Transform = Transform.Translated(new FVec3(Fixed.One, Fixed.Zero, Fixed.Zero)); }"));
        Assert.True(diagnostics.Count == 0, Harness.Describe(diagnostics));
    }

    [Fact]
    public async Task ViewAssigningASyncedTransformRaisesOG0008()
    {
        var diagnostics = await Harness.Diagnose(Entity("[View] public void Show() { Transform = Transform3.Identity; }"));
        var d = Assert.Single(diagnostics);
        Assert.Equal("OG0008", d.Id);
        Assert.Contains("Transform", d.GetMessage());
    }

    [Theory]
    [InlineData("[View] public void Show() { Transform = Transform.WithPosition(new FVec3(1, 2, 3)); }")]
    [InlineData("[View] public void Show() { Home += FVec3.UnitX; }")]
    [InlineData("[View] public void Show() { Facing = FQuat.Identity; }")]
    [InlineData("private void Snap() { Transform = default; } [View] public void Show() { Snap(); }")]
    public async Task ViewWritesToAnyOfTheThreeDimensionalTypesRaiseOG0008(string members)
    {
        var diagnostics = await Harness.Diagnose(Entity(members));
        Assert.Contains(diagnostics, d => d.Id == "OG0008");
        Assert.All(diagnostics, d => Assert.Equal("OG0008", d.Id));
    }

    [Fact]
    public async Task ViewMayReadTransformsAndDescribeThem()
    {
        // A view computes with floats from the fixed-point state and only reads it.
        var diagnostics = await Harness.Diagnose(Entity(
            "[View] public System.Numerics.Vector3 Where() => new System.Numerics.Vector3(Transform.Position.X.ToSingle(), Home.Y.ToSingle(), Facing.W.ToSingle());"
            + "[View] public FVec3 Ahead(Fixed alpha) => Transform.Position + Transform.Forward * alpha;"));
        Assert.True(diagnostics.Count == 0, Harness.Describe(diagnostics));
    }

    [Fact]
    public async Task ViewMethodImplementingAnInterfaceIsStillAView()
    {
        const string Interface = "public interface IShow { void Describe(); }";
        var bad = await Harness.Diagnose(Harness.Entity(Body + "", Interface).Replace("class E : Entity", "class E : Entity, IShow").Replace("[Command] public void Bump()", "[View] public void Describe() { Home = FVec3.One; } [Command] public void Bump()"));
        Assert.Contains(bad, d => d.Id == "OG0008");
    }

    [Theory]
    [InlineData("[Synced] public partial float Yaw { get; set; }")]
    [InlineData("[Synced] public partial double X { get; set; }")]
    [InlineData("[Synced] public partial System.Numerics.Vector3 Position { get; set; }")]
    [InlineData("[Synced] public partial System.Numerics.Quaternion Rotation { get; set; }")]
    [InlineData("[Synced] public partial System.Numerics.Matrix4x4 World { get; set; }")]
    [InlineData("[Synced] public SyncList<System.Numerics.Vector3> Path { get; } = new();")]
    [InlineData("[Command] public void Aim(System.Numerics.Vector3 at) { }")]
    public async Task FloatsInSynced3DStateRaiseOG0001(string members)
    {
        var diagnostics = await Harness.Diagnose(Entity(members));
        Assert.Contains(diagnostics, d => d.Id == "OG0001");
        Assert.Contains("Transform3", diagnostics.First(d => d.Id == "OG0001").GetMessage());
    }

    [Fact]
    public async Task FloatVectorMathInSimCodeRaisesOG0002()
    {
        var diagnostics = await Harness.Diagnose(Entity("protected override void OnTick() { var v = System.Numerics.Vector3.One; }"));
        Assert.Contains(diagnostics, d => d.Id == "OG0002");
    }

    [Fact]
    public async Task SyncedFloatFieldInASyncedStructRaisesOG0001()
    {
        var diagnostics = await Harness.Diagnose(Entity("", "[SyncedStruct] public partial struct Pose { public FVec3 Where { get; set; } public float Heading { get; set; } }"));
        Assert.Contains(diagnostics, d => d.Id == "OG0001");
    }
}

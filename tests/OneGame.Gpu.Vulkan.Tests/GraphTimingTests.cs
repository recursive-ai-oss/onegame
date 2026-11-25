using OneGame.Gpu.Graph;

namespace OneGame.Gpu.Vulkan.Tests;

/// <summary>The conversion of a pair of timestamps into an elapsed time.</summary>
public sealed class GraphTimingTests
{
    [Fact]
    public void AnIntervalIsTheDifferenceOfItsEnds()
    {
        Assert.Equal(1_500UL, RenderGraph.Elapsed(1_000, 2_500));
        Assert.Equal(0UL, RenderGraph.Elapsed(7, 7));
    }

    [Theory]
    [InlineData(2_500UL, 1_000UL)]                      // an almost empty pass stamped at the end before the start
    [InlineData(68_719_476_000UL, 12UL)]                // a 36-bit counter that wrapped between the two stamps
    [InlineData(ulong.MaxValue, 0UL)]
    public void AReversedIntervalIsZeroNotAboutTwoToTheSixtyFourNanoseconds(ulong start, ulong end)
    {
        var elapsed = RenderGraph.Elapsed(start, end);
        Assert.Equal(0UL, elapsed);
        Assert.True(elapsed / 1000.0 / 1000.0 < 1e6, "a wrapped interval would read as about 1.8e13 milliseconds");
    }
}

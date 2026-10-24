using System.Text;

namespace OneGame.Core.Tests;

public class FixedTests
{
    private static Fixed F(double v) => Fixed.FromDouble(v);

    [Fact]
    public void ExactArithmetic()
    {
        Assert.Equal(F(3), F(1.5) * 2);
        Assert.Equal(F(0.75), F(1.5) / 2);
        Assert.Equal(F(-0.75), F(-1.5) * F(0.5));
        Assert.Equal(F(2.5), F(7.5) % F(5));
        Assert.Equal(-1, Fixed.Sign(F(-0.001)));
        Assert.Equal(F(2), Fixed.Max(1, 2));
        Assert.Equal(F(5), Fixed.Clamp(9, 0, 5));
        Assert.Equal(F(4), Fixed.Lerp(2, 6, F(0.5)));
        Assert.Equal(1L << 32, Fixed.One.Raw);
    }

    [Fact]
    public void RoundingHelpers()
    {
        Assert.Equal(F(2), Fixed.Floor(F(2.75)));
        Assert.Equal(F(-3), Fixed.Floor(F(-2.25)));
        Assert.Equal(F(3), Fixed.Ceiling(F(2.25)));
        Assert.Equal(F(-2), Fixed.Ceiling(F(-2.25)));
        Assert.Equal(F(-2), Fixed.Truncate(F(-2.75)));
        Assert.Equal(F(0.75), Fixed.Frac(F(-2.25)));
        Assert.Equal(3, F(2.5).RoundToInt());
        Assert.Equal(-3, F(-2.5).RoundToInt());
        Assert.Equal(-2, F(-2.75).ToInt());
        Assert.Equal(-3, F(-2.75).FloorToInt());
    }

    [Fact]
    public void MultiplyAndDivideTrackDoublesAcrossTheRange()
    {
        var seed = 12345UL;
        for (var i = 0; i < 2000; i++)
        {
            seed = (seed * 6364136223846793005UL) + 1442695040888963407UL;
            var a = (double)(int)(seed >> 40) / 4096.0;
            seed = (seed * 6364136223846793005UL) + 1442695040888963407UL;
            var b = (double)(int)(seed >> 40) / 4096.0;
            Assert.InRange(Math.Abs((F(a) * F(b)).ToDouble() - (a * b)), 0, 1e-8);
            if (Math.Abs(b) > 0.01)
            {
                Assert.InRange(Math.Abs((F(a) / F(b)).ToDouble() - (a / b)), 0, 1e-8);
            }
        }
    }

    [Fact]
    public void DivideByZeroThrows() => Assert.Throws<DivideByZeroException>(() => Fixed.One / Fixed.Zero);

    [Fact]
    public void SqrtIsExactForSquaresAndCloseElsewhere()
    {
        Assert.Equal(F(0), Fixed.Sqrt(F(0)));
        Assert.Equal(F(2), Fixed.Sqrt(F(4)));
        Assert.Equal(F(1.5), Fixed.Sqrt(F(2.25)));
        for (var v = 0.001; v < 100000; v *= 1.37)
        {
            Assert.InRange(Math.Abs(Fixed.Sqrt(F(v)).ToDouble() - Math.Sqrt(v)), 0, 1e-8);
        }

        Assert.Throws<ArgumentOutOfRangeException>(() => Fixed.Sqrt(F(-1)));
    }

    [Fact]
    public void TrigTracksDoubles()
    {
        for (var a = -25.0; a < 25.0; a += 0.0137)
        {
            Assert.InRange(Math.Abs(Fixed.Sin(F(a)).ToDouble() - Math.Sin(a)), 0, 2e-8);
            Assert.InRange(Math.Abs(Fixed.Cos(F(a)).ToDouble() - Math.Cos(a)), 0, 2e-8);
        }

        for (var a = -3.14; a < 3.14; a += 0.0173)
        {
            var (y, x) = (Math.Sin(a) * 7.5, Math.Cos(a) * 7.5);
            Assert.InRange(Math.Abs(Fixed.Atan2(F(y), F(x)).ToDouble() - Math.Atan2(y, x)), 0, 2e-8);
        }

        Assert.Equal(Fixed.Zero, Fixed.Atan2(0, 0));
        Assert.InRange(Math.Abs(Fixed.Atan2(F(1e-6), F(2e-6)).ToDouble() - Math.Atan2(1, 2)), 0, 1e-6);
        Assert.InRange(Math.Abs(Fixed.Atan2(F(1000000), F(-1000000)).ToDouble() - (3 * Math.PI / 4)), 0, 2e-8);
        Assert.InRange(Math.Abs(Fixed.Atan(F(1)).ToDouble() - (Math.PI / 4)), 0, 2e-8);
    }

    /// <summary>Raw results committed as expected values: any change to the algorithms changes these bits.</summary>
    [Fact]
    public void GoldenBits()
    {
        var sb = new StringBuilder();
        void Add(string name, Fixed v) => sb.Append(name).Append('=').Append(v.Raw).Append(';');
        Add("sin1", Fixed.Sin(F(1)));
        Add("cos1", Fixed.Cos(F(1)));
        Add("sin-7.25", Fixed.Sin(F(-7.25)));
        Add("sin100", Fixed.Sin(F(100)));
        Add("atan2(1,2)", Fixed.Atan2(F(1), F(2)));
        Add("atan2(-3,-1)", Fixed.Atan2(F(-3), F(-1)));
        Add("sqrt2", Fixed.Sqrt(F(2)));
        Add("sqrt1e6+", Fixed.Sqrt(F(1000000.5)));
        Add("1/3", Fixed.One / 3);
        Add("pi*pi", Fixed.Pi * Fixed.Pi);
        Add("-1.7*2.9", F(-1.7) * F(2.9));
        Assert.Equal(
            "sin1=3614090361;cos1=2320580734;sin-7.25=-3535105458;sin100=-2174823882;atan2(1,2)=1991351315;atan2(-3,-1)=-8128426958;sqrt2=6074000999;sqrt1e6+=4294968369741;1/3=1431655765;pi*pi=42389628130;-1.7*2.9=-21174188769;",
            sb.ToString());
    }

    [Theory]
    [InlineData("0", "0")]
    [InlineData("1.5", "1.5")]
    [InlineData("-12.375", "-12.375")]
    [InlineData("0.1", "0.1")]
    [InlineData("2147483647", "2147483647")]
    [InlineData("-2147483648", "-2147483648")]
    public void TextRoundTrips(string text, string expected)
    {
        Assert.True(Fixed.TryParse(text, out var v));
        Assert.Equal(expected, v.ToString());
    }

    [Theory]
    [InlineData("")]
    [InlineData("abc")]
    [InlineData("1.")]
    [InlineData(".5")]
    [InlineData("1e3")]
    [InlineData("2147483649")]
    [InlineData("99999999999999999999")]
    public void BadTextIsRefusedWithoutThrowing(string text) => Assert.False(Fixed.TryParse(text, out _));

    [Fact]
    public void CodecRoundTrips()
    {
        foreach (var raw in new long[] { 0, 1, -1, long.MaxValue, long.MinValue, 1L << 32 })
        {
            var w = new SyncWriter();
            Fixed.FromRaw(raw).Write(w);
            var r = new SyncReader(w.Written);
            Assert.Equal(raw, Fixed.Read(ref r).Raw);
            Assert.True(r.Ok && r.AtEnd);
        }
    }
}

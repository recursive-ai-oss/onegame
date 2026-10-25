namespace OneGame.Platform.Tests;

// The iOS hardware keyboard reports USB HID keyboard usages (HID Usage Tables 1.4, section 10); these check the table against known entries.
public class IosKeysTests
{
    [Theory]
    [InlineData(4, Key.A)]
    [InlineData(29, Key.Z)]
    [InlineData(30, Key.Digit1)]
    [InlineData(38, Key.Digit9)]
    [InlineData(39, Key.Digit0)]
    [InlineData(40, Key.Enter)]
    [InlineData(41, Key.Escape)]
    [InlineData(42, Key.Backspace)]
    [InlineData(44, Key.Space)]
    [InlineData(58, Key.F1)]
    [InlineData(69, Key.F12)]
    [InlineData(79, Key.ArrowRight)]
    [InlineData(82, Key.ArrowUp)]
    [InlineData(89, Key.Keypad1)]
    [InlineData(98, Key.Keypad0)]
    [InlineData(224, Key.LeftControl)]
    [InlineData(231, Key.RightMeta)]
    public void UsagesMapToPhysicalKeys(int usage, Key expected) => Assert.Equal(expected, IosKeys.ToKey(usage));

    [Fact]
    public void UnknownUsagesAreUnknown()
    {
        Assert.Equal(Key.Unknown, IosKeys.ToKey(0));
        Assert.Equal(Key.Unknown, IosKeys.ToKey(232));
    }

    [Theory]
    [InlineData(4, true)]
    [InlineData(39, true)]
    [InlineData(44, true)]
    [InlineData(56, true)]
    [InlineData(40, false)]
    [InlineData(42, false)]
    [InlineData(43, false)]
    [InlineData(82, false)]
    [InlineData(88, false)]
    public void OnlyTypingKeysArePrintable(int usage, bool printable) => Assert.Equal(printable, IosKeys.IsPrintable(usage));

    [Fact]
    public void ModifierFlagsMapToModifiers()
    {
        Assert.Equal(KeyModifiers.Shift | KeyModifiers.Meta, IosKeys.ToModifiers(0x20000 | 0x100000));
        Assert.Equal(KeyModifiers.Control | KeyModifiers.Alt | KeyModifiers.CapsLock, IosKeys.ToModifiers(0x40000 | 0x80000 | 0x10000));
        Assert.Equal(KeyModifiers.None, IosKeys.ToModifiers(0));
    }
}

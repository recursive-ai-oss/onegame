using OneGame.Text;

namespace OneGame.Pages.Tests;

public class PageFontsTests
{
    [Fact]
    public void AFaceIsLoadedOnceAndSharedByThePageAndTheGameText()
    {
        var library = new FontLibrary();
        var fonts = new PageFonts(library);
        var id = fonts.Add(File.ReadAllBytes(Paths.TextData("fonts/NotoSans-Subset-Regular.ttf")), "Body");

        Assert.Equal(1, library.FaceCount);
        Assert.Equal(1, fonts.Catalog.FaceCount);
        Assert.NotNull(library.Face(id));
    }
}

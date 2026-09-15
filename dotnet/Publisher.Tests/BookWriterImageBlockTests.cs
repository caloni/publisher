using Publisher.Core.Models;
using Publisher.Core.Writers;

namespace Publisher.Tests;

public class BookWriterImageBlockTests
{
    private static GlobalState BuildState()
    {
        var state = new GlobalState();
        state.Settings["output"] = Path.GetTempPath();
        return state;
    }

    [Fact]
    public void ImageBlock_AsLastBlockOfPost_BackLinkGoesAfterImgTag_NotInsideSrcAttribute()
    {
        var state = BuildState();
        var post = new Post
        {
            Title = "Some Post",
            Slug = "some-post",
            Date = "2024-01-01",
            Blocks = { new PostBlock { Kind = BlockKind.Image, Content = "photo.jpg" } }
        };

        var writer = new BookWriter(state);
        var backLink = " <a href=\"#_some_post\"><em>&lt;</em></a>";
        var result = writer.FormatBlock(0, post, backLink);

        Assert.Equal("<p><img src=\"img/photo.jpg\"/> <a href=\"#_some_post\"><em>&lt;</em></a></p>\n", result);
        Assert.DoesNotContain("jpg <a href=", result);
    }

    [Fact]
    public void ImageBlock_NotLastBlockOfPost_HasNoTrailingBackLink()
    {
        var state = BuildState();
        var post = new Post
        {
            Title = "Some Post",
            Slug = "some-post",
            Date = "2024-01-01",
            Blocks =
            {
                new PostBlock { Kind = BlockKind.Image, Content = "photo.jpg" },
                new PostBlock { Kind = BlockKind.Paragraph, Content = "More text." }
            }
        };

        var writer = new BookWriter(state);
        var result = writer.FormatBlock(0, post);

        Assert.Equal("<p><img src=\"img/photo.jpg\"/></p>\n", result);
    }
}

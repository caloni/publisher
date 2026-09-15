using Publisher.Core.Models;
using Publisher.Core.Writers;

namespace Publisher.Tests;

public class BookWriterListBlockTests
{
    private static GlobalState BuildState()
    {
        var state = new GlobalState();
        state.Settings["output"] = Path.GetTempPath();
        return state;
    }

    [Fact]
    public void UnorderedList_AsFirstBlockOfPost_OpensUl()
    {
        // Regression test: a post whose very first content block is a list has no
        // previous block to compare kinds against, but must still open its own <ul> -
        // otherwise every <li> in the list is emitted with no enclosing list at all.
        var state = BuildState();
        var post = new Post
        {
            Title = "Top Ten",
            Slug = "top-ten",
            Date = "2024-01-01",
            Blocks =
            {
                new PostBlock { Kind = BlockKind.UnorderedList, Content = "First item" },
                new PostBlock { Kind = BlockKind.UnorderedList, Content = "Second item" }
            }
        };

        var writer = new BookWriter(state);
        var first = writer.FormatBlock(0, post);
        var second = writer.FormatBlock(1, post);

        Assert.Equal("<ul><li>First item</li>\n", first);
        Assert.Equal("<li>Second item</li></ul>\n", second);
    }

    [Fact]
    public void OrderedList_AsFirstBlockOfPost_OpensOl_NotUl()
    {
        var state = BuildState();
        var post = new Post
        {
            Title = "Ranked List",
            Slug = "ranked-list",
            Date = "2024-01-01",
            Blocks = { new PostBlock { Kind = BlockKind.OrderedList, Content = "Only item" } }
        };

        var writer = new BookWriter(state);
        var result = writer.FormatBlock(0, post);

        Assert.Equal("<ol><li>Only item</li></ol>\n", result);
        Assert.DoesNotContain("<ul", result);
    }

    [Fact]
    public void UnorderedList_AsLastBlockOfPost_ClosesUlWithBackLinkInsideLi()
    {
        var state = BuildState();
        var post = new Post
        {
            Title = "Top Ten",
            Slug = "top-ten",
            Date = "2024-01-01",
            Blocks = { new PostBlock { Kind = BlockKind.UnorderedList, Content = "Only item" } }
        };

        var writer = new BookWriter(state);
        var backLink = " <a href=\"#_top_ten\"><em>&lt;</em></a>";
        var result = writer.FormatBlock(0, post, backLink);

        Assert.Equal("<ul><li>Only item <a href=\"#_top_ten\"><em>&lt;</em></a></li></ul>\n", result);
    }

    [Fact]
    public void ListFollowedByParagraph_ClosesListBeforeParagraph()
    {
        var state = BuildState();
        var post = new Post
        {
            Title = "Mixed Content",
            Slug = "mixed-content",
            Date = "2024-01-01",
            Blocks =
            {
                new PostBlock { Kind = BlockKind.UnorderedList, Content = "Item" },
                new PostBlock { Kind = BlockKind.Paragraph, Content = "Text after." }
            }
        };

        var writer = new BookWriter(state);
        var result = writer.FormatBlock(0, post);

        Assert.Equal("<ul><li>Item</li></ul>\n", result);
    }
}

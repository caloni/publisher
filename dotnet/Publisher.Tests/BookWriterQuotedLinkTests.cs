using Publisher.Core.Models;
using Publisher.Core.Writers;

namespace Publisher.Tests;

public class BookWriterQuotedLinkTests
{
    private static GlobalState BuildState()
    {
        var state = new GlobalState();
        state.Settings["output"] = Path.GetTempPath();
        return state;
    }

    private static void RegisterPost(GlobalState state, string title, string slug, string? chapter = null)
    {
        state.TitleToSlug[title] = slug;
        state.PostMetadata[slug] = new PostMetadata { Chapter = chapter };
    }

    private static Post MakeParagraphPost(string content)
    {
        return new Post
        {
            Title = "Source",
            Slug = "source",
            Date = "2024-01-01",
            Blocks = { new PostBlock { Kind = BlockKind.Paragraph, Content = content } }
        };
    }

    private static string ProcessedContent(GlobalState state, Post post)
    {
        var writer = new BookWriter(state);
        writer.ProcessPostLines(post);
        return post.Blocks[0].Content ?? "";
    }

    [Fact]
    public void QuotedTitle_WithChapter_IsLinkedToChapterXhtmlAnchor()
    {
        var state = BuildState();
        RegisterPost(state, "Top Filmes 2019", "top-filmes-2019", chapter: "2019-12");

        var post = MakeParagraphPost("Veja \"Top Filmes 2019\" para a lista completa.");
        var result = ProcessedContent(state, post);

        Assert.Contains("<a href=\"_201912.xhtml#_top_filmes_2019\">Top Filmes 2019</a>", result);
    }

    [Fact]
    public void QuotedTitle_WithoutChapter_IsLinkedToSlugXhtml()
    {
        var state = BuildState();
        RegisterPost(state, "My Book Post", "my-book-post", chapter: null);

        var post = MakeParagraphPost("Read \"My Book Post\" for details.");
        var result = ProcessedContent(state, post);

        Assert.Contains("<a href=\"_my_book_post.xhtml\">My Book Post</a>", result);
    }

    [Fact]
    public void QuotedTitle_NoMatch_IsUnchanged()
    {
        var state = BuildState();

        var post = MakeParagraphPost("This is \"not a title\" here.");
        var result = ProcessedContent(state, post);

        Assert.Contains("\"not a title\"", result);
        Assert.DoesNotContain("<a href", result);
    }

    [Fact]
    public void QuotedTitle_MatchIsCaseSensitive_WrongCaseIsUnchanged()
    {
        var state = BuildState();
        RegisterPost(state, "Top Filmes 2019", "top-filmes-2019", chapter: "2019-12");

        var post = MakeParagraphPost("Veja \"top filmes 2019\" aqui.");
        var result = ProcessedContent(state, post);

        Assert.Contains("\"top filmes 2019\"", result);
        Assert.DoesNotContain("<a href", result);
    }

    [Fact]
    public void QuotedTitle_MultipleInOneLine_AllLinked()
    {
        var state = BuildState();
        RegisterPost(state, "Top Filmes 2019", "top-filmes-2019", chapter: "2019-12");
        RegisterPost(state, "Top Filmes 2018", "top-filmes-2018", chapter: "2018-12");

        var post = MakeParagraphPost("Veja \"Top Filmes 2019\" e \"Top Filmes 2018\".");
        var result = ProcessedContent(state, post);

        Assert.Contains("<a href=\"_201912.xhtml#_top_filmes_2019\">Top Filmes 2019</a>", result);
        Assert.Contains("<a href=\"_201812.xhtml#_top_filmes_2018\">Top Filmes 2018</a>", result);
    }

    [Fact]
    public void PreBlock_QuotedTitle_IsNotLinked()
    {
        var state = BuildState();
        RegisterPost(state, "Top Filmes 2019", "top-filmes-2019", chapter: "2019-12");

        var post = new Post
        {
            Title = "Source",
            Slug = "source",
            Date = "2024-01-01",
            Blocks = { new PostBlock { Kind = BlockKind.Code, Content = "\"Top Filmes 2019\"" } }
        };

        var writer = new BookWriter(state);
        writer.ProcessPostLines(post);

        Assert.DoesNotContain("<a href", post.Blocks[0].Content ?? "");
    }

    [Fact]
    public void ListItem_QuotedTitle_IsLinkedAndNotEscaped()
    {
        var state = BuildState();
        RegisterPost(state, "Top Filmes 2019", "top-filmes-2019", chapter: "2019-12");

        var post = new Post
        {
            Title = "Source",
            Slug = "source",
            Date = "2024-01-01",
            Blocks = { new PostBlock { Kind = BlockKind.UnorderedList, Content = "\"Top Filmes 2019\"" } }
        };

        var writer = new BookWriter(state);
        writer.ProcessPostLines(post);

        Assert.Contains("<a href=\"_201912.xhtml#_top_filmes_2019\">Top Filmes 2019</a>", post.Blocks[0].Content ?? "");
        Assert.DoesNotContain("&lt;", post.Blocks[0].Content ?? "");
        Assert.DoesNotContain("&gt;", post.Blocks[0].Content ?? "");
    }

    [Fact]
    public void Blockquote_QuotedTitle_IsLinkedAndNotEscaped()
    {
        var state = BuildState();
        RegisterPost(state, "Top Filmes 2019", "top-filmes-2019", chapter: "2019-12");

        var post = new Post
        {
            Title = "Source",
            Slug = "source",
            Date = "2024-01-01",
            Blocks = { new PostBlock { Kind = BlockKind.Blockquote, Content = "\"Top Filmes 2019\"" } }
        };

        var writer = new BookWriter(state);
        writer.ProcessPostLines(post);

        Assert.Contains("<a href=\"_201912.xhtml#_top_filmes_2019\">Top Filmes 2019</a>", post.Blocks[0].Content ?? "");
        Assert.DoesNotContain("&lt;", post.Blocks[0].Content ?? "");
        Assert.DoesNotContain("&gt;", post.Blocks[0].Content ?? "");
    }

    [Fact]
    public void QuotedDescription_WithYamlImagePath_IsRenderedAsImg()
    {
        var state = BuildState();
        state.Links["My Photo"] = "photos/my-photo.jpg";

        var post = MakeParagraphPost("Look at \"My Photo\" here.");
        var result = ProcessedContent(state, post);

        Assert.Contains("<img src=\"img/my-photo.jpg\"/>", result);
        Assert.DoesNotContain("<a href", result);
        Assert.DoesNotContain("\"My Photo\"", result);
    }

    [Fact]
    public void QuotedDescription_WithYamlImagePath_NoDirectory_IsRenderedAsImg()
    {
        var state = BuildState();
        state.Links["Cover"] = "cover.png";

        var post = MakeParagraphPost("The book has a \"Cover\".");
        var result = ProcessedContent(state, post);

        Assert.Contains("<img src=\"img/cover.png\"/>", result);
        Assert.DoesNotContain("<a href", result);
    }

    [Fact]
    public void QuotedDescription_WithYamlUrl_IsStillLinked()
    {
        var state = BuildState();
        state.Links["My Site"] = "https://example.com";

        var post = MakeParagraphPost("Visit \"My Site\" today.");
        var result = ProcessedContent(state, post);

        Assert.Contains("<a href=\"https://example.com\">My Site</a>", result);
        Assert.DoesNotContain("<img", result);
    }

    [Fact]
    public void BracketLink_WithImagePath_IsRenderedAsImg()
    {
        var state = BuildState();

        var post = new Post
        {
            Title = "Source",
            Slug = "source",
            Date = "2024-01-01",
            Links = { ["My Photo"] = "photos/my-photo.jpg" },
            Blocks = { new PostBlock { Kind = BlockKind.Paragraph, Content = "See [My Photo] above." } }
        };

        var writer = new BookWriter(state);
        writer.ProcessPostLines(post);
        var result = post.Blocks[0].Content ?? "";

        Assert.Contains("<img src=\"img/my-photo.jpg\"/>", result);
        Assert.DoesNotContain("<a href", result);
    }
}

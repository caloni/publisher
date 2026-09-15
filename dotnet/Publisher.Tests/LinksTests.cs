using Publisher.Core.Models;
using Publisher.Core.Utilities;
using Publisher.Core.Writers;

namespace Publisher.Tests;

public class LinksTests
{
    // ── LinksLoader tests ──────────────────────────────────────────────────

    [Fact]
    public void Loader_ParsesValidYaml()
    {
        var yaml = "- description: GitHub\n  link: https://github.com\n";
        var tmp = Path.GetTempFileName();
        File.WriteAllText(tmp, yaml);

        var links = LinksLoader.Load(tmp);

        Assert.Single(links);
        Assert.Equal("https://github.com", links["GitHub"]);
    }

    [Fact]
    public void Loader_SkipsEntriesWithMissingFields()
    {
        var yaml = "- description: NoLink\n  link: \"\"\n- description: \"\"\n  link: https://example.com\n";
        var tmp = Path.GetTempFileName();
        File.WriteAllText(tmp, yaml);

        var links = LinksLoader.Load(tmp);

        Assert.Empty(links);
    }

    [Fact]
    public void Loader_ReturnsEmptyDictForEmptyFile()
    {
        var tmp = Path.GetTempFileName();
        File.WriteAllText(tmp, "");

        var links = LinksLoader.Load(tmp);

        Assert.Empty(links);
    }

    [Fact]
    public void LoadDates_ReturnsDateForEntriesThatDeclareOne()
    {
        var yaml = "- description: Os Suspeitos\n  link: https://example.com\n  date: 2014-10-04\n" +
                   "- description: GitHub\n  link: https://github.com\n";
        var tmp = Path.GetTempFileName();
        File.WriteAllText(tmp, yaml);

        var dates = LinksLoader.LoadDates(tmp);

        Assert.Single(dates);
        Assert.Equal("2014-10-04", dates["Os Suspeitos"]);
    }

    // ── BlogWriter integration tests ───────────────────────────────────────

    private static GlobalState BuildState(bool singlePostMode = false)
    {
        var state = new GlobalState();
        state.Settings["output"] = Path.GetTempPath();
        state.Settings["mode"] = "journal";
        state.Settings["single_post_mode"] = singlePostMode ? "1" : "0";
        return state;
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
        var writer = new BlogWriter(state);
        writer.ProcessPostLines(post);
        return post.Blocks[0].Content ?? "";
    }

    [Fact]
    public void ExternalQuotedTitle_IsLinkedToExternalUrl()
    {
        var state = BuildState();
        state.Links["GitHub"] = "https://github.com";

        var post = MakeParagraphPost("Check out \"GitHub\" today.");
        var result = ProcessedContent(state, post);

        Assert.Contains("<a href=\"https://github.com\">GitHub</a>", result);
    }

    [Fact]
    public void InternalTitleTakesPrecedenceOverExternalLink()
    {
        var state = BuildState();
        state.TitleToSlug["GitHub"] = "github-post";
        state.Index["github-post"] = new Post { Title = "GitHub", Slug = "github-post", Date = "2024-01-01" };
        state.PostMetadata["github-post"] = new PostMetadata { Chapter = null };
        state.Links["GitHub"] = "https://github.com";

        var post = MakeParagraphPost("Read \"GitHub\" here.");
        var result = ProcessedContent(state, post);

        Assert.Contains("<a href=\"github-post.html\">GitHub</a>", result);
        Assert.DoesNotContain("https://github.com", result);
    }

    [Fact]
    public void QuotedTextWithNoMatchIsUnchanged()
    {
        var state = BuildState();

        var post = MakeParagraphPost("This is \"unknown text\" here.");
        var result = ProcessedContent(state, post);

        Assert.Contains("\"unknown text\"", result);
        Assert.DoesNotContain("<a href", result);
    }

    [Fact]
    public void ExternalLink_UrlIsHtmlEncoded()
    {
        var state = BuildState();
        state.Links["Search"] = "https://example.com/q?a=1&b=2";

        var post = MakeParagraphPost("Use \"Search\" now.");
        var result = ProcessedContent(state, post);

        Assert.Contains("href=\"https://example.com/q?a=1&amp;b=2\"", result);
    }

    [Fact]
    public void PreBlock_ExternalLinkIsNotProcessed()
    {
        var state = BuildState();
        state.Links["GitHub"] = "https://github.com";

        var post = new Post
        {
            Title = "Source",
            Slug = "source",
            Date = "2024-01-01",
            Blocks = { new PostBlock { Kind = BlockKind.Code, Content = "\"GitHub\"" } }
        };

        var writer = new BlogWriter(state);
        writer.ProcessPostLines(post);
        var result = post.Blocks[0].Content ?? "";

        Assert.DoesNotContain("<a href", result);
    }

    // ── LinkResource tests ─────────────────────────────────────────────────

    [Theory]
    [InlineData("http://example.com")]
    [InlineData("https://example.com")]
    [InlineData("HTTP://EXAMPLE.COM")]
    public void LinkResource_IsUrl_TrueForHttpLinks(string link)
    {
        Assert.True(LinkResource.IsUrl(link));
        Assert.False(LinkResource.IsLocalPath(link));
    }

    [Theory]
    [InlineData("image.jpg")]
    [InlineData("/path/to/image.png")]
    [InlineData("relative/path.gif")]
    public void LinkResource_IsLocalPath_TrueForNonHttpLinks(string link)
    {
        Assert.True(LinkResource.IsLocalPath(link));
        Assert.False(LinkResource.IsUrl(link));
    }

    // ── Bug-fix regression: local path must not become external/blogging link ──

    [Fact]
    public void QuotedTitle_WithLocalImagePath_IsNotLinked()
    {
        // A Links entry whose value is an image path must not be used as an
        // external link anchor when the description appears quoted in a paragraph.
        var state = BuildState();
        state.Links["My Photo"] = "my-photo.jpg";

        var post = MakeParagraphPost("Look at \"My Photo\" here.");
        var result = ProcessedContent(state, post);

        Assert.DoesNotContain("<a href", result);
        Assert.Contains("\"My Photo\"", result);
    }

    [Fact]
    public void QuotedTitle_WithHttpLink_IsLinked()
    {
        // Sanity-check: a URL value still produces an anchor.
        var state = BuildState();
        state.Links["My Site"] = "https://example.com";

        var post = MakeParagraphPost("Visit \"My Site\" today.");
        var result = ProcessedContent(state, post);

        Assert.Contains("<a href=\"https://example.com\">My Site</a>", result);
    }

    [Fact]
    public void QuotedTitle_WithSlugPath_IsLinked()
    {
        // A Links entry whose value is a post slug (no image extension) must produce an anchor.
        var state = BuildState();
        state.Links["em Python"] = "houaiss_para_babylon_python";

        var post = MakeParagraphPost("versões alternativas (inclusive uma \"em Python\"!) do código.");
        var result = ProcessedContent(state, post);

        Assert.Contains("<a href=\"houaiss_para_babylon_python\">em Python</a>", result);
    }
}

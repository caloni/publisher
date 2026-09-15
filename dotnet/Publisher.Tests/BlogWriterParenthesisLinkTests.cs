using Publisher.Core.Models;
using Publisher.Core.Writers;

namespace Publisher.Tests;

public class BlogWriterParenthesisLinkTests
{
    private static GlobalState BuildState(bool singlePostMode = false)
    {
        var state = new GlobalState();
        state.Settings["output"] = Path.GetTempPath();
        state.Settings["mode"] = "journal";
        state.Settings["single_post_mode"] = singlePostMode ? "1" : "0";
        return state;
    }

    private static void RegisterPost(GlobalState state, string title, string slug, string? chapter = null, string? tags = null)
    {
        state.TitleToSlug[title] = slug;
        state.Index[slug] = new Post { Title = title, Slug = slug, Date = "2024-01-01",
            Tags = tags != null ? tags.Split(' ', StringSplitOptions.RemoveEmptyEntries).ToList() : new List<string>() };
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
        var writer = new BlogWriter(state);
        writer.ProcessPostLines(post);
        return post.Blocks[0].Content ?? "";
    }

    [Fact]
    public void ParenthesizedTitleIsLinked()
    {
        var state = BuildState();
        RegisterPost(state, "My Great Post", "my-great-post");

        var post = MakeParagraphPost("See \"My Great Post\" for details.");
        var result = ProcessedContent(state, post);

        Assert.Contains("<a href=\"my-great-post.html\">My Great Post</a>", result);
    }

    [Fact]
    public void ParenthesizedTextWithNoMatchIsUnchanged()
    {
        var state = BuildState();

        var post = MakeParagraphPost("This is \"not a post title\" here.");
        var result = ProcessedContent(state, post);

        Assert.Contains("\"not a post title\"", result);
        Assert.DoesNotContain("<a href", result);
    }

    [Fact]
    public void MatchIsCaseSensitive_WrongCaseIsUnchanged()
    {
        var state = BuildState();
        RegisterPost(state, "My Great Post", "my-great-post");

        var post = MakeParagraphPost("Look at \"my great post\" please.");
        var result = ProcessedContent(state, post);

        Assert.Contains("\"my great post\"", result);
        Assert.DoesNotContain("<a href", result);
    }

    [Fact]
    public void SinglePostMode_LinksDirectlyToSlugHtml()
    {
        var state = BuildState(singlePostMode: true);
        RegisterPost(state, "About Me", "about-me", tags: "blog");

        var post = MakeParagraphPost("Read \"About Me\" first.");
        var result = ProcessedContent(state, post);

        Assert.Contains("<a href=\"about-me.html\">About Me</a>", result);
    }

    [Fact]
    public void JournalMode_WithChapter_LinksToChapterAnchor()
    {
        var state = BuildState(singlePostMode: false);
        RegisterPost(state, "Deep Dive", "deep-dive", chapter: "2024-01");

        var post = MakeParagraphPost("Try \"Deep Dive\" next.");
        var result = ProcessedContent(state, post);

        Assert.Contains("<a href=\"2024-01.html#deep-dive\">Deep Dive</a>", result);
    }

    [Fact]
    public void MultipleParenthesizedTitlesInOneLine_AllLinked()
    {
        var state = BuildState();
        RegisterPost(state, "Post One", "post-one");
        RegisterPost(state, "Post Two", "post-two");

        var post = MakeParagraphPost("See \"Post One\" and \"Post Two\".");
        var result = ProcessedContent(state, post);

        Assert.Contains("<a href=\"post-one.html\">Post One</a>", result);
        Assert.Contains("<a href=\"post-two.html\">Post Two</a>", result);
    }

    [Fact]
    public void PreBlock_IsNotProcessed()
    {
        var state = BuildState();
        RegisterPost(state, "My Great Post", "my-great-post");

        var post = new Post
        {
            Title = "Source",
            Slug = "source",
            Date = "2024-01-01",
            Blocks = { new PostBlock { Kind = BlockKind.Code, Content = "\"My Great Post\"" } }
        };

        var writer = new BlogWriter(state);
        writer.ProcessPostLines(post);

        Assert.DoesNotContain("<a href", post.Blocks[0].Content ?? "");
    }

    [Fact]
    public void BlogMode_QuotedLink_ToPostNotInBlog_IsRenderedAsPlainText()
    {
        var state = BuildState(singlePostMode: true);
        RegisterPost(state, "Journal Only Post", "journal-only", tags: "journal");

        var post = MakeParagraphPost("See \"Journal Only Post\" for more.");
        var result = ProcessedContent(state, post);

        Assert.DoesNotContain("<a href", result);
        Assert.Contains("Journal Only Post", result);
    }

    [Fact]
    public void BlogMode_QuotedLink_ToPostInBlog_IsLinkedToSlugHtml()
    {
        var state = BuildState(singlePostMode: true);
        RegisterPost(state, "Blog Post", "blog-post", tags: "blog computer");

        var post = MakeParagraphPost("See \"Blog Post\" for more.");
        var result = ProcessedContent(state, post);

        Assert.Contains("<a href=\"blog-post.html\">Blog Post</a>", result);
    }

    [Fact]
    public void BlogMode_BracketLink_ToPostNotInBlog_IsRenderedAsPlainText()
    {
        var state = BuildState(singlePostMode: true);
        var slug = "journal-only";
        state.Index[slug] = new Post { Title = "Journal Only", Slug = slug, Date = "2024-01-01", Tags = new List<string> { "journal" } };
        state.PostMetadata[slug] = new PostMetadata { Chapter = null };

        var post = new Post
        {
            Title = "Source",
            Slug = "source",
            Date = "2024-01-01",
            Blocks = { new PostBlock { Kind = BlockKind.Paragraph, Content = "[see this]" } }
        };
        post.Links["see this"] = slug;

        var writer = new BlogWriter(state);
        writer.ProcessPostLines(post);
        var result = post.Blocks[0].Content ?? "";

        Assert.DoesNotContain("<a href", result);
        Assert.Contains("see this", result);
    }

    [Fact]
    public void BlogMode_BracketLink_ToPostInBlog_IsLinkedToSlugHtml()
    {
        var state = BuildState(singlePostMode: true);
        var slug = "blog-post";
        state.Index[slug] = new Post { Title = "Blog Post", Slug = slug, Date = "2024-01-01", Tags = new List<string> { "blog", "computer" } };
        state.PostMetadata[slug] = new PostMetadata { Chapter = null };

        var post = new Post
        {
            Title = "Source",
            Slug = "source",
            Date = "2024-01-01",
            Blocks = { new PostBlock { Kind = BlockKind.Paragraph, Content = "[see this]" } }
        };
        post.Links["see this"] = slug;

        var writer = new BlogWriter(state);
        writer.ProcessPostLines(post);
        var result = post.Blocks[0].Content ?? "";

        Assert.Contains($"<a href=\"{slug}.html\">see this</a>", result);
    }
}

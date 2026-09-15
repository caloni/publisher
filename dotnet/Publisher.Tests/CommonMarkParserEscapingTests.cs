using Publisher.Core.Models;
using Publisher.Core.Parsers;

namespace Publisher.Tests;

public class CommonMarkParserEscapingTests
{
    private static Post ParseSinglePost(string markdown)
    {
        var tmp = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".md");
        File.WriteAllText(tmp, markdown);
        try
        {
            var state = new GlobalState();
            new CommonMarkParser(state).ParseFiles(tmp);
            return state.Index.Values.Single();
        }
        finally
        {
            File.Delete(tmp);
        }
    }

    [Fact]
    public void ListItem_WithLiteralAngleBrackets_IsHtmlEscaped()
    {
        // Regression test: a literal '<' in list-item text (e.g. "N < M") used to be
        // emitted unescaped, producing invalid XHTML that broke EPUB generation.
        var markdown = "# Test Post\n2024-01-01 test\n\n- Usually getting $N is faster, where N < M\n";

        var post = ParseSinglePost(markdown);
        var content = post.Blocks.Single(b => b.Kind == BlockKind.UnorderedList).Content;

        Assert.Contains("N &lt; M", content);
        Assert.DoesNotContain("N < M", content);
    }

    [Fact]
    public void Heading_WithLiteralAngleBrackets_IsHtmlEscaped()
    {
        var markdown = "# Test Post\n2024-01-01 test\n\n## A < B\n";

        var post = ParseSinglePost(markdown);
        var content = post.Blocks.Single(b => b.Kind.IsHeading()).Content;

        Assert.Contains("A &lt; B", content);
        Assert.DoesNotContain("A < B", content);
    }

    [Fact]
    public void Blockquote_WithLiteralAngleBrackets_IsHtmlEscaped()
    {
        var markdown = "# Test Post\n2024-01-01 test\n\n> A < B\n";

        var post = ParseSinglePost(markdown);
        var content = post.Blocks.Single(b => b.Kind == BlockKind.Blockquote).Content;

        Assert.Contains("A &lt; B", content);
        Assert.DoesNotContain("A < B", content);
    }

    [Fact]
    public void Paragraph_WithLiteralAngleBrackets_IsStillHtmlEscaped()
    {
        var markdown = "# Test Post\n2024-01-01 test\n\nA < B in a paragraph.\n";

        var post = ParseSinglePost(markdown);
        var content = post.Blocks.Single(b => b.Kind == BlockKind.Paragraph).Content;

        Assert.Contains("A &lt; B", content);
        Assert.DoesNotContain("A < B", content);
    }
}

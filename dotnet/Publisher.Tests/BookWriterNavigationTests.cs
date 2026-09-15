using Publisher.Core.Models;
using Publisher.Core.Writers;

namespace Publisher.Tests;

public class BookWriterNavigationTests
{
    [Fact]
    public void Generate_WritesNavigationWithContentsAndLandmarks()
    {
        var outputPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Combine(outputPath, "EPUB"));

        try
        {
            var state = new GlobalState();
            state.Settings["output"] = outputPath;
            state.Settings["date_utc"] = "2026-07-26T00:00:00Z";

            AddPost(state, "First Post", "first-post", "2024-01-15", "2024-01");
            AddPost(state, "Second Post", "second-post", "2024-02-15", "2024-02");

            new BookWriter(state).Generate();

            var navigation = File.ReadAllText(Path.Combine(outputPath, "EPUB", "nav.xhtml"));
            var package = File.ReadAllText(Path.Combine(outputPath, "EPUB", "package.opf"));

            Assert.Contains("<nav epub:type=\"toc\">", navigation);
            Assert.Contains("<li><span>2024</span><ol>", navigation);
            Assert.Contains("<a href=\"_202401.xhtml\">01</a>", navigation);
            Assert.Contains("<a href=\"_202402.xhtml\">02</a>", navigation);
            Assert.Contains("<nav epub:type=\"landmarks\" hidden=\"hidden\">", navigation);
            Assert.Contains("epub:type=\"bodymatter\" href=\"_202401.xhtml\"", navigation);
            Assert.Contains("<item id=\"nav\" properties=\"nav\" href=\"nav.xhtml\"", package);
        }
        finally
        {
            Directory.Delete(outputPath, recursive: true);
        }
    }

    private static void AddPost(GlobalState state, string title, string slug, string date, string month)
    {
        state.PostSlugsInOrder.Add(slug);
        state.Index[slug] = new Post
        {
            Title = title,
            Slug = slug,
            Date = date,
            Month = month
        };
    }
}

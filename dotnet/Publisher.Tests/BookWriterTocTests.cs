using Publisher.Core.Models;
using Publisher.Core.Writers;

namespace Publisher.Tests;

public class BookWriterTocTests
{
    [Fact]
    public void Generate_WritesTocWithLinkedYearsBacklinksAndChronologicalPosts()
    {
        var outputPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Combine(outputPath, "EPUB"));

        try
        {
            var state = new GlobalState();
            state.Settings["output"] = outputPath;
            state.Settings["date_utc"] = "2026-07-26T00:00:00Z";

            // Add posts across multiple months and years, with deliberate date ordering
            AddPost(state, "Alpha 2020", "alpha-2020", "2020-01-20", "2020-01");
            AddPost(state, "Beta 2020", "beta-2020", "2020-01-10", "2020-01");
            AddPost(state, "Gamma 2020", "gamma-2020", "2020-02-05", "2020-02");
            AddPost(state, "Delta 2021", "delta-2021", "2021-03-15", "2021-03");

            new BookWriter(state).Generate();

            var tocPath = Path.Combine(outputPath, "EPUB", "toc.xhtml");
            Assert.True(File.Exists(tocPath));

            var toc = File.ReadAllText(tocPath);

            // Month grid at the top has year as link with anchor
            Assert.Contains("<a id=\"toc_2020\" href=\"#_2020\">2020</a>", toc);
            Assert.Contains("<a id=\"toc_2021\" href=\"#_2021\">2021</a>", toc);
            Assert.Contains("<a href=\"_202001.xhtml\"> 01 </a>", toc);
            Assert.Contains("<a href=\"_202002.xhtml\"> 02 </a>", toc);
            Assert.Contains("<a href=\"_202103.xhtml\"> 03 </a>", toc);

            // Year headings with backlink to the top grid
            Assert.Contains("<h2 id=\"_2020\" class=\"toc-year\"><a href=\"#toc_2020\">2020</a> <a href=\"#toc_2020\"><em>&lt;</em></a></h2>", toc);
            Assert.Contains("<h2 id=\"_2021\" class=\"toc-year\"><a href=\"#toc_2021\">2021</a> <a href=\"#toc_2021\"><em>&lt;</em></a></h2>", toc);

            // Month subheaders
            Assert.Contains("<h3 class=\"toc-month\"><a href=\"_202001.xhtml\">01</a></h3>", toc);
            Assert.Contains("<h3 class=\"toc-month\"><a href=\"_202002.xhtml\">02</a></h3>", toc);
            Assert.Contains("<h3 class=\"toc-month\"><a href=\"_202103.xhtml\">03</a></h3>", toc);

            // Chronological post listing in 2020-01: Beta (2020-01-10) should precede Alpha (2020-01-20)
            var betaIndex = toc.IndexOf("<a href=\"_202001.xhtml#_beta_2020\">Beta 2020</a>");
            var alphaIndex = toc.IndexOf("<a href=\"_202001.xhtml#_alpha_2020\">Alpha 2020</a>");
            Assert.True(betaIndex > 0, "Beta 2020 link should exist");
            Assert.True(alphaIndex > 0, "Alpha 2020 link should exist");
            Assert.True(betaIndex < alphaIndex, "Posts within month should be ordered chronologically by date");

            // Gamma in 2020-02
            Assert.Contains("<a href=\"_202002.xhtml#_gamma_2020\">Gamma 2020</a>", toc);

            // Delta in 2021-03
            Assert.Contains("<a href=\"_202103.xhtml#_delta_2021\">Delta 2021</a>", toc);
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

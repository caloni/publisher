using Publisher.Core.Models;
using Publisher.Core.Writers;

namespace Publisher.Tests;

public class BookWriterCoverTests
{
    [Fact]
    public void Generate_WritesAnImageCoverAndLegacyCoverMetadata()
    {
        var outputPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Combine(outputPath, "EPUB"));

        try
        {
            var state = new GlobalState();
            state.Settings["output"] = outputPath;
            state.Settings["date_utc"] = "2026-07-26T00:00:00Z";

            new BookWriter(state).Generate();

            var cover = File.ReadAllText(Path.Combine(outputPath, "EPUB", "cover.xhtml"));
            var package = File.ReadAllText(Path.Combine(outputPath, "EPUB", "package.opf"));

            Assert.Contains("<img class=\"cover-image\" src=\"img/cover.jpg\"", cover);
            Assert.Contains("epub:type=\"cover\"", cover);
            Assert.Contains("<meta name=\"cover\" content=\"cover-image\"/>", package);
            Assert.Contains("<reference type=\"cover\" title=\"Cover\" href=\"cover.xhtml\"/>", package);
        }
        finally
        {
            Directory.Delete(outputPath, recursive: true);
        }
    }
}

using System.IO;
using System.IO.Compression;

namespace Publisher.Core.Writers
{
    public class EpubPackager
    {
        public static void CreateEpub(string sourceDir, string outputFile)
        {
            if (File.Exists(outputFile))
                File.Delete(outputFile);
                
            // First, write mimetype uncompressed
            using (var archive = ZipFile.Open(outputFile, ZipArchiveMode.Create))
            {
                var mimetypeEntry = archive.CreateEntry("mimetype", CompressionLevel.NoCompression);
                using (var writer = new StreamWriter(mimetypeEntry.Open()))
                {
                    writer.Write("application/epub+zip");
                }
            }
            
            // Then add the rest with compression
            using (var archive = ZipFile.Open(outputFile, ZipArchiveMode.Update))
            {
                AddDirectoryToArchive(archive, Path.Combine(sourceDir, "EPUB"), "EPUB");
                AddDirectoryToArchive(archive, Path.Combine(sourceDir, "META-INF"), "META-INF");
            }
        }
        
        private static void AddDirectoryToArchive(ZipArchive archive, string sourceDir, string entryPrefix)
        {
            foreach (var file in Directory.GetFiles(sourceDir, "*", SearchOption.AllDirectories))
            {
                var relativePath = Path.GetRelativePath(sourceDir, file).Replace("\\", "/");
                var entryName = Path.Combine(entryPrefix, relativePath).Replace("\\", "/");
                archive.CreateEntryFromFile(file, entryName, CompressionLevel.Optimal);
            }
        }
    }
}
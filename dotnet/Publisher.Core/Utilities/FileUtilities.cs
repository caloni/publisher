using System;
using System.Collections.Generic;
using System.IO;

namespace Publisher.Core.Utilities
{
    /// <summary>
    /// Utility methods for file operations (copying static assets, etc.)
    /// </summary>
    public static class FileUtilities
    {
        /// <summary>
        /// Copies static files (CSS, JS, images, etc.) to the output directory
        /// Mirrors functionality from journal2blog.py, journal2journal.py, and journal2book.py
        /// </summary>
        public static void CopyStaticFiles(string outputPath, string staticPath, string mode = "journal")
        {
            Console.WriteLine($"Copying static files to {staticPath}...");

            if (mode == "book")
            {
                // For book mode, copy the entire publisher/book directory structure
                var bookSrc = Path.Combine("publish", "book");
                if (Directory.Exists(bookSrc))
                {
                    CopyDirectory(bookSrc, outputPath, true);
                    Console.WriteLine($"  Copied book template from {bookSrc}");
                }
                else
                {
                    Console.WriteLine($"  Warning: {bookSrc} not found, skipping book template");
                }

                // Copy blog images to EPUB/img (additional content)
                var imgBlogSrc = Path.Combine("img");
                var imgBlogDst = Path.Combine(outputPath, "EPUB", "img");
                if (Directory.Exists(imgBlogSrc))
                {
                    CopyDirectory(imgBlogSrc, imgBlogDst, true);
                    Console.WriteLine($"  Copied blog images from {imgBlogSrc}");
                }
                else
                {
                    Console.WriteLine($"  Warning: {imgBlogSrc} not found, skipping blog images");
                }
            }
            else
            {
                // For blog/journal mode
                var staticFilesToCopy = new List<string>
                {
                    "archives",
                    "css",
                    "js",
                    "img",
                    "fonts",
                    "resume.pdf",
                    "_months.html",
                    "BlogServer.py"
                };

                foreach (var item in staticFilesToCopy)
                {
                    var src = Path.Combine("publish", "blog", item);
                    var dst = Path.Combine(staticPath, item);

                    if (File.Exists(src))
                    {
                        // Copy file
                        var dstDir = Path.GetDirectoryName(dst);
                        if (!string.IsNullOrEmpty(dstDir) && !Directory.Exists(dstDir))
                        {
                            Directory.CreateDirectory(dstDir);
                        }

                        if (File.Exists(dst))
                        {
                            File.Delete(dst);
                        }

                        File.Copy(src, dst, true);
                        Console.WriteLine($"  Copied file: {item}");
                    }
                    else if (Directory.Exists(src))
                    {
                        // Copy directory recursively
                        CopyDirectory(src, dst, true);
                        Console.WriteLine($"  Copied directory: {item}");
                    }
                    else
                    {
                        Console.WriteLine($"  Warning: {src} not found, skipping");
                    }
                }

                // Copy blog images to static path
                var imgSrc = Path.Combine("img");
                var imgDst = Path.Combine(staticPath, "img");

                if (Directory.Exists(imgSrc))
                {
                    CopyDirectory(imgSrc, imgDst, true);
                    Console.WriteLine($"  Copied blog images from {imgSrc}");
                }
                else
                {
                    Console.WriteLine($"  Warning: {imgSrc} not found, skipping blog images");
                }
            }

            Console.WriteLine("Static files copied successfully!");
            Console.WriteLine();
        }

        /// <summary>
        /// Cleans the output directory before generating new files
        /// Mirrors the cleanup logic from journal2blog.py, journal2journal.py, and journal2book.py
        /// </summary>
        public static void CleanOutputDirectory(string outputPath)
        {
            if (!Directory.Exists(outputPath))
            {
                return;
            }

            Console.WriteLine($"Cleaning output directory: {outputPath}...");

            foreach (var file in Directory.GetFiles(outputPath))
            {
                try
                {
                    File.Delete(file);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"  Warning: Failed to delete {file}. Reason: {ex.Message}");
                }
            }

            foreach (var dir in Directory.GetDirectories(outputPath))
            {
                // Don't delete .git directory if present
                if (Path.GetFileName(dir) == ".git")
                {
                    continue;
                }

                try
                {
                    Directory.Delete(dir, true);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"  Warning: Failed to delete {dir}. Reason: {ex.Message}");
                }
            }

            Console.WriteLine("Output directory cleaned!");
            Console.WriteLine();
        }

        /// <summary>
        /// Recursively copies a directory and all its contents
        /// </summary>
        private static void CopyDirectory(string sourceDir, string destDir, bool overwrite)
        {
            // Create destination directory if it doesn't exist
            if (!Directory.Exists(destDir))
            {
                Directory.CreateDirectory(destDir);
            }

            // Copy all files
            foreach (var file in Directory.GetFiles(sourceDir))
            {
                var fileName = Path.GetFileName(file);
                var destFile = Path.Combine(destDir, fileName);
                File.Copy(file, destFile, overwrite);
            }

            // Recursively copy subdirectories
            foreach (var dir in Directory.GetDirectories(sourceDir))
            {
                var dirName = Path.GetFileName(dir);
                var destSubDir = Path.Combine(destDir, dirName);
                CopyDirectory(dir, destSubDir, overwrite);
            }
        }
    }
}

using Publisher.Core.Models;
using Publisher.Core.Parsers;
using Publisher.Core.Pipeline;
using Publisher.Core.Writers;
using Publisher.Core.Utilities;

namespace Publisher.Console
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                // Parse command line arguments
                var arguments = ParseArguments(args);
                
                var mode = GetArgumentValue(arguments, "--mode", "journal");
                var basePath = GetArgumentValue(arguments, "--base-path", "/journal");
                var outputPath = GetArgumentValue(arguments, "--output-path", @"publisher\public\journal");
                var journalPath = GetArgumentValue(arguments, "--journal-path", @"journal.md");
                var privateJournalPath = GetArgumentValue(arguments, "--private-journal-path", @"private\journal.md");
                var includePrivate = GetArgumentValue(arguments, "--private", "false").ToLower() == "true";
                var singlePostMode = GetArgumentValue(arguments, "--single-post-mode", "0") == "1";
                var commentEmail = GetArgumentValue(arguments, "--comment-email", "");
                var statsInlineLinks = arguments.ContainsKey("--stats-inline-links");
                var useTemplates = GetArgumentValue(arguments, "--use-templates", "0") == "1";
                var linksPath = GetArgumentValue(arguments, "--links", "");
                var authorInHeader = GetArgumentValue(arguments, "--author-in-header", "0") == "1";
                
                // Create global state
                var state = new GlobalState();
                state.Settings["mode"] = mode;

                if( mode == "blog" )
                {
                    state.Settings["title"] = "Blogue do Caloni";
                    state.Settings["has_tags_pages"] = "0"; // TODO change this to test tags implementation for blog
                }
                else if (mode == "journal")
                {
                    state.Settings["title"] = "Jornal do Caloni";
                    state.Settings["has_tags_pages"] = "1";
                }

                System.Console.WriteLine($"Publisher v1.0");
                System.Console.WriteLine($"Mode: {mode}");
                System.Console.WriteLine($"Base Path: {basePath}");
                System.Console.WriteLine($"Output Path: {outputPath}");
                System.Console.WriteLine($"Journal Path: {journalPath}");
                System.Console.WriteLine($"Include Private: {includePrivate}");
                if (includePrivate)
                {
                    System.Console.WriteLine($"Private Journal Path: {privateJournalPath}");
                }
                System.Console.WriteLine($"Single Post Mode: {singlePostMode}");
                System.Console.WriteLine($"Comment Email: {commentEmail}");
                if (statsInlineLinks)
                    System.Console.WriteLine("Stats: inline links enabled");
                System.Console.WriteLine($"Author in Header: {authorInHeader}");
                if (!string.IsNullOrEmpty(linksPath))
                    System.Console.WriteLine($"Links: {linksPath}");
                System.Console.WriteLine();

                // Set metadata from arguments
                state.Settings["base_path"] = basePath;
                state.Settings["output"] = outputPath;
                state.Settings["single_post_mode"] = singlePostMode ? "1" : "0";
                state.Settings["comment_email"] = commentEmail;
                state.Settings["stats_inline_links"] = statsInlineLinks ? "1" : "0";
                state.Settings["author_in_header"] = authorInHeader ? "1" : "0";
                state.Settings["date"] = DateTime.Now.ToString("yyyy-MM-ddTHH:mm:sszzz");
                state.Settings["build"] = GetGitCommitHash();

                // Copy new output
                {
                    // Clean and prepare output directory
                    FileUtilities.CleanOutputDirectory(outputPath);

                    // Ensure output directory exists
                    if (!Directory.Exists(outputPath))
                    {
                        Directory.CreateDirectory(outputPath);
                    }

                    // Copy static files (CSS, JS, images, etc.) before generating HTML
                    var staticPath = outputPath; // Same as Python scripts
                    FileUtilities.CopyStaticFiles(outputPath, staticPath, mode);
                }

                // Build the source file list
                var sourceFiles = new List<string> { journalPath };
                if (includePrivate)
                {
                    if (File.Exists(privateJournalPath))
                        sourceFiles.Add(privateJournalPath);
                    else
                        System.Console.WriteLine($"Warning: Private journal not found at {privateJournalPath}");
                }

                System.Console.WriteLine($"Parsing {string.Join(" + ", sourceFiles)}...");

                // Execute the pipeline
                var pipeline = new PublishPipeline(state)
                    .LoadResources(linksPath)
                    .Parse(sourceFiles.ToArray());

                if (!string.IsNullOrEmpty(linksPath) && File.Exists(linksPath))
                    System.Console.WriteLine($"Loaded {state.Links.Count} link(s) from {linksPath}");

                System.Console.WriteLine($"Parsed {state.Settings["totalPosts"]} posts");
                System.Console.WriteLine($"Found {state.Months.Count} months");
                System.Console.WriteLine($"Found {state.SlugsByTagsAndDates.Count} tags");
                System.Console.WriteLine();

                // Generate output
                if (mode == "blog" || mode == "journal")
                {
                    System.Console.WriteLine("Generating blog/journal HTML...");
                    pipeline.Write(new BlogWriter(state));
                    System.Console.WriteLine("Blog/journal generation complete!");
                }
                else if (mode == "book")
                {
                    state.Settings["date_utc"] = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ");
                    System.Console.WriteLine("Generating ebook...");
                    pipeline.WriteBook();

                    var epubOutput = Path.Combine(outputPath, "caloni.epub");
                    EpubPackager.CreateEpub(outputPath, epubOutput);
                    System.Console.WriteLine("Book generation complete!");
                }
                else
                {
                    System.Console.WriteLine($"Unknown mode: {mode}");
                    System.Console.WriteLine("Valid modes: blog, journal, book");
                    return;
                }

                System.Console.WriteLine();
                System.Console.WriteLine("Done!");
            }
            catch (Exception ex)
            {
                System.Console.WriteLine($"Error: {ex.Message}");
                System.Console.WriteLine(ex.StackTrace);
                Environment.Exit(1);
            }
        }

        static Dictionary<string, string> ParseArguments(string[] args)
        {
            var arguments = new Dictionary<string, string>();
            
            for (int i = 0; i < args.Length; i++)
            {
                if (args[i].StartsWith("--"))
                {
                    var key = args[i];
                    
                    // Check if this is a flag (no value) or has a value
                    if (i + 1 < args.Length && !args[i + 1].StartsWith("--"))
                    {
                        var value = args[i + 1];
                        arguments[key] = value;
                        i++; // Skip the next element since we used it as a value
                    }
                    else
                    {
                        // Flag without value (e.g., --private)
                        arguments[key] = "true";
                    }
                }
            }
            
            return arguments;
        }

        static string GetArgumentValue(Dictionary<string, string> arguments, string key, string defaultValue)
        {
            return arguments.TryGetValue(key, out var value) ? value : defaultValue;
        }

        static string GetGitCommitHash()
        {
            try
            {
                var process = new System.Diagnostics.Process();
                process.StartInfo.FileName = "git";
                process.StartInfo.Arguments = "rev-parse --short HEAD";
                process.StartInfo.RedirectStandardOutput = true;
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.CreateNoWindow = true;
                process.Start();
                
                var output = process.StandardOutput.ReadToEnd().Trim();
                process.WaitForExit();
                
                return output;
            }
            catch
            {
                return "unknown";
            }
        }
    }
}

using Markdig;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;
using Publisher.Core.Models;
using Publisher.Core.Utilities;
using System.Text.RegularExpressions;

namespace Publisher.Core.Parsers
{
    /// <summary>
    /// CommonMark-compliant parser using Markdig while preserving custom journal features
    /// </summary>
    public class CommonMarkParser : IParser
    {
        private readonly GlobalState _state;
        private readonly MarkdownPipeline _pipeline;

        public CommonMarkParser(GlobalState state)
        {
            _state = state;

            // Configure Markdig pipeline with specific extensions (excluding AutoLinks for compatibility)
            _pipeline = new MarkdownPipelineBuilder()
                // .UseEmphasisExtras() // Bold, italic, strikethrough, subscript, superscript, etc.
                //.UsePipeTables() // GitHub-flavored tables
                .UseTaskLists() // [ ] and [x] checkboxes
                .UseFootnotes() // Footnote support
                .UseGridTables() // Grid-style tables
                //.UseListExtras() // Extra list features avoid M. Smith paragraphs be parsed as ordered list item
                .UseDefinitionLists() // Definition lists
                .UseCitations() // Citation support
                .UseCustomContainers() // Custom containers
                // .UseGenericAttributes() // Generic attributes, removed because of { text } being interpreted as link (not standard)
                .UseMathematics() // Math support
                .UseMediaLinks() // Media links
                 // .UseSmartyPants() // Smart quotes, dashes, ellipses
                .UseAutoIdentifiers() // Auto-generate heading IDs
                .UseAbbreviations() // Abbreviation definitions
                // .UseAutoLinks() // <-- EXPLICITLY OMIT THIS to prevent www.example.com auto-linking
                .Build();

            // Initialize settings
            _state.Settings["generator"] = "https://github.com/caloni/journal";
            _state.Settings["public_repo_github_address"] = "https://github.com/caloni/journal/tree";
            _state.Settings["post_header_fields"] = "date link tags";
        }

        public void ParseFile(string filePath, bool isPrivate)
        {
            var markdown = File.ReadAllText(filePath);
            var document = Markdown.Parse(markdown, _pipeline);

            var posts = SplitIntoPosts(document, markdown, isPrivate);

            foreach (var post in posts)
                ProcessPost(post, isPrivate);

            PopulateTagNavigation();
        }

        public void ParseFiles(params string[] filePaths)
        {
            foreach (var filePath in filePaths)
            {
                if (!File.Exists(filePath))
                {
                    Console.WriteLine($"Warning: File not found: {filePath}");
                    continue;
                }

                bool isPrivate = filePath.Contains("private");
                ParseFileInternal(filePath, isPrivate);
            }

            PopulateTagNavigation();
        }

        private void ParseFileInternal(string filePath, bool isPrivate)
        {
            var markdown = File.ReadAllText(filePath);
            var document = Markdown.Parse(markdown, _pipeline);

            foreach (var post in SplitIntoPosts(document, markdown, isPrivate))
                ProcessPost(post, isPrivate);
        }

        private List<PostData> SplitIntoPosts(MarkdownDocument document, string sourceMarkdown, bool isPrivate)
        {
            var posts = new List<PostData>();
            var lines = sourceMarkdown.Split('\n');

            // First pass: collect all LinkReferenceDefinitions with their positions
            var linkDefinitions = new Dictionary<int, (string label, string url)>();
            foreach (var linkDef in document.GetLinkReferenceDefinitions(false))
            {
                if (linkDef is LinkReferenceDefinition linkRefDef)
                {
                    linkDefinitions[linkRefDef.Line] = (linkRefDef.Label ?? "", linkRefDef.Url ?? "");
                }
            }
            
            List<Block>? currentBlocks = null;
            PostData? currentPost = null;
            int postStartLine = 0;
            Block? metadataBlock = null;

            foreach (var block in document)
            {
                if (block is HeadingBlock heading && heading.Level == 1)
                {
                    var title = GetInlineContent(heading.Inline, "heading");
                            // Save previous post
                    if (currentPost != null && currentBlocks != null)
                    {
                        // Assign link definitions that belong to this post's range
                        AssignLinkDefinitionsToPost(currentPost, linkDefinitions, postStartLine, heading.Line - 1);
                        currentPost.Blocks = currentBlocks;
                        posts.Add(currentPost);
                    }

                    // Start new post
                    currentPost = new PostData
                    {
                        Title = GetInlineContent(heading.Inline, "heading"),
                        CustomFields = new Dictionary<string, string>(),
                        IsPrivate = isPrivate,
                        LinkDefinitions = new Dictionary<string, string>()
                    };
                    currentPost.Slug = StringUtilities.TitleToSlug(currentPost.Title);
                    currentBlocks = new List<Block>();
                    postStartLine = heading.Line;
                    
                    // Check if next block is paragraph with custom fields
                    var nextBlock = GetNextBlock(document, block);
                    if (nextBlock is ParagraphBlock paragraph && IsMetadataBlock(paragraph, lines))
                    {
                        ExtractCustomFields(paragraph, lines, currentPost);
                        metadataBlock = nextBlock; // Mark this block to skip it later
                    }
                    else
                    {
                        metadataBlock = null;
                    }
                }
                else if (currentBlocks != null && currentPost != null)
                {
                    // Add block to current post (skip the metadata paragraph if it was detected)
                    if (block != metadataBlock)
                    {
                        currentBlocks.Add(block);
                    }
                }
            }

            // Add last post
            if (currentPost != null && currentBlocks != null)
            {
                // Assign remaining link definitions to last post
                AssignLinkDefinitionsToPost(currentPost, linkDefinitions, postStartLine, lines.Length - 1);
                currentPost.Blocks = currentBlocks;
                posts.Add(currentPost);
            }

            return posts;
        }

        private void ExtractCustomFields(ParagraphBlock paragraph, string[] lines, PostData post)
        {
            if (paragraph.Line >= 0 && paragraph.Line < lines.Length)
            {
                var line = lines[paragraph.Line].Trim();
                var match = Regex.Match(line, @"^(\d{4}-\d{2}-\d{2})(\s+(.+))?$");
                if (match.Success)
                {
                    post.CustomFields["date"] = match.Groups[1].Value;
                    if (match.Groups[3].Success && !string.IsNullOrWhiteSpace(match.Groups[3].Value))
                    {
                        post.CustomFields["tags"] = match.Groups[3].Value.Trim();
                    }
                }
            }
        }

        private bool IsMetadataBlock(Block block, string[] lines)
        {
            if (block is not ParagraphBlock para) return false;
            if (para.Line < 0 || para.Line >= lines.Length) return false;

            var line = lines[para.Line].Trim();

            // Check if the line matches metadata pattern: YYYY-MM-DD followed by optional tags
            return Regex.IsMatch(line, @"^\d{4}-\d{2}-\d{2}(\s+.*)?$");
        }

        private Block? GetNextBlock(MarkdownDocument document, Block currentBlock)
        {
            var blocks = document.ToList();
            var index = blocks.IndexOf(currentBlock);
            return index >= 0 && index < blocks.Count - 1 ? blocks[index + 1] : null;
        }

        private void ProcessPost(PostData postData, bool isPrivate)
        {
            var post = new Post
            {
                Title = postData.Title,
                Slug = postData.Slug,
                Links = new Dictionary<string, string>()
            };

            // Extract standard fields
            if (postData.CustomFields.TryGetValue("date", out var date))
            {
                post.Date = date;
                post.Month = date.Substring(0, 7); // YYYY-MM
            }

            if (postData.CustomFields.TryGetValue("tags", out var tags))
            {
                post.Tags = tags.Split(' ', StringSplitOptions.RemoveEmptyEntries).ToList();
            }

            if (postData.CustomFields.TryGetValue("link", out var link))
            {
                post.ExternalLink = link;
            }

            // Override with YAML post links if available (takes precedence over inline "link:" field)
            if (!string.IsNullOrEmpty(postData.Title) &&
                _state.Links.TryGetValue(postData.Title, out var yamlLink) &&
                LinkResource.IsUrl(yamlLink))
            {
                post.ExternalLink = yamlLink;
                post.ExternalLinkFromYaml = true;
            }

            // Process blocks into PostBlocks, passing link definitions for resolution
            foreach (var block in postData.Blocks)
            {
                ProcessBlock(block, post, postData.LinkDefinitions);
            }

            // Extract internal links using post-scoped link definitions
            ExtractInternalLinks(postData.Blocks, post, postData.LinkDefinitions);

            // Preserve original definition order for the passthrough writer
            foreach (var kvp in postData.LinkDefinitionOrder)
                post.LinkOrder[kvp.Key] = kvp.Value;

            RegisterPost(post, isPrivate);
        }

        private void ProcessBlock(Block block, Post post, Dictionary<string, string> linkDefinitions)
        {
            switch (block)
            {
                case HeadingBlock heading when heading.Level >= 2 && heading.Level <= 6:
                    var headingKind = (BlockKind)(BlockKind.Heading2 + (heading.Level - 2));
                    post.Blocks.Add(new PostBlock
                    {
                        Kind = headingKind,
                        Content = GetInlineContent(heading.Inline, $"h{heading.Level}", linkDefinitions)
                    });
                    break;

                case ParagraphBlock paragraph:
                    // Paragraph containing only an image
                    if (paragraph.Inline != null && ContainsOnlyImage(paragraph.Inline, out var imageUrl, out var imageAlt))
                    {
                        post.Blocks.Add(new PostBlock
                        {
                            Kind = BlockKind.Image,
                            Content = imageUrl,
                            Meta = imageAlt
                        });

                        if (string.IsNullOrEmpty(post.Image))
                            post.Image = imageUrl;

                        if (!string.IsNullOrEmpty(imageUrl))
                            _state.PostsImages[imageUrl] = imageUrl;
                    }
                    // Fully-quoted paragraph that maps to an image via YAML
                    else if (paragraph.Inline != null && IsExternalImageParagraph(paragraph.Inline, out var extImageDesc)
                             && _state.Links.TryGetValue(extImageDesc, out var extImagePath)
                             && LinkResource.IsImagePath(extImagePath))
                    {
                        var imagePath = Path.GetFileName(extImagePath);

                        post.Blocks.Add(new PostBlock
                        {
                            Kind = BlockKind.Image,
                            Content = imagePath,
                            Meta = extImageDesc
                        });

                        if (string.IsNullOrEmpty(post.Image))
                            post.Image = imagePath;

                        _state.PostsImages[imagePath] = imagePath;
                    }
                    else
                    {
                        var content = GetInlineContent(paragraph.Inline, "p", linkDefinitions);
                        if (!string.IsNullOrWhiteSpace(content))
                        {
                            post.Blocks.Add(new PostBlock
                            {
                                Kind = BlockKind.Paragraph,
                                Content = content
                            });

                            if ((post.Summary?.Length ?? 0) < 200)
                                post.Summary = (post.Summary ?? "") + " " + content;
                        }
                    }
                    break;

                case CodeBlock codeBlock:
                    var code = codeBlock.Lines.ToString();
                    var lang = (codeBlock is Markdig.Syntax.FencedCodeBlock fenced) ? fenced.Info : null;
                    post.Blocks.Add(new PostBlock
                    {
                        Kind = BlockKind.Code,
                        Content = code,
                        Meta = string.IsNullOrEmpty(lang) ? null : lang
                    });
                    break;

                case QuoteBlock quote:
                    foreach (var subBlock in quote)
                    {
                        if (subBlock is ParagraphBlock quotePara)
                        {
                            post.Blocks.Add(new PostBlock
                            {
                                Kind = BlockKind.Blockquote,
                                Content = GetInlineContent(quotePara.Inline, "blockquote", linkDefinitions)
                            });
                        }
                        else if (subBlock is HeadingBlock quoteHeading)
                        {
                            // Headings inside blockquotes are preserved as blockquote text
                            // with their Markdown prefix to maintain source fidelity.
                            var headingPrefix = new string('#', quoteHeading.Level);
                            post.Blocks.Add(new PostBlock
                            {
                                Kind = BlockKind.Blockquote,
                                Content = $"{headingPrefix} {GetInlineContent(quoteHeading.Inline, $"h{quoteHeading.Level}", linkDefinitions)}"
                            });
                        }
                        else if (subBlock is ListBlock nestedList)
                        {
                            ProcessBlock(nestedList, post, linkDefinitions);
                        }
                    }
                    break;

                case ListBlock list:
                    int itemNumber = list.IsOrdered ? 1 : 0;
                    foreach (var item in list)
                    {
                        // Ordered list items are rendered as paragraphs with a numeric prefix
                        // to preserve source order without introducing a separate list structure.
                        var itemKind = list.IsOrdered ? BlockKind.Paragraph : BlockKind.UnorderedList;
                        if (item is ListItemBlock listItem)
                        {
                            foreach (var subBlock in listItem)
                            {
                                if (subBlock is ParagraphBlock listPara)
                                {
                                    var content = GetInlineContent(listPara.Inline, itemKind.HtmlTag(), linkDefinitions);

                                    if (list.IsOrdered)
                                        content = $"{itemNumber}. {content}";

                                    post.Blocks.Add(new PostBlock
                                    {
                                        Kind = itemKind,
                                        Content = content
                                    });
                                }
                                else if (subBlock is ListBlock nestedList)
                                {
                                    ProcessBlock(nestedList, post, linkDefinitions);
                                }
                            }

                            if (list.IsOrdered)
                                itemNumber++;
                        }
                    }
                    break;

                case Markdig.Syntax.ThematicBreakBlock:
                    post.Blocks.Add(new PostBlock { Kind = BlockKind.HorizontalRule, Content = "" });
                    break;
            }
        }

        private bool ContainsOnlyImage(ContainerInline? inline, out string imageUrl, out string imageAlt)
        {
            imageUrl = "";
            imageAlt = "";

            if (inline == null)
                return false;

            // Check if the paragraph contains a single image link
            var inlines = inline.ToList();

            // Should have exactly one inline element (the image)
            // Or may have whitespace before/after
            var nonWhitespaceInlines = inlines.Where(i => !(i is LiteralInline lit && string.IsNullOrWhiteSpace(lit.Content.ToString()))).ToList();

            if (nonWhitespaceInlines.Count == 1 && nonWhitespaceInlines[0] is LinkInline link && link.IsImage)
            {
                imageUrl = link.Url ?? "";

                // Extract just the filename from the URL if it's a path
                if (imageUrl.Contains("/"))
                {
                    imageUrl = Path.GetFileName(imageUrl);
                }

                imageAlt = GetInlineContent(link, "image");
                return true;
            }

            return false;
        }

        private bool IsExternalImageParagraph(ContainerInline? inline, out string description)
        {
            description = "";

            if (inline == null)
                return false;

            var inlines = inline.ToList();
            var nonWhitespace = inlines.Where(i => !(i is LiteralInline lit && string.IsNullOrWhiteSpace(lit.Content.ToString()))).ToList();

            if (nonWhitespace.Count != 1 || nonWhitespace[0] is not LiteralInline literal)
                return false;

            var text = literal.Content.ToString().Trim();
            if (text.Length >= 2 && text.StartsWith("\"") && text.EndsWith("\""))
            {
                description = text.Substring(1, text.Length - 2);
                return true;
            }

            return false;
        }

        // Only escape &, <, > to match MarkdownParser behavior
        // a better transformation would be to use System.Net.WebUtility.HtmlEncode,
        // but that would break compatibility with MarkdownParser which only
        // escapes these three characters
        private static string EscapeHtml(string text)
        {
            return text.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;");
        }

        // type is used to compatibility with MarkdownParser,
        // but we are going to stop using it here after
        // the perfect match
        private string GetInlineContent(ContainerInline? inline, string type, Dictionary<string, string>? linkDefinitions = null)
        {
            if (inline == null) return string.Empty;

            var content = new System.Text.StringBuilder();
            foreach (var child in inline)
            {
                if (child is LiteralInline literal)
                {
                    // Escape regardless of block type - a literal '<'/'>'/'&' in a heading,
                    // list item, or blockquote is just as invalid in the output XHTML as one
                    // in a paragraph (e.g. "N < M" inside a list item breaks EPUB validation).
                    var text = EscapeHtml(literal.Content.ToString());
                    content.Append(text);
                }
                /*
                we should avoid escape & character inside external links to compatibility to the original
                parser so that we could compare all files and they match. however, this is not feasible
                because to do that we need to extract LinkInline elements and avoid doing the already
                implemented internal references to other posts. and just to keep the match. we are going
                to comment this solution and do not talk about it and hope to find all other matches we
                need to fix. let's pray
                else if (child is LinkInline inlineLink)
                {
                    if (inlineLink.Url != null && inlineLink.Url.Contains("&"))
                    {
                        int x = 0;
                    }
                    else
                    {
                        content.Append($"<a href=\"{inlineLink.Url}\">{inlineLink.Label}</a>");
                    }
                }
                */
                else if (child is CodeInline code)
                {
                    content.Append($"<code>{EscapeHtml(code.Content)}</code>");
                }
                else if (child is AutolinkInline autolink)
                {
                    // Handle bare URLs (e.g., https://youtu.be/Fw1wdM_vzzI on its own line)
                    //var url = autolink.Url ?? "";
                    //content.Append($"<a href=\"{System.Net.WebUtility.HtmlEncode(url)}\">{System.Net.WebUtility.HtmlEncode(url)}</a>");
                    // we are going to keep to compatibility to MarkdownParser
                    var url = autolink.Url ?? "";
                    content.Append($"&lt;{EscapeHtml(url)}&gt;");
                }
                else if (child is LinkInline link)
                {
                    var url = link.Url ?? "";
                    var linkText = GetInlineContent(link, "link", linkDefinitions);

                    // If linkText is empty, use the URL itself (for standalone URLs)
                    if (string.IsNullOrWhiteSpace(linkText))
                    {
                        linkText = url;
                    }

                    // Check if this is a reference link that needs resolution
                    if (linkDefinitions != null && linkDefinitions.ContainsKey(linkText))
                    {
                        // This is a reference-style link - use the post-scoped definition
                        url = linkDefinitions[linkText];
                    }

                    if (Regex.IsMatch(url, @"^(https?)|(ftp)|(mailto):"))
                    {
                        // External link - convert to HTML anchor tag with proper encoding
                        var text = type == "blockquote" ? $"[{linkText}]"
                            : $"<a href=\"{System.Net.WebUtility.HtmlEncode(url)}\">{linkText}</a>";
                        content.Append(text);
                    }
                    else
                    {
                        // Internal link - keep as bracket notation for later processing
                        content.Append($"[{linkText}]");
                    }
                }
                else if (child is EmphasisInline emphasis)
                {
                    // Handle bold, italic, and strikethrough
                    var innerContent = GetInlineContent(emphasis, "emphasis");

                    // For now, maintain compatibility by keeping emphasis markers with content
                    string emphasisMarker = new string(emphasis.DelimiterChar, emphasis.DelimiterCount);
                    content.Append($"{emphasisMarker}{innerContent}{emphasisMarker}");

                    // Check delimiter character and count to determine formatting type
                    /*
                    if (emphasis.DelimiterChar == '~' && emphasis.DelimiterCount == 2)
                    {
                        // Strikethrough: ~~text~~
                        content.Append($"<del>{innerContent}</del>");
                    }
                    else if (emphasis.DelimiterCount == 2)
                    {
                        // Bold: **text** or __text__
                        content.Append($"<strong>{innerContent}</strong>");
                    }
                    else if (emphasis.DelimiterCount == 1)
                    {
                        // Italic: *text* or _text_
                        content.Append($"<em>{innerContent}</em>");
                    }
                    else
                    {
                        // Fallback for unknown emphasis types
                        content.Append(innerContent);
                    }
                    */
                }
                else if (child is Markdig.Extensions.Mathematics.MathInline mathInline)
                {
                    // Handle inline math expressions (e.g., $this text$)
                    // Keep the $ delimiters for compatibility/rendering
                    var delimiter = mathInline.DelimiterCount == 1 ? "$" : "$$";
                    content.Append($"{delimiter}{EscapeHtml(mathInline.Content.ToString())}{delimiter}");
                }
                else
                {
                    // Diagnostic: log unknown inline types
                    //System.Console.WriteLine($"Warning: Unknown inline type: {child.GetType().Name}.");
                }
            }
            return content.ToString().Trim();
        }

        private void ExtractInternalLinks(List<Block> blocks, Post post, Dictionary<string, string> linkDefinitions)
        {
            foreach (var block in blocks)
                ExtractLinksFromBlock(block, post, linkDefinitions);
        }

        private void ExtractLinksFromBlock(Block block, Post post, Dictionary<string, string> linkDefinitions)
        {
            if (block is LeafBlock leaf && leaf.Inline != null)
            {
                foreach (var inline in leaf.Inline)
                {
                    if (inline is LinkInline link)
                    {
                        var url = link.Url ?? "";
                        var text = GetInlineContent(link, "link", linkDefinitions);

                        if (linkDefinitions.ContainsKey(text))
                            url = linkDefinitions[text];

                        // Only register non-external links as internal references
                        if (!Regex.IsMatch(url, @"^(https?)|(ftp)|(mailto):"))
                        {
                            post.Links[text] = url;

                            if (_state.PostMetadata.ContainsKey(url))
                                _state.PostMetadata[url].Used++;
                        }
                    }
                }
            }
            else if (block is ContainerBlock container)
            {
                foreach (var subBlock in container)
                    ExtractLinksFromBlock(subBlock, post, linkDefinitions);
            }
        }

        /// <summary>
        /// Registers a fully-parsed post into the global state.
        /// Derives rendering metadata (link, letter, tag navigation scaffolding)
        /// and indexes the post by slug, title, date, and tags.
        /// </summary>
        private void RegisterPost(Post post, bool isPrivate)
        {
            var slug = post.Slug ?? "";
            if (string.IsNullOrEmpty(slug)) return;

            // Two posts can share the same title (and therefore the same slug).
            // Disambiguate later occurrences with their date so neither post's
            // anchor id/slug collides with (and silently overwrites) the other's.
            if (_state.Index.ContainsKey(slug))
            {
                var baseSlug = string.IsNullOrEmpty(post.Date) ? slug : $"{slug}-{post.Date}";
                var uniqueSlug = baseSlug;
                var suffix = 2;
                while (_state.Index.ContainsKey(uniqueSlug))
                    uniqueSlug = $"{baseSlug}-{suffix++}";

                slug = uniqueSlug;
                post.Slug = slug;
            }

            post.Link = post.Month + ".html#" + slug;
            _state.PostSlugsInOrder.Add(slug);

            if (!string.IsNullOrEmpty(post.ExternalLink))
                post.Tags.Add("blogging");

            if (isPrivate)
                post.Tags.Add("private");

            if (!string.IsNullOrEmpty(post.Date))
            {
                if (post.Month != null)
                    _state.Months[post.Month] = post.Month;

                if (!_state.DateSlugTitle.ContainsKey(post.Date))
                    _state.DateSlugTitle[post.Date] = new Dictionary<string, string>();
                _state.DateSlugTitle[post.Date][slug] = post.Title ?? "";

                foreach (var tag in post.Tags)
                {
                    if (!_state.SlugsByTagsAndDates.ContainsKey(tag))
                        _state.SlugsByTagsAndDates[tag] = new Dictionary<string, Dictionary<string, string>>();
                    if (!_state.SlugsByTagsAndDates[tag].ContainsKey(post.Date))
                        _state.SlugsByTagsAndDates[tag][post.Date] = new Dictionary<string, string>();
                    _state.SlugsByTagsAndDates[tag][post.Date][slug] = slug;
                }
            }

            var title = post.Title ?? "";
            post.Letter = title.Length > 0 ? title.Substring(0, 1) : "";

            if (!_state.PostMetadata.ContainsKey(slug))
                _state.PostMetadata[slug] = new PostMetadata { Chapter = post.Month };

            _state.Index[slug] = post;
            _state.TitleToSlug[title] = slug;

            if (!_state.TitleToSlugs.TryGetValue(title, out var slugsForTitle))
            {
                slugsForTitle = new List<string>();
                _state.TitleToSlugs[title] = slugsForTitle;
            }
            slugsForTitle.Add(slug);
            _state.Settings["totalPosts"] = _state.PostSlugsInOrder.Count.ToString();
        }

        /// <summary>
        /// Builds per-tag previous/next navigation links for all registered posts.
        /// Must be called after all source files have been parsed.
        /// </summary>
        private void PopulateTagNavigation()
        {
            foreach (var tag in _state.SlugsByTagsAndDates.Keys.OrderBy(k => k))
            {
                string? nextInTag = null;

                foreach (var date in _state.SlugsByTagsAndDates[tag].Keys.OrderByDescending(k => k))
                {
                    foreach (var slug in _state.SlugsByTagsAndDates[tag][date].Keys)
                    {
                        if (nextInTag != null)
                        {
                            if (!_state.Index[slug].TagNavigation.ContainsKey(tag))
                                _state.Index[slug].TagNavigation[tag] = new TagNavigation();
                            _state.Index[slug].TagNavigation[tag].NextInTag = nextInTag;

                            if (!_state.Index[nextInTag].TagNavigation.ContainsKey(tag))
                                _state.Index[nextInTag].TagNavigation[tag] = new TagNavigation();
                            _state.Index[nextInTag].TagNavigation[tag].PreviousInTag = slug;
                        }
                        nextInTag = slug;
                    }
                }
            }
        }

        private void AssignLinkDefinitionsToPost(
            PostData post,
            Dictionary<int, (string label, string url)> allLinkDefinitions,
            int postStartLine,
            int postEndLine)
        {
            int order = 0;
            foreach (var kvp in allLinkDefinitions.OrderBy(k => k.Key))
            {
                var (label, url) = kvp.Value;
                if (kvp.Key >= postStartLine && kvp.Key <= postEndLine)
                {
                    post.LinkDefinitions[label] = url;
                    post.LinkDefinitionOrder[label] = order++;
                }
            }
        }

        private class PostData
        {
            public string Title { get; set; } = "";
            public string Slug { get; set; } = "";
            public Dictionary<string, string> CustomFields { get; set; } = new();
            public List<Block> Blocks { get; set; } = new();
            public bool IsPrivate { get; set; }
            public Dictionary<string, string> LinkDefinitions { get; set; } = new();
            public Dictionary<string, int> LinkDefinitionOrder { get; set; } = new();
        }
    }
}
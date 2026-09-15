using Publisher.Core.Models;
using Publisher.Core.Utilities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;

namespace Publisher.Core.Writers
{
    /// <summary>
    /// Generates HTML blog/journal pages from the parsed journal state.
    /// </summary>
    public class BlogWriter : IBlogWriter
    {
        private readonly GlobalState _state;
        private readonly Dictionary<string, string> _files;
        private readonly Dictionary<string, Dictionary<string, string>> _postsByMonth;
        private readonly Dictionary<string, Dictionary<string, string>> _postLinksByMonth;
        private readonly Dictionary<string, string> _nextMonth;
        private readonly Dictionary<string, string> _prevMonth;
        private readonly Dictionary<string, string> _quickSearch;

        public BlogWriter(GlobalState state)
        {
            _state = state;
            _files = new Dictionary<string, string>();
            _postsByMonth = new Dictionary<string, Dictionary<string, string>>();
            _postLinksByMonth = new Dictionary<string, Dictionary<string, string>>();
            _nextMonth = new Dictionary<string, string>();
            _prevMonth = new Dictionary<string, string>();
            _quickSearch = new Dictionary<string, string>();
            
            InitializeSettings();
        }

        private void InitializeSettings()
        {
            if (!_state.Settings.ContainsKey("author"))
                _state.Settings["author"] = "Caloni";
            
            if (!_state.Settings.ContainsKey("base_path"))
                _state.Settings["base_path"] = "";
                
            if (!_state.Settings.ContainsKey("output"))
                _state.Settings["output"] = @"publisher\public\blog";
            
            if (!_state.Settings.ContainsKey("title"))
                _state.Settings["title"] = "Blogue do Caloni";

            if (!_state.Settings.ContainsKey("has_tags_pages"))
                _state.Settings["has_tags_pages"] = "0";

            if (!_state.Settings.ContainsKey("comment_email"))
                _state.Settings["comment_email"] = "";

            _state.Settings["description"] = "Write for computers, people and food.";
            _state.Settings["link"] = "http://www.caloni.com.br";
            _state.Settings["blog_home_link"] = "/blog";
            _state.Settings["blog_home_description"] = "o que foi revisado e publicado no blogue.";
            _state.Settings["text_favorite_tags"] = "draft computer cinema";
            _state.Settings["text_favorite_tags_draft"] = "textos germinais.";
            _state.Settings["text_favorite_tags_computer"] = "programação, depuração, transpiração.";
            _state.Settings["text_favorite_tags_cinema"] = "o finado Cine Tênis Verde veio parar aqui.";
        }

        public void Generate()
        {
            // Ensure output directory exists before generating any files
            var outputPath = _state.Settings["output"];
            if (!Directory.Exists(outputPath))
            {
                Directory.CreateDirectory(outputPath);
            }

            // Check if we're in single-post mode (blog mode)
            var singlePostMode = _state.Settings.ContainsKey("single_post_mode") && 
                                _state.Settings["single_post_mode"] == "1";

            if (singlePostMode)
            {
                // Blog mode: one post per page, filtered by 'blog' tag
                FlushBlogPosts();
                FlushBlogIndexPage();
                FlushNotFoundPage();

                // TODO need to use only blog posts in the tags pages construction
                // and tags navigation
                if (_state.Settings.GetValueOrDefault("has_tags_pages", "0") == "1")
                {
                    FlushTagsPage();
                    FlushTagsPages();
                }
            }
            else
            {
                // Journal mode: posts grouped by month
                FlushPosts();
                TiePreviousMonths(); // Link months before writing pages
                FlushPostsPage();
                FlushPostsPages();
                FlushTagsPage();
                FlushTagsPages();
                FlushMonthsPage();
                FlushIndexPage();
                FlushNotFoundPage();
            }
        }

        private void FlushPosts()
        {
            foreach (var slug in _state.PostSlugsInOrder)
            {
                if (string.IsNullOrEmpty(_state.Index[slug].Date))
                {
                    Console.WriteLine($"skipping {slug}");
                    continue;
                }
                WritePost(slug);
            }
        }

        private void WritePost(string slug)
        {
            var post = _state.Index[slug];
            var month = post.Month ?? "";
            if (string.IsNullOrEmpty(month))
                return;

            var outputPath = _state.Settings["output"];
            var filePath = Path.Combine(outputPath, $"{month}.html");
            var tags = post.Tags;

            // First post in the month
            if (!_files.ContainsKey(month))
            {
                WriteHead(filePath, $"{_state.Settings.GetValueOrDefault("text_page_prefix", "caloni")}::{month}", "months.html", false);
                _files[month] = month;
            }

            // Process post lines (mirrors BlogWriter_WritePost)
            var postText = new StringBuilder();
            ProcessPostLines(post);

            var title = post.Title ?? "";
            // Build post HTML
            postText.AppendLine($"<span id=\"{slug}\" title=\"{StringUtilities.TextToHtml(title)}\"/></span>");
            postText.AppendLine($"<section id=\"section_{slug}\">");
            
            // Title with link
            if (!string.IsNullOrEmpty(post.ExternalLink))
            {
                postText.AppendLine($"<p class=\"title\"><a href=\"{month}.html#{slug}\">#</a> <a class=\"external\" href=\"{post.ExternalLink}\">{StringUtilities.TextToHtml(title)}</a></p>");
            }
            else
            {
                postText.AppendLine($"<p class=\"title\"><a href=\"{month}.html#{slug}\">#</a> {StringUtilities.TextToHtml(title)}</p>");
            }
            
            var date = post.Date ?? "";
            // Author and date
            var authorInHeader = _state.Settings.ContainsKey("author_in_header") &&
                _state.Settings["author_in_header"] == "1";
            if (authorInHeader)
            {
                postText.Append($"<span class=\"title-heading\">{_state.Settings["author"]}, {date}");
            }
            else
            {
                postText.Append($"<span class=\"title-heading\">{date}");
            }

            // Tags linking to the exact post anchor in the tag page
            var hasTagsPages = _state.Settings.GetValueOrDefault("has_tags_pages", "1") == "1";
            if (hasTagsPages)
            {
                foreach (var tag in tags)
                {
                    postText.Append(" ");
                    postText.Append($"<a href=\"{tag}.html#{slug}\">{tag}</a>");
                }
            }
            
            postText.AppendLine($"<a href=\"{month}.html\"> <sup>[up]</sup></a> <a href=\"javascript:;\" onclick=\"copy_clipboard('section#section_{slug}')\"><sup>[copy]</sup></a></span>");
            postText.AppendLine();

            // Post content blocks
            foreach (var block in post.Blocks)
            {
                var content = block.Content ?? "";
                postText.Append(content);
                if (block.Kind != BlockKind.Code)
                    postText.AppendLine();
            }
            
            postText.AppendLine("</section><hr/>\n");

            // Append to month file
            if (!_postsByMonth.ContainsKey(month))
            {
                _postsByMonth[month] = new Dictionary<string, string>();
            }
            if (!_postsByMonth[month].ContainsKey(date))
            {
                _postsByMonth[month][date] = "";
            }
            _postsByMonth[month][date] += "\n" + postText.ToString();

            // Build post link for month page
            var postLink = $"<li><small><a href=\"{month}.html#{slug}\">{StringUtilities.TextToHtml(title)}</a></small></li>";
            if (!_postLinksByMonth.ContainsKey(month))
            {
                _postLinksByMonth[month] = new Dictionary<string, string>();
            }
            if (!_postLinksByMonth[month].ContainsKey(date))
            {
                _postLinksByMonth[month][date] = "";
            }
            _postLinksByMonth[month][date] += "\n" + postLink;

            // Add to quick search
            _quickSearch[slug] = $"{month}.html#{slug}";
        }

        private static bool IsMetaline(PostBlock block) => block.Kind == BlockKind.HorizontalRule;

        internal void ProcessPostLines(Post post)
        {
            var singlePostMode = _state.Settings.ContainsKey("single_post_mode") &&
                                _state.Settings["single_post_mode"] == "1";

            for (int i = 0; i < post.Blocks.Count; i++)
            {
                var block = post.Blocks[i];
                var content = block.Content ?? "";
                var prefix = "";
                var suffix = "";

                if (string.IsNullOrEmpty(content) && !IsMetaline(block))
                    continue;

                // Skip reference-style link definitions (already stored in post.Links)
                if (System.Text.RegularExpressions.Regex.IsMatch(content, @"^\[([^\]]+)\]:\s*(.+)$"))
                    continue;

                switch (block.Kind)
                {
                    case BlockKind.Code:
                        content = StringUtilities.TextToHtml(content) + "\n";
                        if (i == 0 || post.Blocks[i - 1].Kind != BlockKind.Code)
                            content = "<pre>\n" + content;
                        if (i == post.Blocks.Count - 1 || post.Blocks[i + 1].Kind != BlockKind.Code)
                            content = content + "</pre>\n";
                        break;

                    case BlockKind.Blockquote:
                        prefix = "<blockquote>";
                        suffix = "</blockquote>";
                        break;

                    case BlockKind.OrderedList:
                    case BlockKind.UnorderedList:
                        prefix = "<li>";
                        suffix = "</li>";
                        if (i == 0 || post.Blocks[i - 1].Kind != block.Kind)
                            prefix = (block.Kind == BlockKind.OrderedList ? "<ol>" : "<ul>") + prefix;
                        if (i == post.Blocks.Count - 1 || post.Blocks[i + 1].Kind != block.Kind)
                            suffix += block.Kind == BlockKind.OrderedList ? "</ol>" : "</ul>";
                        suffix += "\n";
                        break;

                    case BlockKind kind when kind.IsHeading():
                        var level = kind.HeadingLevel();
                        prefix = $"<h{level}>";
                        suffix = $"</h{level}>\n";
                        break;

                    case BlockKind.Paragraph:
                        prefix = "<p>";
                        suffix = "</p>\n";
                        break;

                    case BlockKind.Image:
                        var basePath = _state.Settings.GetValueOrDefault("base_path", "");
                        content = $"<img src=\"{basePath}/img/{content}\"/>\n";
                        prefix = "";
                        suffix = "";
                        break;

                    case BlockKind.HorizontalRule:
                        content = "<hr/>\n";
                        break;
                }

                // Resolve [linkText] references to HTML anchors
                if (block.Kind != BlockKind.Code && block.Kind != BlockKind.Blockquote)
                {
                    foreach (var link in post.Links)
                    {
                        var linkText = link.Key;
                        var linkTarget = link.Value;

                        if (_state.PostMetadata.ContainsKey(linkTarget))
                        {
                            var targetIsInBlog = singlePostMode &&
                                _state.Index.TryGetValue(linkTarget, out var targetPostForLink) &&
                                targetPostForLink.Tags.Contains("blog");

                            if (singlePostMode && targetIsInBlog)
                                linkTarget = $"<a href=\"{linkTarget}.html\">{linkText}</a>";
                            else if (singlePostMode)
                                linkTarget = linkText;
                            else if (_state.PostMetadata[linkTarget].Chapter != null)
                                linkTarget = $"<a href=\"{_state.PostMetadata[linkTarget].Chapter}.html#{linkTarget}\">{linkText}</a>";
                            else
                                linkTarget = $"<a href=\"{linkTarget}.html\">{linkText}</a>";
                        }
                        else if (!linkTarget.StartsWith("http") && !linkTarget.StartsWith("mailto:") && !linkTarget.StartsWith("ftp:"))
                        {
                            linkTarget = $"<a href=\"{linkTarget}.html\">{linkText}</a>";
                        }
                        else
                        {
                            linkTarget = $"<a href=\"{linkTarget}\">{linkText}</a>";
                        }

                        var pattern = $"\\[{System.Text.RegularExpressions.Regex.Escape(linkText)}\\]";
                        content = System.Text.RegularExpressions.Regex.Replace(content, pattern, linkTarget);
                    }
                }

                // Convert horizontal-rule markers that snuck through as paragraph text
                if (content.Trim() == "---")
                {
                    post.Blocks.Add(new PostBlock { Kind = BlockKind.HorizontalRule, Content = "" });
                    continue;
                }

                // Resolve "Quoted Text" to internal or external links
                if (block.Kind != BlockKind.Code)
                {
                    content = System.Text.RegularExpressions.Regex.Replace(
                        content,
                        @"""([^""]+)""",
                        m =>
                        {
                            var inner = m.Groups[1].Value;
                            var targetSlug = TitleResolver.Resolve(_state, inner, out var titleWarning);
                            if (titleWarning != null)
                                Console.WriteLine($"{titleWarning} (referenced from \"{post.Title}\")");
                            if (targetSlug != null)
                            {
                                if (singlePostMode)
                                {
                                    var targetIsInBlog =
                                        _state.Index.TryGetValue(targetSlug, out var targetPostForQuote) &&
                                        targetPostForQuote.Tags.Contains("blog");
                                    return targetIsInBlog ? $"<a href=\"{targetSlug}.html\">{inner}</a>" : inner;
                                }
                                string href;
                                if (_state.PostMetadata.ContainsKey(targetSlug) && _state.PostMetadata[targetSlug].Chapter != null)
                                    href = $"{_state.PostMetadata[targetSlug].Chapter}.html#{targetSlug}";
                                else
                                    href = $"{targetSlug}.html";
                                return $"<a href=\"{href}\">{inner}</a>";
                            }
                            if (_state.Links.TryGetValue(inner, out var externalUrl) && !LinkResource.IsImagePath(externalUrl))
                                return $"<a href=\"{System.Net.WebUtility.HtmlEncode(externalUrl)}\">{inner}</a>";
                            return m.Value;
                        });
                }

                post.Blocks[i].Content = prefix + content + suffix;
            }
        }

        private void FlushBlogPosts()
        {
            foreach (var slug in _state.PostSlugsInOrder)
            {
                if (string.IsNullOrEmpty(_state.Index[slug].Date))
                {
                    Console.WriteLine($"skipping {slug}");
                    continue;
                }
                WriteSinglePost(slug);
            }
        }

        private void WriteSinglePost(string slug)
        {
            var post = _state.Index[slug];
            var tags = post.Tags;

            if (!tags.Contains("blog"))
                return;

            var outputPath = _state.Settings["output"];
            var filePath = Path.Combine(outputPath, $"{slug}.html");

            string? prevSlug = null, nextSlug = null;
            var foundCurrent = false;

            foreach (var currentSlug in _state.PostSlugsInOrder)
            {
                var currentPost = _state.Index[currentSlug];

                if (string.IsNullOrEmpty(currentPost.Date))
                    continue;

                if (!currentPost.Tags.Contains("blog"))
                    continue;

                if (currentSlug == slug)
                {
                    foundCurrent = true;
                    continue;
                }

                if (foundCurrent)
                {
                    nextSlug = currentSlug;
                    break;
                }
                else
                {
                    prevSlug = currentSlug;
                }
            }

            var title = post.Title ?? "";
            WriteHead(filePath, StringUtilities.TextToHtml(title), "index.html", false);
            
            // Write post content (similar to WritePost but standalone page)
            var postText = new StringBuilder();
            ProcessPostLines(post);

            postText.AppendLine($"<span id=\"{slug}\" title=\"{StringUtilities.TextToHtml(title)}\"/></span>");
            postText.AppendLine($"<section id=\"section_{slug}\">");
            
            if (!string.IsNullOrEmpty(post.ExternalLink))
            {
                postText.AppendLine($"<p class=\"title\"><a class=\"external\" href=\"{post.ExternalLink}\">{StringUtilities.TextToHtml(title)}</a></p>");
            }
            else
            {
                postText.AppendLine($"<p class=\"title\">{StringUtilities.TextToHtml(title)}</p>");
            }
            
            var date = post.Date ?? "";
            // Author and date
            var authorInHeader = _state.Settings.ContainsKey("author_in_header") &&
                _state.Settings["author_in_header"] == "1";
            if (authorInHeader)
            {
                postText.Append($"<span class=\"title-heading\">{_state.Settings["author"]}, {date}");
            }
            else
            {
                postText.Append($"<span class=\"title-heading\">{date}");
            }
            
            // Tags with navigation for single post (blog mode doesn't have tag pages by default)
            var hasTagsPages = _state.Settings.GetValueOrDefault("has_tags_pages", "1") == "1";
            foreach (var tag in tags)
            {
                postText.Append(" ");
                
                if (hasTagsPages)
                {
                    // Previous post with same tag navigation
                    if (post.TagNavigation.ContainsKey(tag) && !string.IsNullOrEmpty(post.TagNavigation[tag].PreviousInTag))
                    {
                        var prevInTagSlug = post.TagNavigation[tag].PreviousInTag!;
                        postText.Append($"<a href=\"{prevInTagSlug}.html\">&lt;</a>");
                    }
                    
                    // Tag page link
                    postText.Append($"<a href=\"{tag}.html\">{tag}</a>");
                    
                    // Next post with same tag navigation
                    if (post.TagNavigation.ContainsKey(tag) && !string.IsNullOrEmpty(post.TagNavigation[tag].NextInTag))
                    {
                        var nextInTagSlug = post.TagNavigation[tag].NextInTag!;
                        postText.Append($"<a href=\"{nextInTagSlug}.html\">&gt;</a>");
                    }
                }
                else
                {
                    // Just display tag without link
                    postText.Append(tag);
                }
            }
            postText.AppendLine("</span>");
            postText.AppendLine();

            foreach (var block in post.Blocks)
            {
                var content = block.Content ?? "";
                postText.Append(content);
                if (block.Kind != BlockKind.Code)
                    postText.AppendLine();
            }

            postText.AppendLine("</section>");
            postText.AppendLine("");

            File.AppendAllText(filePath, postText.ToString(), System.Text.Encoding.UTF8);

            var nextLink = nextSlug != null ? $"{nextSlug}.html" : "";
            var prevLink = prevSlug != null ? $"{prevSlug}.html" : "";

            WriteBottom(filePath, false, nextLink, prevLink);
            _quickSearch[slug] = $"{slug}.html";
        }

        private void FlushBlogIndexPage()
        {
            var outputPath = _state.Settings["output"];
            var filePath = Path.Combine(outputPath, "index.html");
            
            WriteHead(filePath, _state.Settings["title"], "index.html", true, _quickSearch);
            
            var html = new StringBuilder();
            html.AppendLine("<table class=\"sortable\" style=\"width: 100%;\">");
            
            foreach (var date in _state.DateSlugTitle.Keys.OrderByDescending(k => k))
            {
                foreach (var slug in _state.DateSlugTitle[date].Keys)
                {
                    var post = _state.Index[slug];
                    var tags2 = post.Tags;

                    if (!tags2.Contains("blog"))
                        continue;

                    var tagsText = string.Join(" ", tags2);
                    html.AppendLine("<tr><td>");
                    
                    if (!string.IsNullOrEmpty(post.Image))
                    {
                        var basePath = _state.Settings.GetValueOrDefault("base_path", "");
                        html.AppendLine($"<img src=\"{basePath}/img/{post.Image}\"/>");
                    }
                    
                    var summary = post.Summary ?? "";
                    html.AppendLine($"<b><a href=\"{slug}.html\">{StringUtilities.TextToHtml(post.Title ?? "")}</a></b>");
                    html.AppendLine($"<small><i>{date} {tagsText} {summary}</small></i>");
                    html.AppendLine("</td></tr>");
                }
            }
            
            html.AppendLine("</table>");
            File.AppendAllText(filePath, html.ToString(), System.Text.Encoding.UTF8);
            
            WriteBottom(filePath, true, "", "", BuildFooterLink());
            _quickSearch["index"] = "index.html";
        }

        private void FlushPostsPage()
        {
            var outputPath = _state.Settings["output"];
            var filePath = Path.Combine(outputPath, "posts.html");

            WriteHead(filePath, $"{_state.Settings.GetValueOrDefault("text_page_prefix", "caloni")}::posts", "index.html", true);

            var html = new StringBuilder();

            foreach (var date in _state.DateSlugTitle.Keys.OrderByDescending(k => k))
            {
                foreach (var slug in _state.DateSlugTitle[date].Keys)
                {
                    var post = _state.Index[slug];
                    var title = _state.DateSlugTitle[date][slug];
                    var tags = post.Tags;
                    var tagsText = string.Join(" ", tags);

                    html.AppendLine("<tr><td>");

                    // Post image if exists
                    if (!string.IsNullOrEmpty(post.Image))
                    {
                        var basePath = _state.Settings.GetValueOrDefault("base_path", "");
                        html.AppendLine($"<img src=\"{basePath}/img/{post.Image}\"/>");
                    }

                    // Post title linking to month page with anchor
                    html.AppendLine($"<b><a href=\"{post.Month}.html#{slug}\">{StringUtilities.TextToHtml(title)}</a></b>");

                    // Date, tags, summary, and slug
                    html.AppendLine($"<small><i>{post.Date} {tagsText} {post.Summary} {slug}</small></i>");
                    html.AppendLine("</td></tr>");
                }
            }

            File.AppendAllText(filePath, html.ToString(), System.Text.Encoding.UTF8);
            WriteBottom(filePath, true);

            _quickSearch["posts"] = "posts.html";
        }

        private void FlushPostsPages()
        {
            // Write the collected post content and links to each month page
            foreach (var month in _files.Keys.OrderBy(k => k))
            {
                var filePath = Path.Combine(_state.Settings["output"], $"{month}.html");
                var html = new StringBuilder();
                
                // Write the post links (table of contents for the month)
                html.AppendLine("<ul style=\"list-style: none;\">");
                
                if (_postLinksByMonth.ContainsKey(month))
                {
                    foreach (var date in _postLinksByMonth[month].Keys.OrderBy(k => k))
                    {
                        html.Append(_postLinksByMonth[month][date]);
                    }
                }
                
                html.AppendLine("</ul>");
                
                // Write the actual post content
                if (_postsByMonth.ContainsKey(month))
                {
                    foreach (var date in _postsByMonth[month].Keys.OrderBy(k => k))
                    {
                        html.Append(_postsByMonth[month][date]);
                    }
                }
                
                File.AppendAllText(filePath, html.ToString(), System.Text.Encoding.UTF8);
                
                // Write bottom with navigation links
                var nextLink = _nextMonth.ContainsKey(month) ? $"{_nextMonth[month]}.html" : "";
                var prevLink = _prevMonth.ContainsKey(month) ? $"{_prevMonth[month]}.html" : "";
                
                WriteBottom(filePath, false, nextLink, prevLink);
            }
        }

        private void FlushTagsPage()
        {
            // Generate main tags.html page with all tags listing
            var outputPath = _state.Settings["output"];
            var filePath = Path.Combine(outputPath, "tags.html");
            
            WriteHead(filePath, $"{_state.Settings.GetValueOrDefault("text_page_prefix", "caloni")}::tags", "index.html", true);
            
            var html = new StringBuilder();
            
            // Tag buttons
            foreach (var tag in _state.SlugsByTagsAndDates.Keys.OrderBy(k => k))
            {
                html.AppendLine($"<button class=\"tagbutton\" style=\"font-size: 1rem;\" onclick=\"window.location = '{tag}.html';\">{StringUtilities.TextToHtml(tag)}</button>");
            }
            
            // Tag list with preview of post titles
            foreach (var tag in _state.SlugsByTagsAndDates.Keys.OrderBy(k => k))
            {
                var titleText = "";
                var titleMax = 0;
                
                // Get first 15 titles for this tag
                foreach (var date in _state.SlugsByTagsAndDates[tag].Keys.OrderByDescending(k => k))
                {
                    foreach (var slug in _state.SlugsByTagsAndDates[tag][date])
                    {
                        if (string.IsNullOrEmpty(titleText))
                        {
                            titleText = _state.Index[slug.Key].Title ?? "";
                        }
                        else
                        {
                            titleText += " - " + (_state.Index[slug.Key].Title ?? "");
                        }
                        titleMax++;
                        if (titleMax > 15)
                            break;
                    }
                    if (titleMax > 15)
                        break;
                }
                
                html.AppendLine("<tr><td><b><a href=\"" + tag + ".html\">" + StringUtilities.TextToHtml(tag) + "</a></b>");
                html.AppendLine("<small><i>" + titleText + "</small></i>");
                html.AppendLine("</td></tr>");
            }
            
            File.AppendAllText(filePath, html.ToString(), System.Text.Encoding.UTF8);
            WriteBottom(filePath, true);
            
            _quickSearch["tags"] = "tags.html";
        }

        private void FlushTagsPages()
        {
            var singlePostMode = _state.Settings.ContainsKey("single_post_mode") &&
                                _state.Settings["single_post_mode"] == "1";

            foreach (var tag in _state.SlugsByTagsAndDates.Keys.OrderByDescending(k => k))
            {
                var publishedPosts = new List<(string date, string slug)>();
                foreach (var date in _state.SlugsByTagsAndDates[tag].Keys.OrderByDescending(k => k))
                {
                    foreach (var slugPair in _state.SlugsByTagsAndDates[tag][date])
                    {
                        var post = _state.Index[slugPair.Key];
                        if (!singlePostMode || post.Tags.Contains("blog"))
                            publishedPosts.Add((date, slugPair.Key));
                    }
                }

                // Only generate tag page if there is at least one published post for this tag
                if (publishedPosts.Count == 0)
                    continue;

                _quickSearch[tag] = $"{tag}.html";
                var outputPath = _state.Settings["output"];
                var filePath = Path.Combine(outputPath, $"{tag}.html");
                WriteHead(filePath, $"{_state.Settings.GetValueOrDefault("text_page_prefix", "caloni")}::{tag}", "index.html", true);

                var html = new StringBuilder();

                foreach (var (date, slug) in publishedPosts)
                {
                    var post = _state.Index[slug];
                    var tagsText = string.Join(" ", post.Tags);

                    html.AppendLine($"<tr id=\"{slug}\"><td>");
                    if (!string.IsNullOrEmpty(post.Image))
                    {
                        var basePath = _state.Settings.GetValueOrDefault("base_path", "");
                        html.AppendLine($"<img src=\"{basePath}/img/{post.Image}\"/>");
                    }

                    // TODO check whole tags implementation, IA code is broken
                    if (singlePostMode)
                    {
                        html.AppendLine($"<b><a href=\"{slug}.html\">{StringUtilities.TextToHtml(post.Title ?? "")}</a></b>");
                    }
                    else
                    {
                        html.AppendLine($"<b><a href=\"{post.Month}.html#{slug}\">{StringUtilities.TextToHtml(post.Title ?? "")}</a></b>");
                    }
                    html.AppendLine($"<small><i>{post.Date} {tagsText} {post.Summary}</small></i>");
                    html.AppendLine("</td></tr>");
                }

                File.AppendAllText(filePath, html.ToString(), System.Text.Encoding.UTF8);
                WriteBottom(filePath, true);
            }
        }

        private void FlushMonthsPage()
        {
            var outputPath = _state.Settings["output"];
            var filePath = Path.Combine(outputPath, "months.html");

            WriteHead(filePath, $"{_state.Settings.GetValueOrDefault("text_page_prefix", "caloni")}::months", "index.html", false);

            var html = new StringBuilder();

            // Group months by year
            var monthsByYear = new Dictionary<string, List<string>>();
            foreach (var month in _files.Keys.OrderBy(k => k))
            {
                // Extract year from month (format: YYYY-MM)
                var year = month.Substring(0, 4);

                if (!monthsByYear.ContainsKey(year))
                {
                    monthsByYear[year] = new List<string>();
                }

                monthsByYear[year].Add(month);
            }

            // List all years in descending order with their months inline
            foreach (var year in monthsByYear.Keys.OrderByDescending(k => k))
            {
                html.AppendLine($"<p id=\"{year}\" class=\"toc\"><strong>{year}</strong>");

                // List all months for this year inline
                monthsByYear[year].Reverse();
                foreach (var month in monthsByYear[year])
                {
                    var monthOnly = month.Substring(5, 2); // Extract month part (MM)
                    html.Append($"<a href=\"{month}.html\"> {monthOnly} </a> \n");
                }

                html.AppendLine("</p>");
            }

            File.AppendAllText(filePath, html.ToString(), System.Text.Encoding.UTF8);
            WriteBottom(filePath, false);

            _quickSearch["months"] = "months.html";
        }

        private void FlushIndexPage()
        {
            var outputPath = _state.Settings["output"];
            var filePath = Path.Combine(outputPath, "index.html");
            
            // Split favorite tags from settings
            var favoriteTags = _state.Settings.GetValueOrDefault("text_favorite_tags", "draft computer cinema")
        .Split(' ', StringSplitOptions.RemoveEmptyEntries);
            
            // Get the first (earliest) month for back link
            var firstMonth = "";
            if (_files.Keys.Any())
            {
                firstMonth = _files.Keys.OrderBy(k => k).First();
            }
            
            // Write head with back link to first month's about anchor
            var backLink = !string.IsNullOrEmpty(firstMonth) ? $"{firstMonth}.html#about" : "months.html";
            WriteHead(filePath, _state.Settings["title"], backLink, false, _quickSearch);
            
            var html = new StringBuilder();
            
            // Quick search input
            var quickSearchText = _state.Settings.GetValueOrDefault("text_quicksearch", "&#x1F41E; digite algo / type something");
            html.AppendLine($"<input type=\"text\" name=\"quick_search_name\" value=\"\" id=\"quick_search\" placeholder=\"{quickSearchText}\" style=\"width: 100%; font-size: 1.5rem; margin-top: 1em; margin-bottom: 0.5em;\" title=\"\"/></br>");
            
            // Blog home
            var blogHomeLink = _state.Settings.GetValueOrDefault("blog_home_link", "/blog");
            var blogDescription = _state.Settings.GetValueOrDefault("blog_home_description", "o que foi revisado e publicado no blogue.");
            html.AppendLine($"<big><a href=\"{blogHomeLink}\">blog</a></big><small><i>: {blogDescription}</small></i></br>");

            // Favorite tags with descriptions
            foreach (var tag in favoriteTags)
            {
                var description = _state.Settings.GetValueOrDefault($"text_favorite_tags_{tag}", "minha tag favorita.");
                html.AppendLine($"<big><a href=\"{tag}.html\">{tag}</a></big><small><i>: {description}</small></i></br>");
            }
            
            // Main navigation links
            var textTags = _state.Settings.GetValueOrDefault("text_tags", "todos os rótulos dos postes.");
            var textMonths = _state.Settings.GetValueOrDefault("text_months", "lista dos meses com postes.");
            var textPosts = _state.Settings.GetValueOrDefault("text_posts", "lista com toooooooodos os postes do jornal.");
            
            html.AppendLine($"<big><a href=\"tags.html\">tags</a></big><small><i>: {textTags}</small></i></br>");
            html.AppendLine($"<big><a href=\"months.html\">months</a></big><small><i>: {textMonths}</small></i></br>");
            html.AppendLine($"<big><a href=\"posts.html\">posts</a></big><small><i>: {textPosts}</small></i></br>");
            
            // Hidden results span for search
            html.AppendLine("<div><big><span style=\"visibility: hidden; padding: 5px;\" name=\"results\" id=\"results\">...</span></big></div>");
            
            // Empty table for search results (populated by JS)
            html.AppendLine("<table class=\"sortable\" style=\"width: 100%;\">");
            html.AppendLine("</table>");
            
            File.AppendAllText(filePath, html.ToString(), System.Text.Encoding.UTF8);
            
            WriteBottom(filePath, false, "", "", BuildFooterLink());
            
            _quickSearch["index"] = "index.html";
        }

        private void FlushNotFoundPage()
        {
            var outputPath = _state.Settings["output"];
            var filePath = Path.Combine(outputPath, "404.html");
            
            WriteHead(filePath, $"{_state.Settings.GetValueOrDefault("text_page_prefix", "caloni")}::404 page not found", "posts.html", false);
            
            var html = new StringBuilder();
            html.AppendLine("<div class=\"container\">");
            html.AppendLine($"  <p class=\"title\">{_state.Settings.GetValueOrDefault("text_notfound_title", "Opa, essa página não foi encontrada.")}</p>");
            html.AppendLine("    <div class=\"content\">");
            html.AppendLine($"      <p>{_state.Settings.GetValueOrDefault("text_notfound_description", "Não quer fazer uma <a href=\"/journal/posts.html\">busca</a>? Às vezes eu mexo e remexo as coisas por aqui.")}</p>");
            html.AppendLine("    </div>");
            html.AppendLine("</div>");
            
            File.AppendAllText(filePath, html.ToString(), System.Text.Encoding.UTF8);
            WriteBottom(filePath, false);
        }

        private void WriteHead(string filePath, string title, string backLink, bool filter, Dictionary<string, string>? quickSearch = null)
        {
            // Ensure directory exists before writing
            var directory = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var basePath = _state.Settings.GetValueOrDefault("base_path", "");
            var html = new StringBuilder();
            
            html.AppendLine("<!DOCTYPE html>");
            html.AppendLine("<html lang=\"en-us\" dir=\"ltr\" itemscope itemtype=\"http://schema.org/Article\">");
            html.AppendLine("<head>");
            html.AppendLine("<meta charset=\"utf-8\" />");
            html.AppendLine("<meta name=\"viewport\" content=\"width=device-width, initial-scale=1\"/>");
            html.AppendLine($"<title>{_state.Settings["title"]}</title>");
            html.AppendLine($"<meta name=\"author\" content=\"{_state.Settings["author"]}\" />");
            html.AppendLine($"<meta name=\"generator\" content=\"{_state.Settings["generator"]}\">");
            html.AppendLine($"<meta property=\"og:title\" content=\"{_state.Settings["title"]}\"/>");
            html.AppendLine($"<meta property=\"og:type\" content=\"website\"/>");
            html.AppendLine($"<meta property=\"og:url\" content=\"http://www.caloni.com.br\"/>");
            html.AppendLine($"<meta property=\"og:image\" content=\"{basePath}/img/about-brand.png\"/>");
            html.AppendLine($"<meta property=\"og:description\" content=\"Write for computers, people and food.\"/>");
            html.AppendLine($"<link href=\"{basePath}/index.xml\" rel=\"feed\" type=\"application/rss+xml\" title=\"{_state.Settings["title"]}\"/>");
            html.AppendLine($"<link rel=\"stylesheet\" type=\"text/css\" href=\"{basePath}/css/custom.css\"/>");
            html.AppendLine($"<link rel=\"stylesheet\" type=\"text/css\" href=\"{basePath}/css/jquery-ui.css\"/>");
            html.AppendLine($"<script src=\"{basePath}/js/jquery-1.12.4.js\"></script>");
            html.AppendLine($"<script src=\"{basePath}/js/jquery-ui.js\"></script>");
            html.AppendLine($"<script src=\"{basePath}/js/copy_clipboard.js\"></script>");

            if (quickSearch != null)
            {
                html.AppendLine("<script>");
                html.AppendLine("var quick_search_posts = [");
                foreach (var entry in quickSearch.OrderBy(kv => kv.Key))
                {
                    html.AppendLine($"\"{entry.Value}\",");
                }
                html.AppendLine("];");
                html.AppendLine("</script>");
                html.AppendLine($"<script src=\"{basePath}/js/quick_search.js\"></script>");
            }
            
            html.AppendLine($"<script src=\"{basePath}/js/list.js\"></script>");
            html.AppendLine($"<link rel=\"icon\" href=\"{basePath}/img/favicon.ico\"/>");
            html.AppendLine("</head>");
            html.AppendLine("<body style=\"min-height:100vh;display:flex;flex-direction:column\">");
            html.AppendLine("<nav class=\"navbar has-shadow is-white\"");
            html.AppendLine("role=\"navigation\" aria-label=\"main navigation\">");
            html.AppendLine("<div class=\"container\">");
            html.AppendLine("<div class=\"navbar-brand\">");
            html.AppendLine("&nbsp;");
            html.AppendLine($"<a class=\"navbar-item\" href=\"{backLink}\">");
            html.AppendLine($"<div class=\"is-4\"><b>{_state.Settings["title"]}</b></div>");
            html.AppendLine("</a>");
            html.AppendLine("</div>");
            html.AppendLine("</div>");
            html.AppendLine("</nav>");
            html.AppendLine("<div class=\"container\">");
            html.AppendLine("<div class=\"column\">");
            html.AppendLine("<div style=\"min-height:56vh\">");
            html.AppendLine("<div style=\"padding-bottom: 1em;\"></div>");
            
            if (filter)
            {
                html.AppendLine("<input type=\"text\" name=\"filter\" value=\"\" id=\"filter\" placeholder=\"enter to select\" style=\"width: 100%; font-size: 1.5rem; margin-top: 1em; margin-bottom: 0.5em;\" title=\"\"/></br>");
                html.AppendLine("<button id=\"homebutton\" style=\"font-size: 1rem;\" onclick=\"window.location = '/';\">home</button>");
                html.AppendLine("<button id=\"filterbutton\" style=\"font-size: 1rem;\" onclick=\"ApplyFilter($('#filter').val());\">select</button>");
                html.AppendLine("<button id=\"removebutton\" style=\"font-size: 1rem;\" onclick=\"ApplyNotFilter($('#filter').val());\">remove</button>");
                html.AppendLine("<button id=\"randombutton\" style=\"font-size: 1rem;\" onclick=\"window.location = randomPost;\">random</button>");
                html.AppendLine("<div><big><b><span style=\"visibility: hidden; padding: 5px;\" name=\"results\" id=\"results\">...</span></b></big></div>");
                html.AppendLine("<table class=\"sortable\" style=\"width: 100%;\">");
            }
            
            File.WriteAllText(filePath, html.ToString(), System.Text.Encoding.UTF8);
        }

        private string BuildFooterLink()
        {
            var date = _state.Settings.GetValueOrDefault("date", "");
            var commitHash = _state.Settings.GetValueOrDefault("build", "");
            var repoAddress = _state.Settings.GetValueOrDefault("public_repo_github_address", "#");

            var splitIndex = date.IndexOf('T');
            var datePart = splitIndex > 0 ? date.Substring(0, splitIndex) : date;
            var timePart = splitIndex > 0 ? date.Substring(splitIndex + 1) : "";

            return $"<a href=\"{repoAddress}/{commitHash}\" title=\"{timePart}\">{datePart}</a>";
        }

        private void WriteBottom(string filePath, bool filter, string nextLink = "", string prevLink = "", string build = "")
        {
            var html = new StringBuilder();
            var singlePostMode = _state.Settings.ContainsKey("single_post_mode") &&
                                _state.Settings["single_post_mode"] == "1";
            
            if (filter)
            {
                html.AppendLine("</table>");
            }
            
            html.AppendLine("<span style=\"float: left;\">");

            if (singlePostMode)
            {
                var commentEmail = _state.Settings.GetValueOrDefault("comment_email", "");
                var currentSlug = Path.GetFileNameWithoutExtension(filePath) ?? "";
                var currentTitle = currentSlug;
                if (_state.Index.ContainsKey(currentSlug) && !string.IsNullOrWhiteSpace(_state.Index[currentSlug].Title))
                {
                    currentTitle = _state.Index[currentSlug].Title!;
                }

                var subject = Uri.EscapeDataString($"Comment about: {currentTitle}");
                var mailtoTarget = string.IsNullOrWhiteSpace(commentEmail)
                    ? $"mailto:?subject={subject}"
                    : $"mailto:{commentEmail}?subject={subject}";
                html.AppendLine($" <a href=\"{mailtoTarget}\">[comment]</a>");
            }

            if (!string.IsNullOrEmpty(nextLink))
            {
                var label = ResolveNavigationLabel(nextLink);
                html.AppendLine($" <a href=\"{nextLink}\">[{label}]</a>");
            }

            if (!singlePostMode && !string.IsNullOrEmpty(prevLink))
            {
                var label = ResolveNavigationLabel(prevLink);
                html.AppendLine($" <a href=\"{prevLink}\">[{label}]</a>");
            }

            html.AppendLine("</span>");
            html.AppendLine("</div>");
            html.AppendLine("</div>");
            html.AppendLine("</section>");
            html.AppendLine("<footer class=\"footer\">");
            html.AppendLine("<div class=\"container\">");
            
            if (!string.IsNullOrEmpty(build))
            {
                if (!singlePostMode)
                {
                    html.AppendLine($"<p class=\"tiny\"><i>Arquivo pessoal em andamento. Build {build}.</i></p>");
                }
            }
            
            html.AppendLine("</div>");
            html.AppendLine("<div class=\"intentionally-blank\"></div>");
            html.AppendLine("</footer>");
            html.AppendLine("</body>");
            html.AppendLine("</html>");
            
            File.AppendAllText(filePath, html.ToString(), System.Text.Encoding.UTF8);
        }

        private string ResolveNavigationLabel(string link)
        {
            var key = link.Replace(".html", "");
            if (_state.Index.ContainsKey(key))
            {
                var title = _state.Index[key].Title;
                if (!string.IsNullOrWhiteSpace(title))
                {
                    return StringUtilities.TextToHtml(title);
                }
            }

            return StringUtilities.TextToHtml(key);
        }

        private void TiePreviousMonths()
        {
            // Link months chronologically
            // Forward pass: set next month links
            var sortedMonths = _files.Keys.OrderBy(k => k).ToList();
            var defaultLink = "index";
            
            foreach (var month in sortedMonths)
            {
                _nextMonth[month] = defaultLink;
                defaultLink = month;
            }
            
            // Backward pass: set previous month links
            defaultLink = "index";
            foreach (var month in _files.Keys.OrderByDescending(k => k))
            {
                _prevMonth[month] = defaultLink;
                defaultLink = month;
            }
        }
    }
}

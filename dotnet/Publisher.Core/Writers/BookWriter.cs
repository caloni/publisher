using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Publisher.Core.Models;
using Publisher.Core.Utilities;

namespace Publisher.Core.Writers
{
    /// <summary>
    /// Generates EPUB files from parsed journal posts.
    /// </summary>
    public class BookWriter
    {
        private readonly GlobalState _state;
        private readonly Dictionary<string, bool> _files;
        private readonly Dictionary<string, string> _chapters;
        private readonly Dictionary<string, Dictionary<string, string>> _titlesByTags;
        private readonly Dictionary<string, Dictionary<string, Dictionary<string, string>>> _titlesByTagsAndDates;
        private readonly Dictionary<string, string> _letters;

        public BookWriter(GlobalState state)
        {
            _state = state;
            _files = new Dictionary<string, bool>();
            _chapters = new Dictionary<string, string>();
            _titlesByTags = new Dictionary<string, Dictionary<string, string>>();
            _titlesByTagsAndDates = new Dictionary<string, Dictionary<string, Dictionary<string, string>>>();
            _letters = new Dictionary<string, string>();
            
            // Set default settings
            _state.Settings["title"] = "Jornal do Caloni: Write for computers, people and food.";
            _state.Settings["author"] = "Wanderley Caloni";
            _state.Settings["publisher"] = "Caloni";
            if (!_state.Settings.ContainsKey("output"))
              _state.Settings["output"] = Path.Combine("publisher", "public", "book");
        }

        public void Generate()
        {
            FlushCoverPage();
            PopulateChapters();
            FlushPosts();
            FlushPostsPages();
            FlushPackage();
            FlushTocNcx();
            FlushTocPage();
            FlushTagsPage();
            FlushNavigationDocument();
            FlushIndexPage();
        }

        private void FlushCoverPage()
        {
            var outputPath = Path.Combine(_state.Settings["output"], "EPUB", "cover.xhtml");
            var title = StringUtilities.TextToHtml(_state.Settings["title"]);

            var sb = new StringBuilder();
            sb.AppendLine("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
            sb.AppendLine("<html xmlns=\"http://www.w3.org/1999/xhtml\" xmlns:epub=\"http://www.idpf.org/2007/ops\" xml:lang=\"pt-BR\" lang=\"pt-BR\">");
            sb.AppendLine("<head>");
            sb.AppendLine("<meta http-equiv=\"default-style\" content=\"text/html; charset=utf-8\"/>");
            sb.AppendLine("<meta name=\"viewport\" content=\"width=625, height=1000\"/>");
            sb.AppendLine($"<title>{title}</title>");
            sb.AppendLine("<link rel=\"stylesheet\" href=\"css/stylesheet.css\" type=\"text/css\" />");
            sb.AppendLine("<style type=\"text/css\">html, body { margin: 0; padding: 0; } .cover-image { display: block; width: 100%; height: auto; }</style>");
            sb.AppendLine("</head>");
            sb.AppendLine("<body epub:type=\"frontmatter\">");
            sb.AppendLine("<section epub:type=\"cover\">");
            sb.AppendLine($"<img class=\"cover-image\" src=\"img/cover.jpg\" alt=\"Cover of {title}\"/>");
            sb.AppendLine("</section>");
            sb.AppendLine("</body>");
            sb.AppendLine("</html>");

            File.WriteAllText(outputPath, sb.ToString(), Encoding.UTF8);
        }

        private void PopulateChapters()
        {
            foreach (var slug in _state.Index.Keys)
            {
                if (!string.IsNullOrEmpty(_state.Index[slug].Month))
                {
                    _state.Index[slug].Chapter = _state.Index[slug].Month;
                }
            }
        }

        private void FlushPosts()
        {
            foreach (var slug in _state.PostSlugsInOrder)
            {
                if (string.IsNullOrEmpty(_state.Index[slug].Date))
                {
                    Console.WriteLine($"Skipping {slug} (no date)");
                    continue;
                }
                WritePost(slug);
            }
        }

        private void WritePost(string slug)
        {
            var post = _state.Index[slug];
            var chapterId = ChapterToId(post.Chapter ?? "");
            
            _chapters[post.Chapter ?? ""] = post.Chapter ?? "";
            
            // Track titles by tags
            var tags = post.Tags;
            foreach (var tag in tags)
            {
                if (!_titlesByTags.ContainsKey(tag))
                    _titlesByTags[tag] = new Dictionary<string, string>();
                _titlesByTags[tag][post.Title ?? ""] = post.Title ?? "";
                
                if (!_titlesByTagsAndDates.ContainsKey(tag))
                    _titlesByTagsAndDates[tag] = new Dictionary<string, Dictionary<string, string>>();
                if (!_titlesByTagsAndDates[tag].ContainsKey(post.Date ?? ""))
                    _titlesByTagsAndDates[tag][post.Date ?? ""] = new Dictionary<string, string>();
                // Value is this post's own slug, not its title - titles can repeat across
                // posts, and re-deriving the slug from the title later would pick whichever
                // post last claimed that title instead of this specific one.
                _titlesByTagsAndDates[tag][post.Date ?? ""][post.Title ?? ""] = slug;
            }

            var outputPath = Path.Combine(_state.Settings["output"], "EPUB", $"{chapterId}.xhtml");
            var postText = new StringBuilder();

            // First post in chapter - write header
            if (!_files.ContainsKey(chapterId))
            {
                postText.AppendLine("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
                postText.AppendLine("<html xmlns=\"http://www.w3.org/1999/xhtml\" xmlns:epub=\"http://www.idpf.org/2007/ops\">");
                postText.AppendLine("<head><meta http-equiv=\"default-style\" content=\"text/html; charset=utf-8\"/>");
                postText.AppendLine($"<title>{StringUtilities.TextToHtml(post.Chapter ?? "")}</title>");
                postText.AppendLine("<link rel=\"stylesheet\" href=\"css/stylesheet.css\" type=\"text/css\" />");
                postText.AppendLine("<link rel=\"stylesheet\" href=\"css/page-template.xpgt\" type=\"application/adobe-page-template+xml\" />");
                postText.AppendLine("</head>");
                postText.AppendLine("<body>");
                postText.AppendLine("<div class=\"body\">");
                postText.AppendLine($"<span epub:type=\"pagebreak\" id=\"{ChapterToId(post.Chapter ?? "")}\" title=\"{StringUtilities.TextToHtml(post.Chapter ?? "")}\"/>");
                postText.AppendLine($"<h1 class=\"chapter-title\"><strong>{StringUtilities.TextToHtml(post.Chapter ?? "")}</strong></h1>");
                _files[chapterId] = true;
            }

            // Process post lines before writing (replaces [links] with actual HTML)
            ProcessPostLines(post);

            // Post content
            postText.AppendLine($"<span epub:type=\"pagebreak\" id=\"{SlugToId(slug)}\" title=\"{StringUtilities.TextToHtml(post.Title ?? "")}\"/>");
            postText.AppendLine($"<section title=\"{StringUtilities.TextToHtml(post.Title ?? "")}\" epub:type=\"bodymatter chapter\">");
            postText.AppendLine($"<h1 class=\"chapter-subtitle\"><strong>{StringUtilities.TextToHtml(post.Title ?? "")}</strong></h1>");
            postText.Append($"<p class=\"note-title\">{post.Date}");
            
            // Tags linking to the exact post anchor in the tag page
            foreach (var tag in tags)
            {
                postText.Append(" ");
                postText.Append($"<a href=\"toc{SlugToId(tag)}.xhtml#{SlugToId(slug)}\">{tag}</a>");
            }
            postText.AppendLine("</p>");
            postText.AppendLine();

            // Post blocks
            var linkToBegin = $" <a href=\"#{SlugToId(slug)}\"><em>&lt;</em></a>";
            for (int i = 0; i < post.Blocks.Count; i++)
            {
                if (i == post.Blocks.Count - 1)
                    postText.Append(FormatBlock(i, post, linkToBegin));
                else
                    postText.Append(FormatBlock(i, post));
            }
            
            postText.AppendLine("</section>");

            File.AppendAllText(outputPath, postText.ToString(), Encoding.UTF8);
        }

        internal void ProcessPostLines(Post post)
        {
            for (int i = 0; i < post.Blocks.Count; i++)
            {
                var block = post.Blocks[i];
                var content = block.Content ?? "";

                if (string.IsNullOrEmpty(content))
                    continue;

                // Skip reference-style link definitions - metadata, not content
                if (Regex.IsMatch(content, @"^\[([^\]]+)\]:\s*(.+)$"))
                    continue;

                // Resolve [linkText] references for non-code, non-blockquote blocks
                if (block.Kind != BlockKind.Code && block.Kind != BlockKind.Blockquote)
                {
                    foreach (var link in post.Links)
                    {
                        var linkText = link.Key;
                        var linkTarget = link.Value;

                        if (_state.PostMetadata.ContainsKey(linkTarget))
                        {
                            var targetChapter = _state.PostMetadata[linkTarget].Chapter;
                            linkTarget = targetChapter != null
                                ? $"<a href=\"{ChapterToId(targetChapter)}.xhtml#{SlugToId(linkTarget)}\">{linkText}</a>"
                                : $"<a href=\"{SlugToId(linkTarget)}.xhtml\">{linkText}</a>";
                        }
                        else if (linkTarget.StartsWith("http") || linkTarget.StartsWith("mailto:") || linkTarget.StartsWith("ftp:"))
                        {
                            linkTarget = RemoveQueryString(linkTarget);
                            linkTarget = $"<a href=\"{linkTarget}\">{linkText}</a>";
                        }
                        else if (LinkResource.IsImagePath(linkTarget))
                        {
                            var fileName = Path.GetFileName(linkTarget);
                            linkTarget = $"<p><img src=\"img/{fileName}\"/></p>";
                        }
                        else
                        {
                            linkTarget = $"<a href=\"{linkTarget}\">{linkText}</a>";
                        }

                        var pattern = $"\\[{Regex.Escape(linkText)}\\]";
                        content = Regex.Replace(content, pattern, linkTarget);
                    }
                }

                // Resolve "Quoted Text" to internal or external links
                if (block.Kind != BlockKind.Code)
                {
                    content = Regex.Replace(
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
                                var targetChapter = _state.PostMetadata.ContainsKey(targetSlug)
                                    ? _state.PostMetadata[targetSlug].Chapter
                                    : null;
                                string href = targetChapter != null
                                    ? $"{ChapterToId(targetChapter)}.xhtml#{SlugToId(targetSlug)}"
                                    : $"{SlugToId(targetSlug)}.xhtml";
                                return $"<a href=\"{href}\">{inner}</a>";
                            }
                            if (_state.Links.TryGetValue(inner, out var externalUrl))
                            {
                                if (LinkResource.IsImagePath(externalUrl))
                                {
                                    var fileName = Path.GetFileName(externalUrl);
                                    return $"<p><img src=\"img/{fileName}\"/></p>";
                                }
                                externalUrl = RemoveQueryString(externalUrl);
                                return $"<a href=\"{System.Net.WebUtility.HtmlEncode(externalUrl)}\">{inner}</a>";
                            }
                            return m.Value;
                        });
                }

                post.Blocks[i].Content = content;
            }
        }

        private string RemoveQueryString(string url)
        {
            // EPUB validators can reject URLs with query strings, so strip them.
            var questionMarkIndex = url.IndexOf('?');
            if (questionMarkIndex > 0)
                return url.Substring(0, questionMarkIndex);
            return url;
        }

        internal string FormatBlock(int blockIndex, Post post, string preSuffix = "")
        {
            var prefix = "";
            var suffix = "";
            var block = post.Blocks[blockIndex];

            switch (block.Kind)
            {
                case BlockKind.Code:
                    block.Content = StringUtilities.TextToHtml(block.Content ?? "") + "\n";
                    if (blockIndex > 0 && post.Blocks[blockIndex - 1].Kind != BlockKind.Code)
                        prefix = "<pre>\n";
                    if (blockIndex < post.Blocks.Count - 1 && post.Blocks[blockIndex + 1].Kind != BlockKind.Code)
                        suffix = "</pre>\n";
                    else if (blockIndex == post.Blocks.Count - 1)
                        suffix = "</pre>\n";
                    break;

                case BlockKind.Blockquote:
                    if (!string.IsNullOrEmpty(block.Content))
                    {
                        prefix = "<blockquote>";
                        suffix = "</blockquote>\n";
                    }
                    break;

                case BlockKind.UnorderedList:
                case BlockKind.OrderedList:
                    var listTag = block.Kind == BlockKind.OrderedList ? "ol" : "ul";
                    // A list starting at blockIndex 0 has no previous block to compare
                    // against, but still needs its own <ul>/<ol> opened - otherwise the
                    // whole list's <li> items are emitted with no enclosing list at all.
                    prefix = blockIndex == 0 || post.Blocks[blockIndex - 1].Kind != block.Kind
                        ? $"<{listTag}><li>"
                        : "<li>";
                    suffix = blockIndex < post.Blocks.Count - 1 && post.Blocks[blockIndex + 1].Kind != block.Kind
                        ? $"</li></{listTag}>\n"
                        : blockIndex == post.Blocks.Count - 1 ? $"</li></{listTag}>\n" : "</li>\n";
                    break;

                case BlockKind kind when kind.IsHeading():
                    var tag = kind.HtmlTag();
                    prefix = $"<{tag}>";
                    suffix = $"</{tag}>\n";
                    break;

                case BlockKind.Paragraph:
                    prefix = "<p>";
                    suffix = "</p>\n";
                    break;

                case BlockKind.Image:
                    // preSuffix (the "back to top" link) must land after the <img> tag, not
                    // inside its still-open src="..." attribute, so this is composed directly
                    // instead of going through the shared prefix/content/preSuffix/suffix return below.
                    return $"<p><img src=\"img/{block.Content}\"/>{preSuffix}</p>\n";
            }

            return prefix + block.Content + preSuffix + suffix;
        }

        private void FlushPostsPages()
        {
            foreach (var chapter in _files.Keys)
            {
                var outputPath = Path.Combine(_state.Settings["output"], "EPUB", $"{chapter}.xhtml");
                File.AppendAllText(outputPath, "</div>\n</body>\n</html>\n", Encoding.UTF8);
            }
        }

        private void FlushPackage()
        {
            var outputPath = Path.Combine(_state.Settings["output"], "EPUB", "package.opf");
            var sb = new StringBuilder();
            
            sb.AppendLine("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
            sb.AppendLine("<package xmlns=\"http://www.idpf.org/2007/opf\" version=\"3.0\" unique-identifier=\"p0000000000000\">");
            sb.AppendLine("<metadata xmlns:dc=\"http://purl.org/dc/elements/1.1/\">");
            sb.AppendLine($"<dc:title id=\"title\">{_state.Settings["title"]}</dc:title>");
            sb.AppendLine($"<dc:creator>{_state.Settings["author"]}</dc:creator>");
            sb.AppendLine($"<dc:publisher>{_state.Settings["publisher"]}</dc:publisher>");
            sb.AppendLine($"<dc:rights>Copyright {_state.Settings.GetValueOrDefault("build", "")}</dc:rights>");
            sb.AppendLine("<dc:identifier id=\"p0000000000000\">0000000000000</dc:identifier>");
            sb.AppendLine("<dc:source id=\"src-id\">urn:isbn:0000000000000</dc:source>");
            sb.AppendLine("<dc:language>pt-BR</dc:language>");
            sb.AppendLine($"<meta property=\"dcterms:modified\">{_state.Settings.GetValueOrDefault("date_utc", "")}</meta>");
            sb.AppendLine("<meta name=\"cover\" content=\"cover-image\"/>");
            sb.AppendLine("</metadata>");
            sb.AppendLine("<manifest>");
            sb.AppendLine("<item id=\"cover\" href=\"cover.xhtml\" media-type=\"application/xhtml+xml\"/>");
            sb.AppendLine("<item id=\"cover-image\" properties=\"cover-image\" href=\"img/cover.jpg\" media-type=\"image/jpeg\"/>");
            sb.AppendLine("<item id=\"style\" href=\"css/stylesheet.css\" media-type=\"text/css\"/>");
            sb.AppendLine("<item id=\"nav\" properties=\"nav\" href=\"nav.xhtml\" media-type=\"application/xhtml+xml\"/>");
            sb.AppendLine("<item id=\"ncx1\" href=\"toc.ncx\" media-type=\"application/x-dtbncx+xml\"/>");
            sb.AppendLine("<item id=\"page-template\" href=\"css/page-template.xpgt\" media-type=\"application/adobe-page-template+xml\"/>");
            sb.AppendLine("<item id=\"titlepage\" href=\"titlepage.xhtml\" media-type=\"application/xhtml+xml\"/>");
            sb.AppendLine("<item id=\"toc\" href=\"toc.xhtml\" media-type=\"application/xhtml+xml\"/>");
            
            foreach (var tag in _titlesByTags.Keys)
            {
                sb.AppendLine($"<item id=\"toc_{tag}\" href=\"toc_{tag}.xhtml\" media-type=\"application/xhtml+xml\"/>");
            }
            
            sb.AppendLine("<item id=\"index\" href=\"index.xhtml\" media-type=\"application/xhtml+xml\"/>");
            
            foreach (var chapter in _chapters.Keys.OrderBy(k => k))
            {
                sb.AppendLine($"<item id=\"{ChapterToId(chapter)}\" href=\"{ChapterToId(chapter)}.xhtml\" media-type=\"application/xhtml+xml\"/>");
            }
            
            // Images
            int imageId = 0;
            foreach (var image in _state.PostsImages.Keys)
            {
                var mediaType = GetImageMediaType(image);
                sb.AppendLine($"<item id=\"img-id-{++imageId}\" href=\"img/{image}\" media-type=\"{mediaType}\"/>");
            }
            
            sb.AppendLine("</manifest>");
            sb.AppendLine("<spine toc=\"ncx1\">");
            sb.AppendLine("<itemref idref=\"cover\" linear=\"yes\"/>");
            sb.AppendLine("<itemref idref=\"titlepage\" linear=\"yes\"/>");
            sb.AppendLine("<itemref idref=\"toc\" linear=\"yes\"/>");
            
            foreach (var tag in _titlesByTags.Keys)
            {
                sb.AppendLine($"<itemref linear=\"yes\" idref=\"toc{SlugToId(tag)}\"/>");
            }
            
            foreach (var chapter in _chapters.Keys.OrderBy(k => k))
            {
                sb.AppendLine($"<itemref linear=\"yes\" idref=\"{ChapterToId(chapter)}\"/>");
            }
            
            sb.AppendLine("<itemref linear=\"yes\" idref=\"index\"/>");
            sb.AppendLine("<itemref linear=\"yes\" idref=\"nav\"/>");
            sb.AppendLine("</spine>");
            sb.AppendLine("<guide>");
            sb.AppendLine("<reference type=\"cover\" title=\"Cover\" href=\"cover.xhtml\"/>");
            sb.AppendLine("</guide>");
            sb.AppendLine("</package>");
            
            File.WriteAllText(outputPath, sb.ToString(), Encoding.UTF8);
        }

        private void FlushTocNcx()
        {
            var outputPath = Path.Combine(_state.Settings["output"], "EPUB", "toc.ncx");
            var sb = new StringBuilder();
            
            sb.AppendLine("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
            sb.AppendLine("<ncx xmlns=\"http://www.daisy.org/z3986/2005/ncx/\" version=\"2005-1\" xml:lang=\"en-US\">");
            sb.AppendLine("<head>");
            sb.AppendLine("<meta name=\"dtb:uid\" content=\"0000000000000\"/>");
            sb.AppendLine("<meta name=\"dtb:depth\" content=\"1\"/>");
            sb.AppendLine("<meta name=\"dtb:totalPageCount\" content=\"0\"/>");
            sb.AppendLine("<meta name=\"dtb:maxPageNumber\" content=\"0\"/>");
            sb.AppendLine("</head>");
            sb.AppendLine("<docTitle><text>Jornal do Caloni: Write for computers, people and food.</text></docTitle>");
            sb.AppendLine("<docAuthor><text>Wanderley Caloni</text></docAuthor>");
            sb.AppendLine("<navMap>");
            sb.AppendLine("<navPoint id=\"cover\" playOrder=\"1\"><navLabel><text>Cover</text></navLabel><content src=\"cover.xhtml\"/></navPoint>");
            sb.AppendLine("<navPoint id=\"toc\" playOrder=\"2\"><navLabel><text>Contents</text></navLabel><content src=\"toc.xhtml\"/></navPoint>");
            sb.AppendLine("</navMap>");
            sb.AppendLine("</ncx>");
            
            File.WriteAllText(outputPath, sb.ToString(), Encoding.UTF8);
        }

        private void FlushTocPage()
        {
            var outputPath = Path.Combine(_state.Settings["output"], "EPUB", "toc.xhtml");
            var sb = new StringBuilder();
            
            sb.AppendLine("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
            sb.AppendLine("<html xmlns=\"http://www.w3.org/1999/xhtml\" xmlns:epub=\"http://www.idpf.org/2007/ops\">");
            sb.AppendLine("<head><meta http-equiv=\"default-style\" content=\"text/html; charset=utf-8\"/>");
            sb.AppendLine("<title>Jornal do Caloni: Write for computers, people and food.</title>");
            sb.AppendLine("<link rel=\"stylesheet\" href=\"css/stylesheet.css\" type=\"text/css\" />");
            sb.AppendLine("<link rel=\"stylesheet\" href=\"css/page-template.xpgt\" type=\"application/adobe-page-template+xml\" />");
            sb.AppendLine("</head>");
            sb.AppendLine("<body>");
            sb.AppendLine("<div class=\"body\">");
            sb.AppendLine("<a id=\"piii\"></a>");
            sb.AppendLine("<h1 class=\"toc-title\">Contents</h1>");
            sb.AppendLine("<p id=\"indx-1\" class=\"toca\"><a href=\"index.xhtml\"><strong>Index</strong></a></p>");
            
            string lastYear = "2000";
            foreach (var chapter in _chapters.Keys.OrderBy(k => k))
            {
                var year = chapter.Substring(0, 4);
                var month = chapter.Substring(5, 2);
                
                if (year != lastYear)
                {
                    if (lastYear != "2000")
                        sb.AppendLine("</p>");
                    sb.Append($"<p id=\"{ChapterToId(chapter)}\" class=\"toc\"><strong>{year}</strong>");
                    lastYear = year;
                }
                
                sb.Append($"<a href=\"{ChapterToId(chapter)}.xhtml\"> {StringUtilities.TextToHtml(month)} </a>");
            }
            
            sb.AppendLine("</p>");
            sb.AppendLine("<a id=\"piv\"></a>");
            sb.AppendLine("</div>");
            sb.AppendLine("</body>");
            sb.AppendLine("</html>");
            
            File.WriteAllText(outputPath, sb.ToString(), Encoding.UTF8);
        }

        private void FlushTagsPage()
        {
            foreach (var tag in _titlesByTagsAndDates.Keys.OrderBy(k => k))
            {
                var outputPath = Path.Combine(_state.Settings["output"], "EPUB", $"toc_{tag}.xhtml");
                var sb = new StringBuilder();
                
                sb.AppendLine("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
                sb.AppendLine("<html xmlns=\"http://www.w3.org/1999/xhtml\" xmlns:epub=\"http://www.idpf.org/2007/ops\">");
                sb.AppendLine("<head><meta http-equiv=\"default-style\" content=\"text/html; charset=utf-8\"/>");
                sb.AppendLine($"<title>{tag}</title>");
                sb.AppendLine("<link rel=\"stylesheet\" href=\"css/stylesheet.css\" type=\"text/css\" />");
                sb.AppendLine("<link rel=\"stylesheet\" href=\"css/page-template.xpgt\" type=\"application/adobe-page-template+xml\" />");
                sb.AppendLine("</head>");
                sb.AppendLine("<body>");
                sb.AppendLine("<div class=\"body\">");
                sb.AppendLine($"<h1 class=\"toc-title\">{tag}</h1>");
                sb.AppendLine("<ul>");
                
                foreach (var date in _titlesByTagsAndDates[tag].Keys.OrderBy(k => k))
                {
                    foreach (var kv in _titlesByTagsAndDates[tag][date])
                    {
                        var title = kv.Key;
                        var slug = kv.Value;
                        var chapter = _state.Index[slug].Chapter;
                        sb.AppendLine($"<li id=\"{SlugToId(slug)}\"><a href=\"{ChapterToId(chapter ?? "")}.xhtml#{SlugToId(slug)}\">{StringUtilities.TextToHtml(title)}</a></li>");
                    }
                }
                
                sb.AppendLine("</ul>");
                sb.AppendLine("</div>");
                sb.AppendLine("</body>");
                sb.AppendLine("</html>");
                
                File.WriteAllText(outputPath, sb.ToString(), Encoding.UTF8);
            }
        }

        private void FlushNavigationDocument()
        {
            var outputPath = Path.Combine(_state.Settings["output"], "EPUB", "nav.xhtml");
            var sb = new StringBuilder();
            var chapters = _chapters.Keys.OrderBy(k => k).ToList();
            
            sb.AppendLine("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
            sb.AppendLine("<html xmlns=\"http://www.w3.org/1999/xhtml\" xmlns:epub=\"http://www.idpf.org/2007/ops\" xml:lang=\"pt-BR\" lang=\"pt-BR\">");
            sb.AppendLine("<head><meta http-equiv=\"default-style\" content=\"text/html; charset=utf-8\"/>");
            sb.AppendLine("<title>Jornal do Caloni: Write for computers, people and food.</title>");
            sb.AppendLine("<link rel=\"stylesheet\" href=\"css/stylesheet.css\" type=\"text/css\" />");
            sb.AppendLine("</head>");
            sb.AppendLine("<body>");
            sb.AppendLine("<nav epub:type=\"toc\">");
            sb.AppendLine("<h2>Contents</h2>");
            sb.AppendLine("<ol>");
            sb.AppendLine("<li><a href=\"cover.xhtml\">Cover</a></li>");
            sb.AppendLine("<li><a href=\"titlepage.xhtml\">Title Page</a></li>");
            sb.AppendLine("<li><a href=\"toc.xhtml\">Contents Page</a></li>");
            sb.AppendLine("<li><a href=\"index.xhtml\">Index</a></li>");

            string? currentYear = null;
            foreach (var chapter in chapters)
            {
                var year = chapter.Substring(0, 4);
                var month = chapter.Substring(5, 2);

                if (year != currentYear)
                {
                    if (currentYear != null)
                        sb.AppendLine("</ol></li>");

                    // EPUB3 nav requires an <li> preceding a nested <ol> to contain a
                    // child <a> or <span> - bare text here gets the whole <li> rejected.
                    sb.AppendLine($"<li><span>{year}</span><ol>");
                    currentYear = year;
                }

                sb.AppendLine($"<li><a href=\"{ChapterToId(chapter)}.xhtml\">{month}</a></li>");
            }

            if (currentYear != null)
                sb.AppendLine("</ol></li>");

            sb.AppendLine("</ol>");
            sb.AppendLine("</nav>");
            sb.AppendLine("<nav epub:type=\"landmarks\" hidden=\"hidden\">");
            sb.AppendLine("<h2>Landmarks</h2>");
            sb.AppendLine("<ol>");
            sb.AppendLine("<li><a epub:type=\"cover\" href=\"cover.xhtml\">Cover</a></li>");
            sb.AppendLine("<li><a epub:type=\"titlepage\" href=\"titlepage.xhtml\">Title Page</a></li>");
            sb.AppendLine("<li><a epub:type=\"toc\" href=\"toc.xhtml\">Contents</a></li>");
            if (chapters.Count > 0)
                sb.AppendLine($"<li><a epub:type=\"bodymatter\" href=\"{ChapterToId(chapters[0])}.xhtml\">Start of Content</a></li>");
            sb.AppendLine("<li><a epub:type=\"index\" href=\"index.xhtml\">Index</a></li>");
            sb.AppendLine("</ol>");
            sb.AppendLine("</nav>");
            sb.AppendLine("</body>");
            sb.AppendLine("</html>");
            
            File.WriteAllText(outputPath, sb.ToString(), Encoding.UTF8);
        }

        private void FlushIndexPage()
        {
            var outputPath = Path.Combine(_state.Settings["output"], "EPUB", "index.xhtml");
            var sb = new StringBuilder();
            
            sb.AppendLine("<!DOCTYPE html>");
            sb.AppendLine("<html xmlns=\"http://www.w3.org/1999/xhtml\" xmlns:epub=\"http://www.idpf.org/2007/ops\" xml:lang=\"en-US\" lang=\"en-US\">");
            sb.AppendLine("<head>");
            sb.AppendLine("<meta http-equiv=\"default-style\" content=\"text/html; charset=utf-8\"/>");
            sb.AppendLine("<title>Jornal do Caloni: Write for computers, people and food.</title>");
            sb.AppendLine("<link rel=\"stylesheet\" href=\"css/stylesheet.css\" type=\"text/css\" />");
            sb.AppendLine("<link rel=\"stylesheet\" href=\"css/page-template.xpgt\" type=\"application/adobe-page-template+xml\" />");
            sb.AppendLine("</head>");
            sb.AppendLine("<body>");
            sb.AppendLine("<h1 class=\"index-title\"><span epub:type=\"pagebreak\" id=\"idx\" title=\"Index\"/><a href=\"toc.xhtml#indx-1\"><strong>Index</strong></a></h1>");
            sb.AppendLine("<section epub:type=\"index-group\" id=\"letters\">");
            
            // Build letter index
            int currentId = 2;
            foreach (var slug in _state.Index.Keys.OrderBy(k => k))
            {
                var post = _state.Index[slug];
                if (string.IsNullOrEmpty(post.Date))
                    continue;
                    
                var letter = CharacterToLetter(post.Letter);
                
                if (!_letters.ContainsKey(letter))
                {
                    _letters[letter] = $"<h3 id=\"{SlugToId(letter)}\" class=\"groupletter\">{StringUtilities.TextToHtml(letter)}</h3>\n<ul class=\"indexlevel1\">";
                }
                
                _letters[letter] += $"<li epub:type=\"index-entry\" class=\"indexhead1\" id=\"mh{currentId++}\">" +
                                   $"<a href=\"{ChapterToId(post.Chapter ?? "")}.xhtml#{SlugToId(slug)}\">{StringUtilities.TextToHtml(post.Title ?? "")}</a></li>\n";
            }
            
            // Letter navigation
            foreach (var letter in _letters.Keys.OrderBy(k => k))
            {
                sb.Append($"<a href=\"#{SlugToId(letter)}\">{letter}</a>");
            }
            
            // Tags section
            sb.AppendLine("<h3 id=\"toc_tags\" class=\"groupletter\">Tags</h3>\n<div class=\"indexlevel1\">");
            foreach (var tag in _titlesByTags.Keys.OrderBy(k => k))
            {
                sb.AppendLine($"<span epub:type=\"index-entry\" class=\"indexhead1\" id=\"mh{currentId++}\">" +
                             $"<a href=\"toc{SlugToId(tag)}.xhtml\">{StringUtilities.TextToHtml(tag)}</a></span>");
            }
            sb.AppendLine("</div>");
            
            // Print letter sections
            foreach (var letter in _letters.Keys.OrderBy(k => k))
            {
                sb.Append(_letters[letter]);
                sb.AppendLine("</ul>");
            }
            
            sb.AppendLine("</section>");
            sb.AppendLine("</body>");
            sb.AppendLine("</html>");
            
            File.WriteAllText(outputPath, sb.ToString(), Encoding.UTF8);
        }

        private string ChapterToId(string chapter)
        {
            return StringUtilities.ChapterToId(chapter);
        }

        private string SlugToId(string slug)
        {
            return StringUtilities.SlugToId(slug);
        }

        private string CharacterToLetter(string? chr)
        {
            if (string.IsNullOrEmpty(chr))
                return "#";
                
            var c = chr[0];
            if (char.IsDigit(c))
                return "#";
                
            c = char.ToUpper(c);

            // Normalize accented Latin letters (Á, É, Ó, ...) to their base letter via Unicode
            // decomposition rather than a hardcoded character table, which is one bad file
            // save/encoding round-trip away from silently losing its accented keys.
            var decomposed = c.ToString().Normalize(System.Text.NormalizationForm.FormD);
            c = decomposed[0];

            if ("()\'\"".Contains(c))
                return "#";
                
            return c.ToString();
        }

        private string GetImageMediaType(string filename)
        {
            if (filename.EndsWith(".jpg") || filename.EndsWith(".jpeg"))
                return "image/jpeg";
            if (filename.EndsWith(".png"))
                return "image/png";
            if (filename.EndsWith(".gif"))
                return "image/gif";
            if (filename.EndsWith(".svg"))
                return "image/svg+xml";
            return "image/jpeg";
        }
    }
}

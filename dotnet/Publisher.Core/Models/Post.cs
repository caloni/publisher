namespace Publisher.Core.Models
{
    /// <summary>
    /// A journal post produced by the parser.
    /// Holds the raw parsed data and the rendering metadata populated during state registration.
    /// </summary>
    public class Post
    {
        // ── Parsed data (set by the parser) ────────────────────────────────

        /// <summary>Post title, taken from the H1 line.</summary>
        public string? Title { get; set; }

        /// <summary>URL-safe slug derived from <see cref="Title"/>.</summary>
        public string? Slug { get; set; }

        /// <summary>Publication date in <c>YYYY-MM-DD</c> format (required for visible posts).</summary>
        public string? Date { get; set; }

        /// <summary>Month portion of <see cref="Date"/> in <c>YYYY-MM</c> format.</summary>
        public string? Month { get; set; }

        /// <summary>Optional external URL declared in the post header or loaded from YAML.</summary>
        public string? ExternalLink { get; set; }

        /// <summary><c>true</c> when <see cref="ExternalLink"/> originated from YAML rather than the inline header field.</summary>
        public bool ExternalLinkFromYaml { get; set; }

        /// <summary>Post tags as an ordered list (e.g. <c>["cinema", "draft"]</c>).</summary>
        public List<string> Tags { get; set; }

        /// <summary>Structured content blocks of the post body.</summary>
        public List<PostBlock> Blocks { get; set; }

        // ── Rendering metadata (populated during state registration) ───────

        /// <summary>Relative HTML link to this post (e.g. <c>2024-01.html#my-slug</c>).</summary>
        public string? Link { get; set; }

        /// <summary>Plain-text excerpt used in index/search listings (first ~200 chars).</summary>
        public string? Summary { get; set; }

        /// <summary>Filename of the first image found in the post body.</summary>
        public string? Image { get; set; }

        /// <summary>First character of <see cref="Title"/>, used for alphabetical indexes.</summary>
        public string? Letter { get; set; }

        /// <summary>EPUB chapter identifier (set by <c>BookWriter</c>).</summary>
        public string? Chapter { get; set; }

        /// <summary>Internal-link definitions scoped to this post (label → target slug or URL).</summary>
        public Dictionary<string, string> Links { get; set; }

        /// <summary>Insertion order for <see cref="Links"/> entries, used by the passthrough writer.</summary>
        public Dictionary<string, int> LinkOrder { get; set; }

        /// <summary>Per-tag previous/next navigation links.</summary>
        public Dictionary<string, TagNavigation> TagNavigation { get; set; }

        public Post()
        {
            Tags = new List<string>();
            Blocks = new List<PostBlock>();
            Links = new Dictionary<string, string>();
            LinkOrder = new Dictionary<string, int>();
            TagNavigation = new Dictionary<string, TagNavigation>();
        }
    }

    /// <summary>
    /// A single structural block of content within a <see cref="Post"/>.
    /// </summary>
    public class PostBlock
    {
        /// <summary>The structural role of this block.</summary>
        public BlockKind Kind { get; set; }

        /// <summary>
        /// Text content of the block.
        /// May contain inline HTML after the rendering pass (e.g. resolved <c>&lt;a&gt;</c> tags).
        /// </summary>
        public string Content { get; set; } = "";

        /// <summary>
        /// Auxiliary metadata:
        /// <list type="bullet">
        ///   <item><description><see cref="BlockKind.Image"/> — the image alt text.</description></item>
        ///   <item><description><see cref="BlockKind.Code"/> — the fenced-code language identifier.</description></item>
        /// </list>
        /// </summary>
        public string? Meta { get; set; }
    }

    /// <summary>Per-tag previous/next navigation state for a <see cref="Post"/>.</summary>
    public class TagNavigation
    {
        public string? PreviousInTag { get; set; }
        public string? NextInTag { get; set; }
    }
}
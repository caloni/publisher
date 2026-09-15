namespace Publisher.Core.Models
{
    /// <summary>
    /// Identifies the structural role of a content block within a <see cref="Post"/>.
    /// </summary>
    public enum BlockKind
    {
        /// <summary>Regular paragraph — emits <c>&lt;p&gt;</c>.</summary>
        Paragraph,

        /// <summary>Second-level heading — emits <c>&lt;h2&gt;</c>.</summary>
        Heading2,

        /// <summary>Third-level heading — emits <c>&lt;h3&gt;</c>.</summary>
        Heading3,

        /// <summary>Fourth-level heading — emits <c>&lt;h4&gt;</c>.</summary>
        Heading4,

        /// <summary>Fifth-level heading — emits <c>&lt;h5&gt;</c>.</summary>
        Heading5,

        /// <summary>Sixth-level heading — emits <c>&lt;h6&gt;</c>.</summary>
        Heading6,

        /// <summary>Fenced or indented code block — emits <c>&lt;pre&gt;</c>.</summary>
        Code,

        /// <summary>Block-quote — emits <c>&lt;blockquote&gt;</c>.</summary>
        Blockquote,

        /// <summary>
        /// Ordered-list item.
        /// Note: the parser currently renders ordered lists as <see cref="Paragraph"/>
        /// blocks with a numeric prefix; this value is reserved for future use.
        /// </summary>
        OrderedList,

        /// <summary>Unordered-list item — emits <c>&lt;li&gt;</c> inside <c>&lt;ul&gt;</c>.</summary>
        UnorderedList,

        /// <summary>Inline image — emits <c>&lt;img&gt;</c>.</summary>
        Image,

        /// <summary>Thematic break — emits <c>&lt;hr&gt;</c>.</summary>
        HorizontalRule
    }

    /// <summary>
    /// Extension helpers for <see cref="BlockKind"/>.
    /// </summary>
    public static class BlockKindExtensions
    {
        /// <summary>Returns <c>true</c> for any heading level (H2–H6).</summary>
        public static bool IsHeading(this BlockKind kind) =>
            kind >= BlockKind.Heading2 && kind <= BlockKind.Heading6;

        /// <summary>Returns the numeric heading level (2–6), or 0 for non-headings.</summary>
        public static int HeadingLevel(this BlockKind kind) => kind switch
        {
            BlockKind.Heading2 => 2,
            BlockKind.Heading3 => 3,
            BlockKind.Heading4 => 4,
            BlockKind.Heading5 => 5,
            BlockKind.Heading6 => 6,
            _                  => 0
        };

        /// <summary>
        /// Returns the lowercase HTML tag name (e.g. <c>"h2"</c>, <c>"p"</c>, <c>"pre"</c>),
        /// or an empty string for kinds without a single canonical tag.
        /// </summary>
        public static string HtmlTag(this BlockKind kind) => kind switch
        {
            BlockKind.Heading2      => "h2",
            BlockKind.Heading3      => "h3",
            BlockKind.Heading4      => "h4",
            BlockKind.Heading5      => "h5",
            BlockKind.Heading6      => "h6",
            BlockKind.Paragraph     => "p",
            BlockKind.Code          => "pre",
            BlockKind.Blockquote    => "blockquote",
            BlockKind.UnorderedList => "ul",
            BlockKind.OrderedList   => "ol",
            BlockKind.Image         => "img",
            BlockKind.HorizontalRule => "hr",
            _                        => ""
        };
    }
}

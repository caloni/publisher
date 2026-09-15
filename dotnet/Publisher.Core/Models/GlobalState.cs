using System.Collections.Generic;

namespace Publisher.Core.Models
{
    /// <summary>
    /// Holds the full parsed journal state shared between the parser and the writers.
    /// </summary>
    public class GlobalState
    {
        /// <summary>All posts indexed by slug.</summary>
        public Dictionary<string, Post> Index { get; set; }

        /// <summary>Slugs in the order they were parsed (chronological appearance in source files).</summary>
        public List<string> PostSlugsInOrder { get; set; }

        /// <summary>Date → slug → title mapping, used to build index and tag pages.</summary>
        public Dictionary<string, Dictionary<string, string>> DateSlugTitle { get; set; }

        /// <summary>Tag → date → slug mapping, used to build per-tag pages and navigation.</summary>
        public Dictionary<string, Dictionary<string, Dictionary<string, string>>> SlugsByTagsAndDates { get; set; }

        /// <summary>Title → slug mapping, used to resolve quoted-title internal links.</summary>
        public Dictionary<string, string> TitleToSlug { get; set; }

        /// <summary>Title → every slug registered under that title, in registration order.
        /// Parallels <see cref="TitleToSlug"/> but keeps all matches, so a quoted-title
        /// reference can detect when a title is ambiguous (shared by more than one post).</summary>
        public Dictionary<string, List<string>> TitleToSlugs { get; set; }

        /// <summary>Description → disambiguating date (YYYY-MM-DD), loaded from the optional
        /// "date" field in links.yaml. Used to pick the intended post when a quoted title
        /// matches more than one post (see <see cref="TitleToSlugs"/>).</summary>
        public Dictionary<string, string> LinkDates { get; set; }

        /// <summary>Set of month keys (YYYY-MM) that have at least one published post.</summary>
        public Dictionary<string, string> Months { get; set; }

        /// <summary>Image filenames referenced by posts, collected for the EPUB manifest.</summary>
        public Dictionary<string, string> PostsImages { get; set; }

        /// <summary>Per-slug metadata: chapter assignment and reference-usage count.</summary>
        public Dictionary<string, PostMetadata> PostMetadata { get; set; }

        /// <summary>Stringly-typed configuration and runtime settings bag.</summary>
        public Dictionary<string, string> Settings { get; set; }

        /// <summary>
        /// External resources loaded from YAML (description → URL or image path).
        /// Resolution semantics are determined at render time:
        /// a description matching a post title becomes an internal link,
        /// a standalone quoted paragraph becomes an image,
        /// and an inline quoted description becomes an external link.
        /// </summary>
        public Dictionary<string, string> Links { get; set; }

        public GlobalState()
        {
            Index = new Dictionary<string, Post>();
            PostSlugsInOrder = new List<string>();
            DateSlugTitle = new Dictionary<string, Dictionary<string, string>>();
            SlugsByTagsAndDates = new Dictionary<string, Dictionary<string, Dictionary<string, string>>>();
            TitleToSlug = new Dictionary<string, string>();
            TitleToSlugs = new Dictionary<string, List<string>>();
            LinkDates = new Dictionary<string, string>();
            Months = new Dictionary<string, string>();
            PostsImages = new Dictionary<string, string>();
            PostMetadata = new Dictionary<string, PostMetadata>();
            Settings = new Dictionary<string, string>();
            Links = new Dictionary<string, string>();
        }
    }
}
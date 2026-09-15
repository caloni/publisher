namespace Publisher.Core.Models
{
    /// <summary>
    /// Ancillary data tracked per post slug after registration.
    /// Used for navigation, EPUB chapter assignment and reference-usage counting.
    /// </summary>
    public class PostMetadata
    {
        /// <summary>
        /// The chapter (month) this post belongs to, e.g. <c>"2024-01"</c>.
        /// Null for posts that have not been assigned a chapter.
        /// </summary>
        public string? Chapter { get; set; }

        /// <summary>Number of times this post has been referenced by other posts.</summary>
        public int Used { get; set; }
    }
}

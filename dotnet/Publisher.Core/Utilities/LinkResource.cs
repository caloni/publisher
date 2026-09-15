namespace Publisher.Core.Utilities
{
    public enum LinkKind
    {
        Url,
        ImagePath,
        LinkPath
    }

    /// <summary>
    /// Classifies a link value so that each resolution site can accept only the
    /// semantically correct kind.
    ///
    /// Rules:
    ///   - Url       : value starts with "http" (http:// or https://)
    ///                 eligible for external links and blogging links, NOT for image paths.
    ///   - ImagePath : non-http value with a known image file extension (.jpg, .png, etc.)
    ///                 eligible for image sources, NOT for <a href> link targets.
    ///   - LinkPath  : non-http value without an image extension (e.g. a post slug)
    ///                 eligible for <a href> link targets, NOT for image sources.
    /// </summary>
    public static class LinkResource
    {
        private static readonly HashSet<string> ImageExtensions = new(StringComparer.OrdinalIgnoreCase)
        {
            ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".webp", ".svg", ".ico", ".tiff", ".tif"
        };

        public static LinkKind GetKind(string link)
        {
            if (link.StartsWith("http", StringComparison.OrdinalIgnoreCase))
                return LinkKind.Url;
            var ext = System.IO.Path.GetExtension(link);
            return ImageExtensions.Contains(ext) ? LinkKind.ImagePath : LinkKind.LinkPath;
        }

        public static bool IsUrl(string link) => GetKind(link) == LinkKind.Url;

        public static bool IsImagePath(string link) => GetKind(link) == LinkKind.ImagePath;

        public static bool IsLocalPath(string link) => GetKind(link) != LinkKind.Url;
    }
}

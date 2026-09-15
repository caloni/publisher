using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Publisher.Core.Utilities
{
    /// <summary>
    /// Loads link mappings from a YAML file.
    /// Each entry maps a description to a link (URL or image path).
    /// The semantics (external link, blogging link, or image) are determined by context at use time.
    /// </summary>
    public static class LinksLoader
    {
        /// <summary>
        /// Loads links from a YAML file.
        /// Returns a dictionary mapping description text to link (URL or path).
        /// The YAML file must contain a top-level list of items with 'description' and 'link' fields.
        /// </summary>
        public static Dictionary<string, string> Load(string filePath)
        {
            var result = new Dictionary<string, string>();
            foreach (var entry in LoadEntries(filePath))
            {
                if (!string.IsNullOrWhiteSpace(entry.Description) && !string.IsNullOrWhiteSpace(entry.Link))
                    result[entry.Description] = entry.Link;
            }

            return result;
        }

        /// <summary>
        /// Loads the optional disambiguating "date" field from a YAML file.
        /// Returns a mapping of description → date (YYYY-MM-DD) for entries that declared one.
        /// Used to pick a specific post when a quoted title matches more than one post
        /// sharing the same title.
        /// </summary>
        public static Dictionary<string, string> LoadDates(string filePath)
        {
            var result = new Dictionary<string, string>();
            foreach (var entry in LoadEntries(filePath))
            {
                if (!string.IsNullOrWhiteSpace(entry.Description) && !string.IsNullOrWhiteSpace(entry.Date))
                    result[entry.Description] = entry.Date;
            }

            return result;
        }

        private static List<LinkEntry> LoadEntries(string filePath)
        {
            var yaml = File.ReadAllText(filePath);

            var deserializer = new DeserializerBuilder()
                .WithNamingConvention(CamelCaseNamingConvention.Instance)
                .IgnoreUnmatchedProperties()
                .Build();

            return deserializer.Deserialize<List<LinkEntry>>(yaml) ?? new List<LinkEntry>();
        }

        private class LinkEntry
        {
            public string Description { get; set; } = "";
            public string Link { get; set; } = "";

            /// <summary>Optional disambiguating date (YYYY-MM-DD) when Description matches
            /// more than one post title.</summary>
            public string? Date { get; set; }
        }
    }
}

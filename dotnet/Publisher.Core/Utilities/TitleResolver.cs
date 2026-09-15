using System.Linq;
using Publisher.Core.Models;

namespace Publisher.Core.Utilities
{
    /// <summary>
    /// Resolves a quoted-title reference (e.g. "Os Suspeitos") to the single post slug it
    /// should link to, handling the case where more than one post shares the same title.
    /// </summary>
    public static class TitleResolver
    {
        /// <summary>
        /// Resolves <paramref name="title"/> to a post slug.
        /// When the title is ambiguous (registered by more than one post), the optional
        /// "date" field of a matching links.yaml entry (same description) is used to pick
        /// the intended post. If no disambiguating date is available, or it does not match
        /// any of the candidate posts, <paramref name="warning"/> is set so the caller can
        /// surface it, and the most recently registered post is used as a best effort.
        /// </summary>
        /// <returns>The resolved slug, or null if the title matches no post at all.</returns>
        public static string? Resolve(GlobalState state, string title, out string? warning)
        {
            warning = null;

            if (!state.TitleToSlugs.TryGetValue(title, out var slugs) || slugs.Count == 0)
                return state.TitleToSlug.TryGetValue(title, out var singleSlug) ? singleSlug : null;

            if (slugs.Count == 1)
                return slugs[0];

            if (state.LinkDates.TryGetValue(title, out var date))
            {
                var match = slugs.FirstOrDefault(s => state.Index.TryGetValue(s, out var p) && p.Date == date);
                if (match != null)
                    return match;

                warning = $"Warning: quoted title \"{title}\" matches {slugs.Count} posts, but the date " +
                          $"\"{date}\" given for it in links.yaml does not match any of them ({string.Join(", ", slugs)}).";
                return state.TitleToSlug.TryGetValue(title, out var fallback) ? fallback : slugs[^1];
            }

            warning = $"Warning: quoted title \"{title}\" matches {slugs.Count} posts ({string.Join(", ", slugs)}) " +
                      "and links.yaml has no disambiguating \"date\" entry for it. Add one to pick the intended post.";
            return state.TitleToSlug.TryGetValue(title, out var lastSlug) ? lastSlug : slugs[^1];
        }
    }
}

using System.Text;
using System.Text.RegularExpressions;

namespace Publisher.Core.Utilities
{
    /// <summary>
    /// String utility helpers
    /// </summary>
    public static class StringUtilities
    {
        public static string TitleToSlug(string title)
        {
            // Decompose accented characters (é→e, ñ→n, etc.) via Unicode normalization
            title = new string(
                title.Normalize(System.Text.NormalizationForm.FormD)
                     .Where(c => System.Globalization.CharUnicodeInfo.GetUnicodeCategory(c)
                                 != System.Globalization.UnicodeCategory.NonSpacingMark)
                     .ToArray());
            title = Regex.Replace(title, @"&", "and");
            title = Regex.Replace(title, @"%", "perc");
            title = Regex.Replace(title, @"<", "lt");
            title = Regex.Replace(title, @">", "gt");
            title = Regex.Replace(title, @"\+", "p");
            title = Regex.Replace(title, @"[^a-zA-Z0-9\s]", "");
            title = title.Trim();
            title = Regex.Replace(title, @"\s+", "-");
            title = Regex.Replace(title, @"-+", "-");
            title = title.Trim('-');

            return title.ToLower();
        }

        public static string TextToHtml(string text)
        {
            text = text.Replace("&", "&amp;");
            text = text.Replace("<", "&lt;");
            text = text.Replace(">", "&gt;");
            text = text.Replace("\"", "&quot;");
            return text;
        }

        public static string SlugToId(string slug)
        {
            var id = slug.Replace("-", "_").Replace(" ", "_").Replace("#", "sharp");
            return "_" + id;
        }

        public static string ChapterToId(string chapter)
        {
            var id = chapter.Replace("-", "").Replace(" ", "_").Replace("#", "sharp");
            return "_" + id;
        }
    }
}
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace Publisher.Core.Utilities
{
    public static class TemplateEngine
    {
        public static string Render(string template, Dictionary<string, object> context)
        {
            // Handle foreach blocks
            template = Regex.Replace(template,
                @"<!-- foreach:(\w+) begin -->(.*?)<!-- foreach:\1 end -->",
                match =>
                {
                    var key = match.Groups[1].Value;
                    var block = match.Groups[2].Value;
                    if (context.TryGetValue(key, out var value) && value is IEnumerable<Dictionary<string, object>> list)
                    {
                        var sb = new StringBuilder();
                        foreach (var item in list)
                        {
                            // Merge parent context for nested replacements
                            var merged = new Dictionary<string, object>(context);
                            foreach (var kv in item)
                                merged[kv.Key] = kv.Value;
                            sb.Append(Render(block, merged));
                        }
                        return sb.ToString();
                    }
                    return string.Empty;
                },
                RegexOptions.Singleline);

            // Handle simple placeholders: {{key}}
            template = Regex.Replace(template, @"\{\{(\w+)\}\}", match =>
            {
                var key = match.Groups[1].Value;
                return context.TryGetValue(key, out var value) ? value?.ToString() ?? "" : "";
            });

            return template;
        }
    }
}
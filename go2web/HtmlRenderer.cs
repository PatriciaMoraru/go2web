using System.Text;
using System.Text.Json;
using System.Web;
using HtmlAgilityPack;

namespace go2web;

public static class HtmlRenderer
{
    public static string Render(string body, string contentType)
    {
        // --- JSON response ---
        if (contentType.Contains("application/json", StringComparison.OrdinalIgnoreCase) ||
            contentType.Contains("text/json", StringComparison.OrdinalIgnoreCase))
        {
            return PrettyPrintJson(body);
        }

        // --- Plain text ---
        if (contentType.Contains("text/plain", StringComparison.OrdinalIgnoreCase))
        {
            return body.Trim();
        }

        // --- HTML (default) ---
        return RenderHtml(body);
    }

    private static string RenderHtml(string html)
    {
        var doc = new HtmlDocument();
        doc.LoadHtml(html);

        // Remove script, style, head tags entirely — we don't want their content
        var nodesToRemove = doc.DocumentNode
            .SelectNodes("//script|//style|//head") ?? new HtmlNodeCollection(null);

        foreach (var node in nodesToRemove.ToList())
        {
            node.Remove();
        }

        var sb = new StringBuilder();
        RenderNode(doc.DocumentNode, sb);

        // Clean up: collapse multiple blank lines into one
        string result = sb.ToString();
        result = System.Text.RegularExpressions.Regex.Replace(result, @"\n{3,}", "\n\n");
        return result.Trim();
    }

    private static void RenderNode(HtmlNode node, StringBuilder sb)
    {
        switch (node.NodeType)
        {
            case HtmlNodeType.Text:
                string text = HttpUtility.HtmlDecode(node.InnerText);
                if (!string.IsNullOrWhiteSpace(text))
                    sb.Append(text);
                break;

            case HtmlNodeType.Element:
                string tag = node.Name.ToLower();

                // Block elements — print on their own line
                if (IsBlock(tag))
                    sb.Append('\n');

                // Headings — add a marker
                if (tag is "h1" or "h2" or "h3" or "h4" or "h5" or "h6")
                {
                    sb.Append('\n');
                    int level = int.Parse(tag[1].ToString());
                    sb.Append(new string('#', level) + " ");
                }

                // Links — show URL after text
                if (tag == "a")
                {
                    string href = node.GetAttributeValue("href", "");
                    foreach (var child in node.ChildNodes)
                        RenderNode(child, sb);
                    if (!string.IsNullOrWhiteSpace(href))
                        sb.Append($" [{href}]");
                    return; // already handled children
                }

                // List items — add bullet
                if (tag == "li")
                    sb.Append("\n  • ");

                // Horizontal rule
                if (tag == "hr")
                {
                    sb.Append('\n' + new string('─', 40) + '\n');
                    return;
                }

                // Line break
                if (tag == "br")
                {
                    sb.Append('\n');
                    return;
                }

                // Recurse into children
                foreach (var child in node.ChildNodes)
                    RenderNode(child, sb);

                // Close block elements with newline
                if (IsBlock(tag))
                    sb.Append('\n');

                break;

            default:
                // Document node — just recurse
                foreach (var child in node.ChildNodes)
                    RenderNode(child, sb);
                break;
        }
    }

    private static bool IsBlock(string tag) => tag is
        "p" or "div" or "section" or "article" or "main" or
        "header" or "footer" or "nav" or "aside" or
        "ul" or "ol" or "li" or "blockquote" or
        "h1" or "h2" or "h3" or "h4" or "h5" or "h6" or
        "table" or "tr" or "td" or "th" or
        "form" or "pre" or "figure" or "figcaption";

    private static string PrettyPrintJson(string json)
    {
        try
        {
            var doc = JsonDocument.Parse(json);
            return JsonSerializer.Serialize(doc, new JsonSerializerOptions
            {
                WriteIndented = true
            });
        }
        catch
        {
            return json; // Not valid JSON — return as-is
        }
    }
}
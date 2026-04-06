using System.Web;
using HtmlAgilityPack;

namespace go2web;

public static class SearchEngine
{
    public static async Task<List<SearchResult>> SearchAsync(string term)
    {
        string encoded = HttpUtility.UrlEncode(term);
        string searchUrl = $"https://html.duckduckgo.com/html/?q={encoded}";

        var response = await TcpHttpClient.GetAsync(searchUrl);

        if (response.StatusCode != 200)
        {
            Console.Error.WriteLine($"Search failed with status {response.StatusCode}");
            return [];
        }

        return ParseResults(response.Body);
    }

    private static List<SearchResult> ParseResults(string html)
    {
        var doc = new HtmlDocument();
        doc.LoadHtml(html);

        var results = new List<SearchResult>();

        var resultNodes = doc.DocumentNode
            .SelectNodes("//div[contains(@class,'result__body')]");

        if (resultNodes == null) return results;

        foreach (var resultBody in resultNodes)
        {
            if (results.Count >= 10) break;

            // Title + URL from result__a
            var titleNode = resultBody.SelectSingleNode(".//a[contains(@class,'result__a')]");
            if (titleNode == null) continue;

            string title = HttpUtility.HtmlDecode(titleNode.InnerText.Trim());
            string rawHref = titleNode.GetAttributeValue("href", "");
            string url = UnwrapDuckDuckGoUrl(rawHref);

            if (string.IsNullOrWhiteSpace(title) || string.IsNullOrWhiteSpace(url))
                continue;
            if (url.Contains("duckduckgo.com"))
                continue;

            // Snippet from result__snippet
            string snippet = "";
            var snippetNode = resultBody.SelectSingleNode(
                ".//a[contains(@class,'result__snippet')]|.//div[contains(@class,'result__snippet')]"
            );
            if (snippetNode != null)
                snippet = HttpUtility.HtmlDecode(snippetNode.InnerText.Trim());

            results.Add(new SearchResult(title, url, snippet));
        }

        return results;
    }

    // DuckDuckGo uses protocol-relative redirect URLs like:
    // //duckduckgo.com/l/?uddg=https%3A%2F%2Factualsite.com&rut=...
    private static string UnwrapDuckDuckGoUrl(string href)
    {
        if (string.IsNullOrWhiteSpace(href)) return "";

        // Already absolute
        if (href.StartsWith("http://") || href.StartsWith("https://"))
            return href;

        // Protocol-relative DDG redirect — prepend https: to parse it
        string absolute = href.StartsWith("//") ? "https:" + href : "https://duckduckgo.com" + href;

        if (Uri.TryCreate(absolute, UriKind.Absolute, out var uri))
        {
            // Extract uddg param which holds the real URL
            string query = uri.Query.TrimStart('?');
            foreach (var param in query.Split('&'))
            {
                int eq = param.IndexOf('=');
                if (eq == -1) continue;
                string key = param[..eq];
                string value = HttpUtility.UrlDecode(param[(eq + 1)..]);
                if (key == "uddg" && !string.IsNullOrWhiteSpace(value))
                    return value;
            }
        }

        return "";
    }
}

public record SearchResult(string Title, string Url, string Snippet);
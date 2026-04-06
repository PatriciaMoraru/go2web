using System.Net.Security;
using System.Net.Sockets;
using System.Text;

namespace go2web;

public static class TcpHttpClient
{
    private const int MaxRedirects = 10;

    public static async Task<HttpResponse> GetAsync(string url)
    {
        return await GetAsync(url, 0);
    }

    private static async Task<HttpResponse> GetAsync(string url, int redirectCount)
    {
        // --- Guard against infinite redirect loops ---
        if (redirectCount > MaxRedirects)
        {
            Console.Error.WriteLine($"Error: too many redirects (>{MaxRedirects})");
            Environment.Exit(1);
        }

        if (!url.StartsWith("http://") && !url.StartsWith("https://"))
            url = "https://" + url;

        var uri = new Uri(url);
        string host = uri.Host;
        string pathAndQuery = uri.PathAndQuery;
        bool isHttps = uri.Scheme == "https";
        int port = uri.IsDefaultPort ? (isHttps ? 443 : 80) : uri.Port;

        using var tcpClient = new TcpClient();
        await tcpClient.ConnectAsync(host, port);

        Stream stream;
        if (isHttps)
        {
            var sslStream = new SslStream(tcpClient.GetStream(), leaveInnerStreamOpen: false);
            await sslStream.AuthenticateAsClientAsync(host);
            stream = sslStream;
        }
        else
        {
            stream = tcpClient.GetStream();
        }

        string request = $"GET {pathAndQuery} HTTP/1.1\r\n" +
                         $"Host: {host}\r\n" +
                         "Connection: close\r\n" +
                         "User-Agent: go2web/1.0\r\n" +
                         "Accept: text/html,application/json\r\n" +
                         "Accept-Encoding: gzip\r\n" +
                         "\r\n";

        byte[] requestBytes = Encoding.ASCII.GetBytes(request);
        await stream.WriteAsync(requestBytes);

        using var memoryStream = new MemoryStream();
        await stream.CopyToAsync(memoryStream);
        byte[] responseBytes = memoryStream.ToArray();

        var response = HttpResponseParser.Parse(responseBytes);

        // --- Handle redirects ---
        if (response.StatusCode is 301 or 302 or 303 or 307 or 308)
        {
            if (!response.Headers.TryGetValue("Location", out string? location) ||
                string.IsNullOrWhiteSpace(location))
            {
                Console.Error.WriteLine("Error: redirect with no Location header.");
                Environment.Exit(1);
            }

            // Location can be relative (e.g. "/new-path") or absolute ("https://other.com")
            string nextUrl = BuildRedirectUrl(url, location);

            Console.Error.WriteLine($"  → {response.StatusCode} redirect to {nextUrl}");
            return await GetAsync(nextUrl, redirectCount + 1);
        }

        return response;
    }

    // Resolves a redirect Location against the original URL.
    // Handles absolute URLs, protocol-relative URLs, and relative paths.
    private static string BuildRedirectUrl(string originalUrl, string location)
    {
        // Absolute URL — use as-is
        if (location.StartsWith("http://") || location.StartsWith("https://"))
            return location;

        // Protocol-relative — e.g. "//example.com/path"
        if (location.StartsWith("//"))
        {
            var original = new Uri(originalUrl);
            return original.Scheme + ":" + location;
        }

        // Relative path — resolve against original base
        var baseUri = new Uri(originalUrl);
        var resolved = new Uri(baseUri, location);
        return resolved.ToString();
    }
}
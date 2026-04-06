using System.Net.Security;
using System.Net.Sockets;
using System.Text;

namespace go2web;

public static class HttpClient
{
    private const int MaxRedirects = 10;

    public static async Task<HttpResponse> GetAsync(string url)
    {
        return await GetAsync(url, 0);
    }

    private static async Task<HttpResponse> GetAsync(string url, int redirectCount)
    {
        // --- 1. Parse the URL ---
        if (!url.StartsWith("http://") && !url.StartsWith("https://"))
            url = "https://" + url;

        var uri = new Uri(url);
        string host = uri.Host;
        string pathAndQuery = uri.PathAndQuery; // e.g. "/some/path?q=1"
        bool isHttps = uri.Scheme == "https";
        int port = uri.IsDefaultPort ? (isHttps ? 443 : 80) : uri.Port;

        // --- 2. Open TCP connection ---
        using var tcpClient = new TcpClient();
        await tcpClient.ConnectAsync(host, port);

        Stream stream;

        if (isHttps)
        {
            // Wrap in SSL for HTTPS
            var sslStream = new SslStream(tcpClient.GetStream(), leaveInnerStreamOpen: false);
            await sslStream.AuthenticateAsClientAsync(host);
            stream = sslStream;
        }
        else
        {
            stream = tcpClient.GetStream();
        }

        // --- 3. Build and send the raw HTTP request ---
        string request = $"GET {pathAndQuery} HTTP/1.1\r\n" +
                         $"Host: {host}\r\n" +
                         "Connection: close\r\n" +
                         "User-Agent: go2web/1.0\r\n" +
                         "Accept: text/html,application/json\r\n" +
                         "\r\n";

        byte[] requestBytes = Encoding.ASCII.GetBytes(request);
        await stream.WriteAsync(requestBytes);

        // --- 4. Read the full response ---
        using var memoryStream = new MemoryStream();
        await stream.CopyToAsync(memoryStream);
        byte[] responseBytes = memoryStream.ToArray();

        // Try UTF-8 first, fall back to latin-1
        string raw;
        try
        {
            raw = Encoding.UTF8.GetString(responseBytes);
        }
        catch
        {
            raw = Encoding.Latin1.GetString(responseBytes);
        }

        // --- 5. Return raw response for now ---
        // (Step 3 will properly parse this into headers/body)
        return new HttpResponse
        {
            RawResponse = raw,
            Body = raw
        };
    }
}
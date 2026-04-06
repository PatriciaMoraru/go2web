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

        // Read raw bytes — pass directly to parser
        using var memoryStream = new MemoryStream();
        await stream.CopyToAsync(memoryStream);
        byte[] responseBytes = memoryStream.ToArray();

        return HttpResponseParser.Parse(responseBytes);
    }
}
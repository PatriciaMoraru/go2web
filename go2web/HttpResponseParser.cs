using System.IO.Compression;
using System.Text;

namespace go2web;

public static class HttpResponseParser
{
    public static HttpResponse Parse(byte[] responseBytes)
    {
        // --- 1. Find where headers end and body begins ---
        // Headers and body are separated by \r\n\r\n
        byte[] separator = "\r\n\r\n"u8.ToArray();
        int separatorIndex = IndexOf(responseBytes, separator);

        if (separatorIndex == -1)
        {
            // Malformed response — return as-is
            return new HttpResponse
            {
                RawResponse = Encoding.UTF8.GetString(responseBytes)
            };
        }

        // --- 2. Split into header bytes and body bytes ---
        byte[] headerBytes = responseBytes[..separatorIndex];
        byte[] bodyBytes = responseBytes[(separatorIndex + 4)..];

        string headerSection = Encoding.ASCII.GetString(headerBytes);

        // --- 3. Parse status line ---
        string[] headerLines = headerSection.Split("\r\n");
        string statusLine = headerLines[0]; // e.g. "HTTP/1.1 200 OK"
        string[] statusParts = statusLine.Split(' ', 3);

        int statusCode = int.Parse(statusParts[1]);
        string statusMessage = statusParts.Length > 2 ? statusParts[2] : "";

        // --- 4. Parse headers into dictionary ---
        var headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (string line in headerLines.Skip(1))
        {
            int colonIndex = line.IndexOf(':');
            if (colonIndex == -1) continue;

            string key = line[..colonIndex].Trim();
            string value = line[(colonIndex + 1)..].Trim();
            headers[key] = value;
        }

        // --- 5. Handle Transfer-Encoding: chunked ---
        if (headers.TryGetValue("Transfer-Encoding", out string? transferEncoding) &&
            transferEncoding.Contains("chunked", StringComparison.OrdinalIgnoreCase))
        {
            bodyBytes = DecodeChunked(bodyBytes);
        }

        // --- 6. Handle Content-Encoding: gzip ---
        if (headers.TryGetValue("Content-Encoding", out string? contentEncoding) &&
            contentEncoding.Contains("gzip", StringComparison.OrdinalIgnoreCase))
        {
            bodyBytes = DecompressGzip(bodyBytes);
        }

        // --- 7. Decode body to string ---
        string body = Encoding.UTF8.GetString(bodyBytes);

        return new HttpResponse
        {
            StatusCode = statusCode,
            StatusMessage = statusMessage,
            Headers = headers,
            Body = body,
            RawResponse = Encoding.UTF8.GetString(responseBytes)
        };
    }

    // Reassembles a chunked HTTP body into a single byte array.
    // Chunked format: each chunk is [hex size]\r\n[data]\r\n, ending with 0\r\n\r\n
    private static byte[] DecodeChunked(byte[] data)
    {
        var result = new MemoryStream();
        int pos = 0;

        while (pos < data.Length)
        {
            // Read the chunk size line (hex number followed by \r\n)
            int lineEnd = IndexOf(data, "\r\n"u8.ToArray(), pos);
            if (lineEnd == -1) break;

            string sizeLine = Encoding.ASCII.GetString(data, pos, lineEnd - pos).Trim();

            // Strip chunk extensions (e.g. "a; ext=value" → "a")
            int semicolon = sizeLine.IndexOf(';');
            if (semicolon != -1) sizeLine = sizeLine[..semicolon];

            if (!int.TryParse(sizeLine, System.Globalization.NumberStyles.HexNumber, null, out int chunkSize))
                break;

            if (chunkSize == 0) break; // Last chunk

            pos = lineEnd + 2; // Move past \r\n
            result.Write(data, pos, chunkSize);
            pos += chunkSize + 2; // Move past chunk data and trailing \r\n
        }

        return result.ToArray();
    }

    private static byte[] DecompressGzip(byte[] data)
    {
        using var input = new MemoryStream(data);
        using var gzip = new GZipStream(input, CompressionMode.Decompress);
        using var output = new MemoryStream();
        gzip.CopyTo(output);
        return output.ToArray();
    }

    // Finds the index of a byte sequence inside a byte array, starting at 'start'
    private static int IndexOf(byte[] source, byte[] pattern, int start = 0)
    {
        for (int i = start; i <= source.Length - pattern.Length; i++)
        {
            bool found = true;
            for (int j = 0; j < pattern.Length; j++)
            {
                if (source[i + j] != pattern[j]) { found = false; break; }
            }
            if (found) return i;
        }
        return -1;
    }
}
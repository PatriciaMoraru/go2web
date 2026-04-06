using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace go2web;

public static class HttpCache
{
    private static readonly string CacheDir = Path.Combine(
        Directory.GetCurrentDirectory(), ".cache"
    );

    // Returns cached response if it exists and hasn't expired, otherwise null
    public static HttpResponse? Get(string url)
    {
        string path = GetCachePath(url);
        if (!File.Exists(path)) return null;

        try
        {
            string json = File.ReadAllText(path);
            var entry = JsonSerializer.Deserialize<CacheEntry>(json);
            if (entry == null) return null;

            // Check expiry
            if (DateTime.UtcNow > entry.ExpiresAt)
            {
                File.Delete(path);
                return null;
            }

            var response = entry.Response;
            response.FromCache = true;
            return response;
        }
        catch
        {
            // Corrupted cache file — delete and ignore
            File.Delete(path);
            return null;
        }
    }

    public static void Set(string url, HttpResponse response, int ttlSeconds = 300)
    {
        try
        {
            Directory.CreateDirectory(CacheDir);

            var entry = new CacheEntry
            {
                Url = url,
                CachedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddSeconds(ttlSeconds),
                Response = response
            };

            string json = JsonSerializer.Serialize(entry, new JsonSerializerOptions
            {
                WriteIndented = false
            });

            File.WriteAllText(GetCachePath(url), json);
        }
        catch
        {
            // Cache write failure is non-fatal — just skip it
        }
    }

    public static void Clear()
    {
        if (Directory.Exists(CacheDir))
            Directory.Delete(CacheDir, recursive: true);
        Console.WriteLine("Cache cleared.");
    }

    private static string GetCachePath(string url)
    {
        // MD5 hash of the URL → safe filename
        byte[] hash = MD5.HashData(Encoding.UTF8.GetBytes(url));
        string filename = Convert.ToHexString(hash).ToLower() + ".json";
        return Path.Combine(CacheDir, filename);
    }

    private class CacheEntry
    {
        public string Url { get; set; } = "";
        public DateTime CachedAt { get; set; }
        public DateTime ExpiresAt { get; set; }
        public HttpResponse Response { get; set; } = new();
    }
}
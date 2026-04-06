namespace go2web;

public class HttpResponse
{
    public int StatusCode { get; set; }
    public string StatusMessage { get; set; } = "";
    public Dictionary<string, string> Headers { get; set; } = new();
    public string Body { get; set; } = "";
    public string RawResponse { get; set; } = "";
    public bool FromCache { get; set; } = false;
}
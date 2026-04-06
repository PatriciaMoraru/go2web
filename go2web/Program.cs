const string help = """
                    go2web - a simple HTTP client

                    Usage:
                      go2web -u <URL>          Make an HTTP request to the URL and print the response
                      go2web -s <search-term>  Search using DuckDuckGo and print top 10 results
                      go2web -h                Show this help message

                    Examples:
                      go2web -u https://example.com
                      go2web -s artificial intelligence
                    """;

if (args.Length == 0 || args[0] == "-h")
{
    Console.WriteLine(help);
    return;
}

if (args[0] == "-u")
{
    if (args.Length < 2)
    {
        Console.Error.WriteLine("Error: -u requires a URL argument.");
        Console.Error.WriteLine("Usage: go2web -u <URL>");
        Environment.Exit(1);
    }

    string url = args[1];
    var response = await go2web.HttpClient.GetAsync(url);
    
    Console.WriteLine($"Status: {response.StatusCode} {response.StatusMessage}");
    Console.WriteLine($"Content-Type: {response.Headers.GetValueOrDefault("Content-Type", "unknown")}");
    Console.WriteLine(new string('-', 40));
    Console.WriteLine(response.Body);
    return;
}

if (args[0] == "-s")
{
    if (args.Length < 2)
    {
        Console.Error.WriteLine("Error: -s requires a search term.");
        Console.Error.WriteLine("Usage: go2web -s <search-term>");
        Environment.Exit(1);
    }

    string searchTerm = string.Join(" ", args[1..]);
    Console.WriteLine($"[stub] Searching for: {searchTerm}");
    return;
}

Console.Error.WriteLine($"Error: unknown argument '{args[0]}'");
Console.Error.WriteLine("Run 'go2web -h' for usage.");
Environment.Exit(1);
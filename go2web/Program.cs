using go2web;

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
    var response = await go2web.TcpHttpClient.GetAsync(url);
    string contentType = response.Headers.GetValueOrDefault("Content-Type", "text/html");
    string rendered = HtmlRenderer.Render(response.Body, contentType);
    Console.WriteLine(rendered);
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
    Console.WriteLine($"Searching for: {searchTerm}\n");

    var results = await SearchEngine.SearchAsync(searchTerm);

    if (results.Count == 0)
    {
        Console.WriteLine("No results found.");
        return;
    }

    // Display numbered results
    for (int i = 0; i < results.Count; i++)
    {
        var r = results[i];
        Console.WriteLine($"{i + 1,2}. {r.Title}");
        Console.WriteLine($"    {r.Url}");
        if (!string.IsNullOrWhiteSpace(r.Snippet))
            Console.WriteLine($"    {r.Snippet}");
        Console.WriteLine();
    }

    // Let the user pick a result to open
    while (true)
    {
        Console.Write("Open result (1-10) or q to quit: ");
        string? input = Console.ReadLine()?.Trim();

        if (input is null or "q" or "Q")
            break;

        if (int.TryParse(input, out int choice) &&
            choice >= 1 && choice <= results.Count)
        {
            string selectedUrl = results[choice - 1].Url;
            Console.WriteLine($"\nFetching: {selectedUrl}\n");

            var response = await TcpHttpClient.GetAsync(selectedUrl);
            string contentType = response.Headers.GetValueOrDefault("Content-Type", "text/html");
            string rendered = HtmlRenderer.Render(response.Body, contentType);
            Console.WriteLine(rendered);

            // After showing the page, ask again
            Console.WriteLine("\n--- Back to results ---\n");
            for (int i = 0; i < results.Count; i++)
            {
                var r = results[i];
                Console.WriteLine($"{i + 1,2}. {r.Title}");
                Console.WriteLine($"    {r.Url}");
                Console.WriteLine();
            }
        }
        else
        {
            Console.WriteLine($"Please enter a number between 1 and {results.Count}, or q to quit.");
        }
    }

    return;
}

Console.Error.WriteLine($"Error: unknown argument '{args[0]}'");
Console.Error.WriteLine("Run 'go2web -h' for usage.");
Environment.Exit(1);
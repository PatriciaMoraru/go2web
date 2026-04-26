# go2web

A command-line HTTP client built over raw TCP sockets in C#. No HTTP libraries used.

## Demo

![demo](demo.gif)

## Usage

```
go2web -u <URL>          # Fetch a URL and print human-readable response
go2web -s <search-term>  # Search DuckDuckGo and show top 10 results
go2web -h                # Show help
go2web --clear-cache     # Clear the response cache
```

## Examples

```bash
go2web -u https://example.com
go2web -u http://github.com
go2web -s artificial intelligence
go2web -s c# programming
```

## Features

- Raw HTTP/1.1 and HTTPS over `TcpClient` and `SslStream` — no `HttpClient`
- Chunked transfer encoding and gzip decompression
- Automatic redirect following (301, 302, 303, 307, 308)
- HTML rendered as human-readable text via HtmlAgilityPack
- JSON pretty-printed automatically
- DuckDuckGo search with top 10 results
- Open any search result by number
- File-based response cache with 5-minute TTL
- Single-file self-contained executable (no .NET install required)

## How to run

### From source

```bash
git clone https://github.com/YOUR_USERNAME/go2web.git
cd go2web/go2web
dotnet run -- -u https://example.com
dotnet run -- -s artificial intelligence
```

### As executable

```bash
dotnet publish -c Release
.\bin\Release\net9.0\win-x64\publish\go2web.exe -u https://example.com
```

### Add to PATH (run once, then use `go2web` from anywhere)

```powershell
cd "path\to\go2web\go2web"
$exePath = (Resolve-Path ".\bin\Release\net9.0\win-x64\publish").Path
[Environment]::SetEnvironmentVariable("PATH", $env:PATH + ";$exePath", "User")
```

Then reopen your terminal and run:

```bash
go2web -h
```

## Project structure

```
go2web/
├── Program.cs              # CLI entry point, argument parsing
├── TcpHttpClient.cs        # Raw TCP/TLS connection and request sending
├── HttpResponseParser.cs   # Parse status, headers, chunked body, gzip
├── HttpResponse.cs         # Response data model
├── HtmlRenderer.cs         # HTML → readable text, JSON pretty-print
├── SearchEngine.cs         # DuckDuckGo search and result extraction
├── HttpCache.cs            # File-based response cache with TTL
└── go2web.csproj           # Project config and dependencies
```

## Known limitations

- JavaScript-gated sites (Fandom, Instagram, YouTube) will not render correctly — they require a real browser to execute JS before serving content
- Some sites block non-browser user agents

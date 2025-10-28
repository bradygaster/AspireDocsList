using AspireDocsList.Agents;
using HtmlAgilityPack;

public class AspireDocsCrawler(SummarizationAgent summarizationAgent, HttpClient httpClient)
{
    private readonly HttpClient _httpClient = httpClient;
    private readonly int _limit = 10;
    private readonly string _rootUrl = "https://learn.microsoft.com/dotnet/aspire/";
    private readonly string _outputDir = Path.Combine(Directory.GetCurrentDirectory(), "instructions");

    public async Task RunAsync(int limit = -1)
    {
        _httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (compatible; CrawlerBot/1.0)");
        Directory.CreateDirectory(_outputDir);

        limit = limit > 0 ? limit : _limit;
        var baseUri = new Uri(_rootUrl);
        var visitedUrls = new HashSet<string>();
        var urlsToVisit = new Queue<Uri>();
        urlsToVisit.Enqueue(baseUri);

        while (urlsToVisit.Count > 0)
        {
            var currentUri = urlsToVisit.Dequeue();
            if (visitedUrls.Contains(currentUri.AbsoluteUri))
                continue;
            visitedUrls.Add(currentUri.AbsoluteUri);
            try
            {
                var response = await _httpClient.GetStringAsync(currentUri);
                var doc = new HtmlDocument();
                doc.LoadHtml(response);
                var links = doc.DocumentNode.SelectNodes("//a[@href]")?
                .Select(a => a.GetAttributeValue("href", "").Trim())
                .Where(href => !string.IsNullOrEmpty(href))
                .Select(href => href.StartsWith("http") ? new Uri(href) : new Uri(baseUri, href))
                .Where(uri => uri.AbsoluteUri.Contains("learn.microsoft.com/dotnet/aspire"))
                .Distinct();
                if (links != null)
                {
                    foreach (var link in links)
                    {
                        if (!visitedUrls.Contains(link.AbsoluteUri) && !urlsToVisit.Any(u => u.AbsoluteUri == link.AbsoluteUri))
                            urlsToVisit.Enqueue(link);
                    }
                }
            }
            catch (Exception) { }
        }

        var sortedUrls = visitedUrls.OrderBy(url => url).ToList();
        Console.WriteLine("\nFinal Alphabetized List:");
        foreach (var url in sortedUrls)
            Console.WriteLine(url);

        int converted = 0;
        for (int i = 0; i < sortedUrls.Count && converted < limit; i++)
        {
            var url = sortedUrls[i];
            try
            {
                var headRequest = new HttpRequestMessage(HttpMethod.Head, url);
                var headResponse = await _httpClient.SendAsync(headRequest);
                if (headResponse.IsSuccessStatusCode)
                {
                    var markdown = await HtmlToMarkdownConverter.FromUrlAsync(url);
                    var slug = GetSlugFromUrl(url);
                    var filePath = Path.Combine(_outputDir, slug + ".md");
                    await File.WriteAllTextAsync(filePath, markdown);
                    Console.WriteLine($"Saved markdown for {url} to {filePath}");
                    converted++;
                }
                else
                {
                    Console.WriteLine($"\nSkipping {url}: HEAD request returned status {(int)headResponse.StatusCode} {headResponse.ReasonPhrase}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\nSkipping {url}: Exception during HEAD request or markdown conversion: {ex.Message}");
            }
        }
    }

    private static string GetSlugFromUrl(string url)
    {
        var uri = new Uri(url);
        var segments = uri.AbsolutePath.Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (segments.Length == 0)
            return "index";
        return string.Join('-', segments.Skip(1)); // skip 'dotnet' (first segment)
    }
}

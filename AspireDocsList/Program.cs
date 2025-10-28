using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using HtmlAgilityPack;

class Program
{
    static async Task Main(string[] args)
    {
        var rootUrl = "https://learn.microsoft.com/dotnet/aspire/";
        var baseUri = new Uri(rootUrl);
        var visitedUrls = new HashSet<string>(); // Use HashSet for uniqueness
        var urlsToVisit = new Queue<Uri>();
        urlsToVisit.Enqueue(baseUri);

        using var httpClient = new HttpClient();
        httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (compatible; CrawlerBot/1.0)");

        while (urlsToVisit.Count > 0)
        {
            var currentUri = urlsToVisit.Dequeue();
            if (visitedUrls.Contains(currentUri.AbsoluteUri))
            {
                // Skip URLs that have already been visited
                continue;
            }

            visitedUrls.Add(currentUri.AbsoluteUri);
            // Do not print here; only queue and crawl

            try
            {
                var response = await httpClient.GetStringAsync(currentUri);
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
                        {
                            urlsToVisit.Enqueue(link);
                        }
                    }
                }
            }
            catch (Exception)
            {
                // Ignore errors, just skip
            }
        }

        var sortedUrls = visitedUrls.OrderBy(url => url).ToList();
        Console.WriteLine("\nFinal Alphabetized List:");
        foreach (var url in sortedUrls)
        {
            Console.WriteLine(url);
        }
    }
}

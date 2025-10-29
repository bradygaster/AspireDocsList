using HtmlAgilityPack;
using Microsoft.Extensions.Logging;
using ReverseMarkdown;

public class HtmlToMarkdownConverter(ILogger<HtmlToMarkdownConverter> logger)
{
    private readonly HttpClient _httpClient = new();

    public async Task<string> FromUrlAsync(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
            throw new ArgumentException("URL cannot be null or empty.", nameof(url));

        logger.LogInformation($"Starting HTML to Markdown conversion for URL: {url}");
        try
        {
            // Fetch HTML from the URL
            var html = await _httpClient.GetStringAsync(url);
            logger.LogInformation($"Fetched HTML content from {url}, length: {html.Length}");

            // Parse HTML
            var doc = new HtmlDocument();
            doc.LoadHtml(html);
            HtmlNode contentNode = doc.DocumentNode.SelectSingleNode("//main")!;
            if (contentNode != null)
                logger.LogInformation("Selected <main> node for conversion.");
            if (contentNode == null)
            {
                contentNode = doc.DocumentNode.SelectSingleNode("//article")!;
                if (contentNode != null)
                    logger.LogInformation("Selected <article> node for conversion.");
            }
            if (contentNode == null)
            {
                contentNode = doc.DocumentNode.SelectSingleNode("//body")!;
                if (contentNode != null)
                    logger.LogInformation("Selected <body> node for conversion.");
            }
            if (contentNode == null)
                logger.LogWarning("No <main>, <article>, or <body> node found. Using full HTML.");

            // If none found, use full HTML
            string contentToConvert = contentNode != null ? contentNode.InnerHtml : html;

            // Remove <nav>, <footer>, <script>, <style> tags from the extracted content
            var tempDoc = new HtmlDocument();
            tempDoc.LoadHtml(contentToConvert);
            foreach (var tag in new[] { "nav", "footer", "script", "style" })
            {
                var nodes = tempDoc.DocumentNode.SelectNodes($"//{tag}");
                if (nodes != null)
                {
                    foreach (var node in nodes)
                    {
                        node.Remove();
                    }
                    logger.LogInformation($"Removed {nodes.Count} <{tag}> nodes from content.");
                }
            }
            contentToConvert = tempDoc.DocumentNode.InnerHtml;

            // Configure ReverseMarkdown converter
            var config = new ReverseMarkdown.Config
            {
                GithubFlavored = true,
                RemoveComments = true,
                UnknownTags = Config.UnknownTagsOption.PassThrough
            };

            var converter = new Converter(config);
            logger.LogInformation("Configured ReverseMarkdown converter.");

            // Convert extracted HTML to Markdown
            var markdown = converter.Convert(contentToConvert);
            logger.LogInformation($"Conversion to Markdown complete for URL: {url}, length: {markdown.Length}");
            return markdown;
        }
        catch (Exception ex)
        {
            logger.LogError($"Error converting {url}: {ex.Message}");
            return $"# Error\n\nFailed to convert content from {url}\n\nError: {ex.Message}";
        }
    }
}

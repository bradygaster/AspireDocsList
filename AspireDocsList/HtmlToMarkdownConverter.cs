using HtmlAgilityPack;
using ReverseMarkdown;

public static class HtmlToMarkdownConverter
{
    private static readonly HttpClient _httpClient = new();

    public static async Task<string> FromUrlAsync(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
            throw new ArgumentException("URL cannot be null or empty.", nameof(url));

        try
        {
            // Fetch HTML from the URL
            var html = await _httpClient.GetStringAsync(url);

            // Parse HTML
            var doc = new HtmlDocument();
            doc.LoadHtml(html);
            HtmlNode contentNode = null;

            // Try <main>, <article>, <body> in order
            contentNode = doc.DocumentNode.SelectSingleNode("//main");
            if (contentNode == null)
                contentNode = doc.DocumentNode.SelectSingleNode("//article");
            if (contentNode == null)
                contentNode = doc.DocumentNode.SelectSingleNode("//body");

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

            // Convert extracted HTML to Markdown
            return converter.Convert(contentToConvert);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error converting {url}: {ex.Message}");
            return $"# Error\n\nFailed to convert content from {url}\n\nError: {ex.Message}";
        }
    }
}

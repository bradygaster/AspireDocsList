using Microsoft.Extensions.Logging;

public class MarkdownInstructionCrawler(ILogger<MarkdownInstructionCrawler> logger,
    AgentFactory agentFactory)
{
    private readonly string _outputDir = Path.Combine(Directory.GetCurrentDirectory(), "instructions");

    public async Task CrawlMarkdownFilesAsync()
    {
        if (!Directory.Exists(_outputDir))
        {
            logger.LogWarning($"Directory not found: {_outputDir}");
            return;
        }

        var mdFiles = Directory.GetFiles(_outputDir, "*.md", SearchOption.TopDirectoryOnly);
        foreach (var file in mdFiles)
        {
            logger.LogInformation($"Processing file: {file}");
            var instruction = await agentFactory.FromInstructions(file);
        }
    }
}
using AspireDocsList.Agents;

public class MarkdownInstructionCrawler(InstructionLoader instructionLoader, SummarizationAgent summarizationAgent)
{
    private readonly string _outputDir = Path.Combine(Directory.GetCurrentDirectory(), "instructions");

    public async Task CrawlMarkdownFilesAsync()
    {
        if (!Directory.Exists(_outputDir))
        {
            Console.WriteLine($"Directory not found: {_outputDir}");
            return;
        }

        var mdFiles = Directory.GetFiles(_outputDir, "*.md", SearchOption.TopDirectoryOnly);
        foreach (var file in mdFiles)
        {
            var instruction = instructionLoader.LoadInstruction(file);
            var summary = await summarizationAgent.SummarizeAsync(instruction);

            Console.WriteLine("=======================================================");
            Console.WriteLine("Summary for file: " + Path.GetFileName(file));
            Console.WriteLine(summary);
            Console.WriteLine("=======================================================");
        }
    }
}
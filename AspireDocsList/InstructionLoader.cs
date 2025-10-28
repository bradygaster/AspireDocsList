public class InstructionLoader
{
    public string LoadInstruction(string fileName)
    {
        var basePath = AppContext.BaseDirectory;
        var promptsPath = Path.Combine(basePath, "prompts", "prompt_template.md");
        var instructionPath = Path.Combine(basePath, "instructions", fileName);

        // Validate required files exist
        if (!File.Exists(promptsPath))
            throw new FileNotFoundException("Prompt template file not found. Ensure prompt_template.md exists in the prompts directory.");
        
        if (!File.Exists(instructionPath))
            throw new FileNotFoundException($"Instruction file '{fileName}' not found in the instructions directory.");

        // Read the instruction file
        var instructionContent = File.ReadAllText(instructionPath);
        
        // Load and process prompt template
        var promptTemplate = File.ReadAllText(promptsPath).Replace("{{DOMAIN_NAME}}", ".NET Aspire");

        // Combine prompt template with instruction content
        var combinedContent = $"{promptTemplate}\n\n# Knowledge Base\n{instructionContent}";

        return combinedContent;
    }
}
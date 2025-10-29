using AspireDocsList.Agents;
using Azure.AI.OpenAI;
using Azure.Identity;
using Microsoft.Agents.AI;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenAI;

public class AgentFactory(ILogger<AgentFactory> logger,
    IOptions<AzureSettings> azureSettings, 
    SummarizationAgent summarizationAgent,
    NamingAgent namingAgent,
    AgentPool agentPool)
{
    // Create chat client once
    ChainedTokenCredential credential = new ChainedTokenCredential(
        new AzureCliCredential(),
        new EnvironmentCredential(),
        new ManagedIdentityCredential()
    );

    public async Task<AIAgent> CreateAgent(string instructions)
    {
        var domain = await summarizationAgent.SummarizeAsync(instructions);
        var name = await namingAgent.NameAsync(instructions);

        var agent = new AzureOpenAIClient(new Uri(azureSettings.Value.Endpoint), credential)
            .GetChatClient(azureSettings.Value.ModelName)
            .CreateAIAgent(
                name: name,
                instructions: instructions,
                description: domain
            );

        return agent;
    }

    public async Task<AIAgent> FromInstructions(string fileName)
    {
        var basePath = AppContext.BaseDirectory;
        var promptsPath = Path.Combine(basePath, "prompts", "prompt_template.md");
        var instructionPath = Path.Combine(basePath, "instructions", fileName);

        logger.LogInformation($"Resolved promptsPath: {promptsPath}");
        logger.LogInformation($"Resolved instructionPath: {instructionPath}");

        // Validate required files exist
        if (!File.Exists(promptsPath))
        {
            logger.LogError($"Prompt template file not found at {promptsPath}");
            throw new FileNotFoundException("Prompt template file not found. Ensure prompt_template.md exists in the prompts directory.");
        }

        if (!File.Exists(instructionPath))
        {
            logger.LogError($"Instruction file '{fileName}' not found at {instructionPath}");
            throw new FileNotFoundException($"Instruction file '{fileName}' not found in the instructions directory.");
        }

        logger.LogInformation($"Reading instruction file: {instructionPath}");
        var instructionContent = File.ReadAllText(instructionPath);

        logger.LogInformation($"Creating agent from instruction file: {instructionPath}");
        var agent = await this.CreateAgent(instructionContent);
        logger.LogInformation($"Created agent {agent.Name} ");

        logger.LogInformation($"Reading prompt template: {promptsPath}");
        var promptTemplate = File.ReadAllText(promptsPath)
                                 .Replace("{{DOMAIN_NAME}}", agent.Description)
                                 .Replace("{{AGENT_NAME}}", agent.Name);

        logger.LogInformation($"Processed instructions and created agent with domain {agent.Description}.");

        // Combine prompt template with instruction content
        var combinedContent = $"{promptTemplate}\n\n# Knowledge Base\n{instructionContent}";
        logger.LogInformation("Returning combined instruction content.");

        // Update agent with combined instructions that include the output format
        agent = new AzureOpenAIClient(new Uri(azureSettings.Value.Endpoint), credential)
            .GetChatClient(azureSettings.Value.ModelName)
            .CreateAIAgent(
                name: agent.Name!,
                instructions: combinedContent,
                description: agent.Description
            );

        // Add agent to pool
        logger.LogInformation($"Adding agent {agent.Name} to agent pool.");
        agentPool.AddAgent(agent.Name!, agent);
        logger.LogInformation($"Added agent {agent.Name} to agent pool.");

        return agent;
    }
}

using Azure.AI.OpenAI;
using Azure.Identity;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenAI;
using System.Collections.Concurrent;
using System.Text.Json;

namespace AspireDocsList.Agents;

public class NamingAgent : AIAgent
{
    ChatClientAgent _agent;
    // Dictionary to cache markdown <-> agent name
    private readonly ConcurrentDictionary<string, string> _markdownToName = new();
    private readonly ConcurrentDictionary<string, string> _nameToMarkdown = new();

    public NamingAgent(ILogger<SummarizationAgent> logger, IOptions<AzureSettings> azureSettings)
    {
        // Create chat client once
        var credential = new ChainedTokenCredential(
            new AzureCliCredential(),
            new EnvironmentCredential(),
            new ManagedIdentityCredential()
        );

        // Create a new AI Agent from an Azure Open AI Client
        _agent = new AzureOpenAIClient(new Uri(azureSettings.Value.Endpoint), credential)
            .GetChatClient(azureSettings.Value.ModelName)
            .CreateAIAgent(
                name: "Agent Namer",
                instructions: @"# Agent Instructions: Generate an agent name from Markdown

You are an expert documentation summarization agent.

Your task is to read the provided markdown content, which represents a technical article or documentation page.

**Instructions:**
1. Carefully read and understand the entire markdown content.
2. Identify the main topic, purpose, and scope of the article.
3. Write a single string summarizing the agent's purpose:
 - The string must be32 characters or less.
 - Only use lowercase letters, numbers, and the underscore (_) character.
 - The string should be clear, concise, and capture the core subject and use-case.
 - Do not include spaces, punctuation, or any other characters.
 - Output only the string, with no additional commentary or formatting.

**Example Outputs:**
- dapr_integration_dotnet_aspire
- distributed_tracing_cloud_apps
- aspire_docs_summarization"
            );
    }

    public async Task<string> NameAsync(string markdownContent, CancellationToken cancellationToken = default)
    {
        // Check cache first
        if (_markdownToName.TryGetValue(markdownContent, out var cachedName))
            return cachedName;

        // Start conversation
        var thread = _agent.GetNewThread();
        var sb = new System.Text.StringBuilder();
        await foreach (var update in _agent!.RunStreamingAsync(markdownContent, thread!))
        {
            sb.Append(update.Text);
        }
        var name = sb.ToString();
        // Cache both directions
        _markdownToName[markdownContent] = name;
        _nameToMarkdown[name] = markdownContent;
        return name;
    }

    // Reverse lookup: get markdown by agent name
    public string? GetMarkdownForName(string agentName)
        => _nameToMarkdown.TryGetValue(agentName, out var markdown) ? markdown : null;

    public override AgentThread DeserializeThread(JsonElement serializedThread, JsonSerializerOptions? jsonSerializerOptions = null) 
        => _agent.DeserializeThread(serializedThread, jsonSerializerOptions);

    public override AgentThread GetNewThread() 
        => _agent.GetNewThread();

    public override Task<AgentRunResponse> RunAsync(IEnumerable<ChatMessage> messages, AgentThread? thread = null, AgentRunOptions? options = null, CancellationToken cancellationToken = default) 
        => _agent.RunAsync(messages, thread, options, cancellationToken);

    public override IAsyncEnumerable<AgentRunResponseUpdate> RunStreamingAsync(IEnumerable<ChatMessage> messages, AgentThread? thread = null, AgentRunOptions? options = null, CancellationToken cancellationToken = default) 
        => _agent.RunStreamingAsync(messages, thread, options, cancellationToken);
}

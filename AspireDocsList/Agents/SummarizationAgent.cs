using Azure.AI.OpenAI;
using Azure.Identity;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Options;
using OpenAI;
using System.Text.Json;

namespace AspireDocsList.Agents;

public class SummarizationAgent : AIAgent
{
    ChatClientAgent _agent;

    public SummarizationAgent(IOptions<AzureSettings> azureSettings)
    {
        // Create chat client once
        var cred = new ChainedTokenCredential(
            new AzureCliCredential(),
            new EnvironmentCredential(),
            new ManagedIdentityCredential()
        );

        // Create a new AI Agent from an Azure Open AI Client
        _agent = new AzureOpenAIClient(new Uri(azureSettings.Value.Endpoint), cred)
            .GetChatClient(azureSettings.Value.ModelName)
            .CreateAIAgent(
                name: "Agent Domain Summarizer",
                instructions: @"# Agent Instructions: Generate a Domain Statement from Markdown

You are an expert documentation summarization agent.

Your task is to read the provided markdown content, which represents a technical article or documentation page.

**Instructions:**
1. Carefully read and understand the entire markdown content.
2. Identify the main topic, purpose, and scope of the article.
3. Write a single, clear, and concise “domain statement” that defines the specific domain of the article.
 - The domain statement should capture the core subject, intended audience, and use-case.
 - Avoid unnecessary details, examples, or code unless essential to the domain.
4. Output only the domain statement, with no additional commentary or formatting.

**Example Domain Statements:**
- “Domain: Dapr integration in .NET Aspire applications.”
- “Domain: Distributed tracing for cloud-native .NET apps using Aspire.”

Your domain statement will be used to define the scope and expertise of a specialized agent representing this article."
            );
    }

    public async Task<string> SummarizeAsync(string markdownContent, CancellationToken cancellationToken = default)
    {
        // Start conversation
        var thread = _agent.GetNewThread();

        var sb = new System.Text.StringBuilder();

        await foreach (var update in _agent!.RunStreamingAsync(markdownContent, thread!))
        {
            sb.Append(update.Text);
        }

        return sb.ToString();
    }

    public override AgentThread DeserializeThread(JsonElement serializedThread, JsonSerializerOptions? jsonSerializerOptions = null) 
        => _agent.DeserializeThread(serializedThread, jsonSerializerOptions);

    public override AgentThread GetNewThread() 
        => _agent.GetNewThread();

    public override Task<AgentRunResponse> RunAsync(IEnumerable<ChatMessage> messages, AgentThread? thread = null, AgentRunOptions? options = null, CancellationToken cancellationToken = default) 
        => _agent.RunAsync(messages, thread, options, cancellationToken);

    public override IAsyncEnumerable<AgentRunResponseUpdate> RunStreamingAsync(IEnumerable<ChatMessage> messages, AgentThread? thread = null, AgentRunOptions? options = null, CancellationToken cancellationToken = default) 
        => _agent.RunStreamingAsync(messages, thread, options, cancellationToken);
}

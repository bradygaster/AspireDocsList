using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.AI;
using System.Text.Json;

public class ConversationLoop(AgentPool agentPool)
{
    private AIAgent? _currentAgent;
    private AgentThread? _currentThread;
    private string? _currentAgentKey;

    public async Task Chat()
    {
        Console.WriteLine("Crawling completed. Ready for questions about Aspire.");

        while (true)
        {
            Console.Write("\n> ");
            var input = Console.ReadLine();

            switch (input?.Trim().ToLowerInvariant())
            {
                case null or "":
                    continue;
                case "exit":
                    return;
                case "agents":
                    ShowAvailableAgents();
                    break;
                default:
                    await UserPrompt(input);
                    break;
            }
        }
    }

    private void ShowAvailableAgents()
    {
        Console.WriteLine("\nAvailable Agents:", ConsoleColor.Cyan);

        foreach (var kvp in agentPool.GetAllAgents())
        {
            var agent = agentPool.GetAgent(kvp.Key);
            Console.WriteLine($"🤖 {agent?.Name}: {agent?.Description}", ConsoleColor.Green);
        }
    }

    public async Task<List<ChatMessage>> UserPrompt(string prompt)
    {
        var initialMessage = new ChatMessage(ChatRole.User, prompt);

        var allAgents = agentPool.GetAllAgents().Select(kvp => kvp.Value).ToList();
        var workflow = AgentWorkflowBuilder.BuildSequential(allAgents);

        string? lastExecutorId = null;

        // Start a fresh streaming run for this order so previous conversation state does not leak.
        await using StreamingRun run = await InProcessExecution.StreamAsync(workflow: workflow, initialMessage);
        await run.TrySendMessageAsync(new TurnToken(emitEvents: true));
        await foreach (WorkflowEvent evt in run.WatchStreamAsync())
        {
            if (evt is AgentRunUpdateEvent e)
            {
                if (e.ExecutorId != lastExecutorId)
                {
                    lastExecutorId = e.ExecutorId;
                    Console.WriteLine();
                }

                Console.Write($"{e.Update.Text}");

                // if there's a function/tool to call
                if (e.Update.Contents.OfType<FunctionCallContent>().FirstOrDefault() is FunctionCallContent call)
                {
                    Console.WriteLine();
                    Console.WriteLine($"  [Calling function '{call.Name}' with arguments: {JsonSerializer.Serialize(call.Arguments)}]");
                }
            }
            else if (evt is WorkflowOutputEvent output)
            {
                Console.WriteLine();

                // Return the list of chat messages produced by the workflow
                return output.As<List<ChatMessage>>() ?? new List<ChatMessage>();
            }
        }

        return new List<ChatMessage>();
    }
}
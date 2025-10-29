using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using AspireDocsList.Agents;
using Microsoft.Extensions.Logging;

var builder = Host.CreateApplicationBuilder(args);

builder.AddSettings();
builder.Services.AddHttpClient();
builder.Services.AddSingleton<HtmlToMarkdownConverter>();
builder.Services.AddSingleton<AgentPool>();
builder.Services.AddSingleton<AgentFactory>();
builder.Services.AddTransient<ConversationLoop>();
builder.Services.AddSingleton<NamingAgent>();
builder.Services.AddSingleton<SummarizationAgent>();
builder.Services.AddTransient<AspireDocsCrawler>();
builder.Services.AddTransient<MarkdownInstructionCrawler>();

var app = builder.Build();

await app.Services.GetRequiredService<AspireDocsCrawler>().RunAsync(limit: 5);
await app.Services.GetRequiredService<MarkdownInstructionCrawler>().CrawlMarkdownFilesAsync();

try
{
    var conversation = app.Services.GetRequiredService<ConversationLoop>();
    await conversation.Chat();
}
catch (Exception ex)
{
    app.Services.GetRequiredService<ILogger<Program>>().LogError(ex, $"\nError: {ex.Message}");
}


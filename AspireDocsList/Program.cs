using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using AspireDocsList.Agents;

var builder = Host.CreateApplicationBuilder(args);

builder.AddSettings();
builder.Services.AddHttpClient();
builder.Services.AddSingleton<InstructionLoader>();
builder.Services.AddTransient<SummarizationAgent>();
builder.Services.AddTransient<AspireDocsCrawler>();
builder.Services.AddTransient<MarkdownInstructionCrawler>();

var app = builder.Build();

await app.Services.GetRequiredService<AspireDocsCrawler>().RunAsync();
await app.Services.GetRequiredService<MarkdownInstructionCrawler>().CrawlMarkdownFilesAsync();

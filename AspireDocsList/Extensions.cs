using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.Extensions.Hosting;

internal static class Extensions
{
    public static IHostApplicationBuilder AddSettings(this IHostApplicationBuilder builder)
    {
        builder.Configuration.AddUserSecrets<Program>();
        builder.Services.Configure<AzureSettings>(builder.Configuration.GetSection("Azure"));
        return builder;
    }
}
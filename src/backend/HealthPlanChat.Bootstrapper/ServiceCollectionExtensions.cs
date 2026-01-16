using HealthPlanChat.Core.ExternalInterfaces;
using HealthPlanChat.Core.UseCases.Chat;
using HealthPlanChat.Infrastructure.AgentFramework;
using HealthPlanChat.Infrastructure.Prompting;
using HealthPlanChat.Infrastructure.Redis;
using HealthPlanChat.Infrastructure.Search;
using HealthPlanChat.Infrastructure.Storage;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace HealthPlanChat.Bootstrapper;

/// <summary>
/// Extension methods for registering Health Plan Chat services.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds all Health Plan Chat services to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The configuration.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddHealthPlanChatServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Bind infrastructure options
        services.Configure<RedisOptions>(configuration.GetSection(RedisOptions.SectionKey));
        services.Configure<SearchOptions>(configuration.GetSection(SearchOptions.SectionKey));
        services.Configure<FoundryOptions>(configuration.GetSection(FoundryOptions.SectionKey));
        services.Configure<StorageOptions>(configuration.GetSection(StorageOptions.SectionKey));

        // Register infrastructure services
        services.AddSingleton<IChatSessionStore, RedisChatSessionStore>();
        services.AddSingleton<IPlanMaterialSearch, AzureAiSearchPlanMaterialSearch>();
        services.AddSingleton<IChatAgent, AgentFrameworkChatAgent>();
        services.AddSingleton<PlanMaterialBlobPublisher>();
        services.AddSingleton<PromptBuilder>();

        // Register use case interactors
        var topK = configuration.GetValue("Retrieval:TopK", 5);
        services.AddScoped<IChatInputBoundary>(sp =>
            new ChatInteractor(
                sp.GetRequiredService<IChatSessionStore>(),
                sp.GetRequiredService<IPlanMaterialSearch>(),
                sp.GetRequiredService<IChatAgent>(),
                sp.GetRequiredService<ILogger<ChatInteractor>>(),
                topK));

        return services;
    }
}

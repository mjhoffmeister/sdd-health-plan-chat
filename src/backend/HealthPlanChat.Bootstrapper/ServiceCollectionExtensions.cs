using HealthPlanChat.Core.ExternalInterfaces;
using HealthPlanChat.Core.UseCases;
using HealthPlanChat.Core.UseCases.Chat;
using HealthPlanChat.Infrastructure.AgentFramework;
using HealthPlanChat.Infrastructure.Prompting;
using HealthPlanChat.Infrastructure.Redis;
using HealthPlanChat.Infrastructure.Search;
using HealthPlanChat.Infrastructure.Storage;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace HealthPlanChat.Bootstrapper;

/// <summary>
/// Extension methods for registering Health Plan Chat services.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds core Health Plan Chat services to the service collection.
    /// Does not register presentation-layer types (boundaries with IResult).
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The configuration.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddHealthPlanChatCoreServices(
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
        services.AddSingleton<IChatAgent, AgentFrameworkChatAgent>();
        services.AddSingleton<PlanMaterialBlobPublisher>();
        services.AddSingleton<PromptBuilder>();

        // Note: IPlanMaterialSearch is no longer needed by ChatInteractor.
        // The agent handles retrieval internally via AzureAISearchAgentTool.
        // Keeping AzureAiSearchPlanMaterialSearch available for index maintenance utilities.
        services.AddSingleton<IPlanMaterialSearch, AzureAiSearchPlanMaterialSearch>();

        return services;
    }

    /// <summary>
    /// Registers a use case interactor with its boundary.
    /// </summary>
    /// <typeparam name="TRequest">The request type.</typeparam>
    /// <typeparam name="TOutput">The output type.</typeparam>
    /// <typeparam name="TInteractor">The interactor type.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddUseCase<TRequest, TOutput, TInteractor>(
        this IServiceCollection services)
        where TInteractor : class, IUseCaseInteractor<TRequest, TOutput>
    {
        services.AddScoped<IUseCaseInteractor<TRequest, TOutput>, TInteractor>();
        return services;
    }

    /// <summary>
    /// Registers a boundary implementation.
    /// </summary>
    /// <typeparam name="TBoundary">The boundary interface type.</typeparam>
    /// <typeparam name="TOutput">The output type.</typeparam>
    /// <typeparam name="TImplementation">The implementation type.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddBoundary<TBoundary, TOutput, TImplementation>(
        this IServiceCollection services)
        where TBoundary : class
        where TImplementation : class, TBoundary
    {
        services.AddScoped<TBoundary, TImplementation>();
        return services;
    }
}

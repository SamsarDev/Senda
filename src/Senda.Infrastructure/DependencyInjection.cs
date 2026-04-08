using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Senda.Core.Interfaces;
using Senda.Infrastructure.Persistence;
using Senda.Infrastructure.Services;

namespace Senda.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

        services.AddDbContext<SendaDbContext>(options =>
        {
            options.UseNpgsql(
                connectionString,
                npgsqlOptions =>
                {
                    npgsqlOptions.EnableRetryOnFailure(3);
                    npgsqlOptions.CommandTimeout(30);
                    npgsqlOptions.UseVector();
                });
        });

        // Current Tenant Context
        services.AddScoped<TenantContext>();
        services.AddScoped<ITenantContext>(sp => sp.GetRequiredService<TenantContext>());

        // AI Services
        services.AddScoped<ITextEmbeddingService, OllamaEmbeddingService>();
        services.AddScoped<IChatCompletionService, OllamaChatCompletionService>();

        // Storage Services
        services.AddScoped<IFileStorageService, FileStorageService>();

        // Orchestration Services
        services.AddScoped<ITextExtractorService, TextExtractorService>();
        services.AddScoped<ITextChunkerService, TextChunkerService>();

        // Repositories
        services.AddScoped<ITenantRepository, TenantRepository>();
        services.AddScoped<IKnowledgeRepository, KnowledgeRepository>();
        services.AddScoped<IChatRepository, ChatRepository>();
        services.AddScoped<IVectorSearchRepository, VectorSearchRepository>();

        return services;
    }

    public static async Task InitializeDatabaseAsync(this IServiceProvider services)
    {
        await using var scope = services.CreateAsyncScope();
        var dataContext = scope.ServiceProvider.GetRequiredService<SendaDbContext>();
        
        await dataContext.Database.MigrateAsync();
    }
}
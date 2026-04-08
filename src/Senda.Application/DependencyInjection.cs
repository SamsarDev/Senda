using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Senda.Application.Commands;
using Senda.Application.Services;
using Senda.Application.Validators;

namespace Senda.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        // Register MediatR
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(typeof(IngestDocumentCommand).Assembly);
        });

        // Register FluentValidation validators
        services.AddValidatorsFromAssemblyContaining<IngestDocumentCommandValidator>();

        // Register application services
        services.AddScoped<ITextExtractorService, TextExtractorService>();
        services.AddScoped<ITextChunkerService, TextChunkerService>();

        return services;
    }
}

/// <summary>
/// Placeholder for ITextExtractorService implementation in Application layer.
/// This should be implemented in Infrastructure or can be a simple orchestration.
/// </summary>
internal class TextExtractorService : ITextExtractorService
{
    public Task<string> ExtractTextAsync(Stream fileStream, string contentType)
    {
        throw new NotImplementedException("Text extraction logic should be implemented in Infrastructure layer.");
    }
}

/// <summary>
/// Placeholder for ITextChunkerService implementation in Application layer.
/// </summary>
internal class TextChunkerService : ITextChunkerService
{
    public IEnumerable<string> ChunkText(string fullText, int maxTokens, int overlap)
    {
        throw new NotImplementedException("Text chunking logic should be implemented in Infrastructure layer.");
    }
}

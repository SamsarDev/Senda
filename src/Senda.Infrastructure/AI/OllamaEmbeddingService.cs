using Microsoft.Extensions.Configuration;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Embeddings;
using Senda.Core.Services;

namespace Senda.Infrastructure.AI;

public class OllamaEmbeddingService : ITextEmbeddingService
{
    private readonly ITextEmbeddingGenerationService _embeddingService;

    public OllamaEmbeddingService(IConfiguration configuration)
    {
        var endpoint = configuration["AI:Endpoint"] ?? "http://localhost:11434";
        var modelId = configuration["AI:EmbeddingModel"] ?? "all-minilm";

        // Using OpenAI connector for Ollama (OpenAI compatible API)
        var builder = Kernel.CreateBuilder();
        
        #pragma warning disable SKEXP0070
        builder.AddOpenAITextEmbeddingGeneration(
            modelId: modelId,
            apiKey: "ollama", // Not used but required by connector
            endpoint: new Uri($"{endpoint.TrimEnd('/')}/v1")
        );
        #pragma warning restore SKEXP0070

        var kernel = builder.Build();
        _embeddingService = kernel.GetRequiredService<ITextEmbeddingGenerationService>();
    }

    public async Task<float[]> GenerateEmbeddingAsync(string text)
    {
        var embedding = await _embeddingService.GenerateEmbeddingAsync(text);
        return embedding.ToArray();
    }
}

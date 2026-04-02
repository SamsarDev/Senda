namespace Senda.Core.Services;

public interface ITextEmbeddingService
{
    Task<ReadOnlyMemory<float>> GenerateEmbeddingAsync(string text);
}

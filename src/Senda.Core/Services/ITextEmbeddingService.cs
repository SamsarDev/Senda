namespace Senda.Core.Services;

public interface ITextEmbeddingService
{
    Task<float[]> GenerateEmbeddingAsync(string text);
}

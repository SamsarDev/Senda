using Senda.Application.Services;

namespace Senda.Infrastructure.Services;

public class TextChunkerService : ITextChunkerService
{
    public IEnumerable<string> ChunkText(string fullText, int maxTokens, int overlap)
    {
        if (string.IsNullOrWhiteSpace(fullText))
            return Enumerable.Empty<string>();

        // Simple word-based chunking as an approximation for token-based chunking
        // average 1 token ~= 0.75 words, but we'll use words for simplicity in MVP
        var words = fullText.Split(new[] { ' ', '\r', '\n', '\t' }, StringSplitOptions.RemoveEmptyEntries);
        var chunks = new List<string>();
        
        for (int i = 0; i < words.Length; i += (maxTokens - overlap))
        {
            var chunkWords = words.Skip(i).Take(maxTokens).ToArray();
            if (chunkWords.Length == 0) break;
            
            chunks.Add(string.Join(" ", chunkWords));
            
            if (i + maxTokens >= words.Length) break;
        }

        return chunks;
    }
}

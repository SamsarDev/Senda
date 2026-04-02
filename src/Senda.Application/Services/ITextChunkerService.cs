namespace Senda.Application.Services;

/// <summary>
/// Responsible for dividing long texts into manageable fragments for the LLM, maintaining context (overlapping).
/// </summary>
public interface ITextChunkerService
{
    /// <summary>
    /// Splits the full text into chunks.
    /// </summary>
    /// <param name="fullText">The complete text to split into chunks.</param>
    /// <param name="maxTokens">Maximum number of tokens per chunk.</param>
    /// <param name="overlap">Number of overlapping tokens between chunks to maintain context.</param>
    /// <returns>Collection of text chunks.</returns>
    IEnumerable<string> ChunkText(string fullText, int maxTokens, int overlap);
}

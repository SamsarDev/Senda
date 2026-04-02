namespace Senda.Application.Services;

/// <summary>
/// Isolates the logic of reading physical files (PDF, Word, TXT).
/// </summary>
public interface ITextExtractorService
{
    /// <summary>
    /// Extracts text content from a file stream.
    /// </summary>
    /// <param name="fileStream">The file stream to extract text from.</param>
    /// <param name="contentType">The MIME type of the file (e.g., "application/pdf", "text/plain").</param>
    /// <returns>The extracted text content.</returns>
    Task<string> ExtractTextAsync(Stream fileStream, string contentType);
}

namespace Senda.Core.Exceptions;

public class DocumentProcessingException : Exception
{
    public Guid DocumentId { get; }

    public DocumentProcessingException(string message)
        : base(message)
    {
    }

    public DocumentProcessingException(Guid documentId, string message)
        : base($"Error processing document '{documentId}': {message}")
    {
        DocumentId = documentId;
    }

    public DocumentProcessingException(Guid documentId, string message, Exception innerException)
        : base($"Error processing document '{documentId}': {message}", innerException)
    {
        DocumentId = documentId;
    }
}

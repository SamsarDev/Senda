using MediatR;
using Senda.Application.DTOs;

namespace Senda.Application.Commands;

/// <summary>
/// Command to ingest a document into the knowledge base (RAG Pipeline - Input).
/// </summary>
public class IngestDocumentCommand : IRequest<KnowledgeDocumentDto>
{
    public Guid TenantId { get; set; }
    public Stream FileStream { get; set; } = Stream.Null!;
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
}

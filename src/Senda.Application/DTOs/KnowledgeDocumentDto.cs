using Senda.Core.Enums;

namespace Senda.Application.DTOs;

public class KnowledgeDocumentDto
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public SourceType SourceType { get; set; }
    public DocumentStatus Status { get; set; }
    public DateTimeOffset UploadedAt { get; set; }
}

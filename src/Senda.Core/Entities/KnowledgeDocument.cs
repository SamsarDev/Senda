using Senda.Core.Enums;

namespace Senda.Core.Entities;

public class KnowledgeDocument
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public SourceType SourceType { get; set; }
    public DocumentStatus Status { get; set; } = DocumentStatus.Pending;
    public DateTimeOffset UploadedAt { get; set; } = DateTimeOffset.UtcNow;
}

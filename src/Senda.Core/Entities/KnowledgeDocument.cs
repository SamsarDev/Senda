using Senda.Core.Enums;
using Senda.Core.Interfaces;

namespace Senda.Core.Entities;

public class KnowledgeDocument : ITenantEntity, IAuditableEntity
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public SourceType SourceType { get; set; }
    public DocumentStatus Status { get; set; } = DocumentStatus.Pending;
    public DateTimeOffset UploadedAt { get; set; } = DateTimeOffset.UtcNow;

    // IAuditableEntity
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public string? CreatedBy { get; set; }
    public DateTimeOffset? ModifiedAt { get; set; }
    public string? ModifiedBy { get; set; }
}

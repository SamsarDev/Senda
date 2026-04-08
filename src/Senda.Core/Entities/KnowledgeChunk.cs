using Senda.Core.Interfaces;

namespace Senda.Core.Entities;

public class KnowledgeChunk : ITenantEntity, IAuditableEntity
{
    public Guid Id { get; set; }
    public Guid DocumentId { get; set; }
    public Guid TenantId { get; set; }
    public string Content { get; set; } = string.Empty;
    public ReadOnlyMemory<float>? Embedding { get; set; }
    public int TokenCount { get; set; }

    // IAuditableEntity
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public string? CreatedBy { get; set; }
    public DateTimeOffset? ModifiedAt { get; set; }
    public string? ModifiedBy { get; set; }
}

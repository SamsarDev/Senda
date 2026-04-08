using Senda.Core.Enums;
using Senda.Core.Interfaces;

namespace Senda.Core.Entities;

public class ChatMessage : ITenantEntity, IAuditableEntity
{
    public Guid Id { get; set; }
    public Guid SessionId { get; set; }
    public Guid TenantId { get; set; }
    public MessageRole Role { get; set; }
    public string Content { get; set; } = string.Empty;

    // IAuditableEntity
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public string? CreatedBy { get; set; }
    public DateTimeOffset? ModifiedAt { get; set; }
    public string? ModifiedBy { get; set; }
}

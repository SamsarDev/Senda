using Senda.Core.Interfaces;

namespace Senda.Core.Entities;

public class Tenant : IAuditableEntity
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? SystemPrompt { get; set; }
    public bool IsActive { get; set; } = true;

    // IAuditableEntity
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public string? CreatedBy { get; set; }
    public DateTimeOffset? ModifiedAt { get; set; }
    public string? ModifiedBy { get; set; }
}

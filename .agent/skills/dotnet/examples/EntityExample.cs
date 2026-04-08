using Senda.Core.Interfaces;

namespace Senda.Core.Entities;

/// <summary>
/// Example of a tenant-isolated entity following the skill guidelines.
/// </summary>
public class BusinessResource : ITenantEntity, IAuditableEntity
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    
    // IAuditableEntity properties
    public DateTimeOffset CreatedAt { get; set; }
    public string? CreatedBy { get; set; }
    public DateTimeOffset? ModifiedAt { get; set; }
    public string? ModifiedBy { get; set; }
}

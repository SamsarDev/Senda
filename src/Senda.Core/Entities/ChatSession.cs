namespace Senda.Core.Entities;

public class ChatSession
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public string CustomerIdentifier { get; set; } = string.Empty;
    public DateTimeOffset StartedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset LastActivityAt { get; set; } = DateTimeOffset.UtcNow;
}

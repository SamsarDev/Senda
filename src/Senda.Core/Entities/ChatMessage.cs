using Senda.Core.Enums;

namespace Senda.Core.Entities;

public class ChatMessage
{
    public Guid Id { get; set; }
    public Guid SessionId { get; set; }
    public MessageRole Role { get; set; }
    public string Content { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}

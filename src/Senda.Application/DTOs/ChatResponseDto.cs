namespace Senda.Application.DTOs;

public class ChatResponseDto
{
    public string Message { get; set; } = string.Empty;
    public Guid SessionId { get; set; }
    public List<string> References { get; set; } = new();
}

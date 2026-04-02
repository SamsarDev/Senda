using MediatR;
using Senda.Application.DTOs;

namespace Senda.Application.Commands;

/// <summary>
/// Command to send a message in a chat conversation (RAG Pipeline - Output).
/// </summary>
public class SendMessageCommand : IRequest<ChatResponseDto>
{
    public Guid TenantId { get; set; }
    public Guid? SessionId { get; set; }
    public string CustomerIdentifier { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}

using Senda.Core.Entities;

namespace Senda.Core.Repositories;

public interface IChatRepository
{
    Task<ChatSession?> GetSessionWithMessagesAsync(Guid sessionId);
    Task AddMessageAsync(ChatMessage message);
}

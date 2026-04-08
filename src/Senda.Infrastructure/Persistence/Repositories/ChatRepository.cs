using Microsoft.EntityFrameworkCore;
using Senda.Core.Entities;
using Senda.Core.Repositories;
using Senda.Infrastructure.Persistence;

namespace Senda.Infrastructure.Persistence.Repositories;

public class ChatRepository : IChatRepository
{
    private readonly SendaDbContext _context;

    public ChatRepository(SendaDbContext context)
    {
        _context = context;
    }

    public async Task<ChatSession?> GetSessionWithMessagesAsync(Guid sessionId)
    {
        return await _context.ChatSessions
            .Include(s => s.Messages.OrderBy(m => m.CreatedAt))
            .FirstOrDefaultAsync(s => s.Id == sessionId);
    }

    public async Task AddSessionAsync(ChatSession session)
    {
        await _context.ChatSessions.AddAsync(session);
        await _context.SaveChangesAsync();
    }

    public async Task AddMessageAsync(ChatMessage message)
    {
        await _context.ChatMessages.AddAsync(message);
        await _context.SaveChangesAsync();
    }

    public async Task<IEnumerable<ChatSession>> GetSessionsByTenantAsync(Guid tenantId, string customerIdentifier)
    {
        return await _context.ChatSessions
            .Where(s => s.TenantId == tenantId && s.CustomerIdentifier == customerIdentifier)
            .OrderByDescending(s => s.LastActivityAt)
            .ToListAsync();
    }
}

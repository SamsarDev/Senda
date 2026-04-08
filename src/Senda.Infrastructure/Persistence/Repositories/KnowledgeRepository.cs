using Microsoft.EntityFrameworkCore;
using Senda.Core.Entities;
using Senda.Core.Repositories;
using Senda.Infrastructure.Persistence;

namespace Senda.Infrastructure.Persistence.Repositories;

public class KnowledgeRepository : IKnowledgeRepository
{
    private readonly SendaDbContext _context;

    public KnowledgeRepository(SendaDbContext context)
    {
        _context = context;
    }

    public async Task<KnowledgeDocument?> GetDocumentByIdAsync(Guid id)
    {
        return await _context.KnowledgeDocuments.FindAsync(id);
    }

    public async Task<IEnumerable<KnowledgeDocument>> GetDocumentsByTenantAsync(Guid tenantId)
    {
        return await _context.KnowledgeDocuments
            .Where(d => d.TenantId == tenantId)
            .OrderByDescending(d => d.UploadedAt)
            .ToListAsync();
    }

    public async Task AddDocumentAsync(KnowledgeDocument document)
    {
        var existing = await _context.KnowledgeDocuments.FindAsync(document.Id);
        if (existing != null)
        {
            _context.Entry(existing).CurrentValues.SetValues(document);
        }
        else
        {
            await _context.KnowledgeDocuments.AddAsync(document);
        }
        await _context.SaveChangesAsync();
    }

    public async Task AddChunksAsync(IEnumerable<KnowledgeChunk> chunks)
    {
        await _context.KnowledgeChunks.AddRangeAsync(chunks);
        await _context.SaveChangesAsync();
    }

    public async Task<IEnumerable<KnowledgeChunk>> GetChunksByDocumentIdAsync(Guid documentId)
    {
        return await _context.KnowledgeChunks
            .Where(c => c.DocumentId == documentId)
            .ToListAsync();
    }

    public async Task DeleteDocumentAsync(Guid id)
    {
        var doc = await _context.KnowledgeDocuments.FindAsync(id);
        if (doc != null)
        {
            // Chunks should be deleted via cascade or manual if not configured
            var chunks = _context.KnowledgeChunks.Where(c => c.DocumentId == id);
            _context.KnowledgeChunks.RemoveRange(chunks);
            _context.KnowledgeDocuments.Remove(doc);
            await _context.SaveChangesAsync();
        }
    }
}

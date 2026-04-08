using Microsoft.EntityFrameworkCore;
using Pgvector;
using Pgvector.EntityFrameworkCore;
using Senda.Core.Entities;
using Senda.Core.Repositories;
using Senda.Infrastructure.Persistence;

namespace Senda.Infrastructure.Persistence.Repositories;

public class VectorSearchRepository : IVectorSearchRepository
{
    private readonly SendaDbContext _context;

    public VectorSearchRepository(SendaDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<KnowledgeChunk>> SearchSimilarChunksAsync(
        Guid tenantId, 
        float[] queryEmbedding, 
        int limit = 5)
    {
        var vector = new Vector(queryEmbedding);

        // L2 distance search (can also use Coseno distance with <-> or <=> operator)
        // Pgvector in EF Core uses specialized operators
        return await _context.KnowledgeChunks
            .Where(c => c.TenantId == tenantId)
            .OrderBy(c => c.Embedding!.L2Distance(vector))
            .Take(limit)
            .ToListAsync();
    }
}

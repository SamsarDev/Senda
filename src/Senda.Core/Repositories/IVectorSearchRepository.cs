using Senda.Core.Entities;

namespace Senda.Core.Repositories;

public interface IVectorSearchRepository
{
    Task<IEnumerable<KnowledgeChunk>> SearchSimilarChunksAsync(
        Guid tenantId, 
        ReadOnlyMemory<float> queryEmbedding, 
        int maxResults);
}

using Senda.Core.Entities;

namespace Senda.Core.Repositories;

public interface IKnowledgeRepository
{
    Task<KnowledgeDocument?> GetDocumentByIdAsync(Guid id);
    Task<IEnumerable<KnowledgeDocument>> GetDocumentsByTenantAsync(Guid tenantId);
    Task AddDocumentAsync(KnowledgeDocument doc);
    Task AddChunksAsync(IEnumerable<KnowledgeChunk> chunks);
    Task<IEnumerable<KnowledgeChunk>> GetChunksByDocumentIdAsync(Guid documentId);
    Task DeleteDocumentAsync(Guid id);
}

using Senda.Core.Entities;

namespace Senda.Core.Repositories;

public interface IKnowledgeRepository
{
    Task AddDocumentAsync(KnowledgeDocument doc);
    Task AddChunksAsync(IEnumerable<KnowledgeChunk> chunks);
    Task<IEnumerable<KnowledgeChunk>> GetChunksByDocumentIdAsync(Guid documentId);
}

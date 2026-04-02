namespace Senda.Core.Entities;

public class KnowledgeChunk
{
    public Guid Id { get; set; }
    public Guid DocumentId { get; set; }
    public Guid TenantId { get; set; }
    public string Content { get; set; } = string.Empty;
    public ReadOnlyMemory<float>? Embedding { get; set; }
    public int TokenCount { get; set; }
}

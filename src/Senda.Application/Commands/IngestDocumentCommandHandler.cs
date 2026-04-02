using MediatR;
using Senda.Application.DTOs;
using Senda.Application.Services;
using Senda.Core.Entities;
using Senda.Core.Enums;
using Senda.Core.Exceptions;
using Senda.Core.Repositories;
using Senda.Core.Services;

namespace Senda.Application.Commands;

/// <summary>
/// Handler for IngestDocumentCommand - orchestrates the RAG pipeline for document ingestion.
/// </summary>
public class IngestDocumentCommandHandler : IRequestHandler<IngestDocumentCommand, KnowledgeDocumentDto>
{
    private readonly ITenantRepository _tenantRepository;
    private readonly IKnowledgeRepository _knowledgeRepository;
    private readonly ITextExtractorService _textExtractorService;
    private readonly ITextChunkerService _textChunkerService;
    private readonly ITextEmbeddingService _textEmbeddingService;

    public IngestDocumentCommandHandler(
        ITenantRepository tenantRepository,
        IKnowledgeRepository knowledgeRepository,
        ITextExtractorService textExtractorService,
        ITextChunkerService textChunkerService,
        ITextEmbeddingService textEmbeddingService)
    {
        _tenantRepository = tenantRepository;
        _knowledgeRepository = knowledgeRepository;
        _textExtractorService = textExtractorService;
        _textChunkerService = textChunkerService;
        _textEmbeddingService = textEmbeddingService;
    }

    public async Task<KnowledgeDocumentDto> Handle(IngestDocumentCommand request, CancellationToken cancellationToken)
    {
        // 1. Validate that the Tenant exists
        var tenant = await _tenantRepository.GetActiveTenantByIdAsync(request.TenantId);
        if (tenant == null)
        {
            throw new TenantNotFoundException(request.TenantId);
        }

        // 2. Determine SourceType from ContentType
        var sourceType = GetSourceType(request.ContentType);

        // 3. Create KnowledgeDocument record with Processing status
        var document = new KnowledgeDocument
        {
            Id = Guid.NewGuid(),
            TenantId = request.TenantId,
            FileName = request.FileName,
            SourceType = sourceType,
            Status = DocumentStatus.Processing,
            UploadedAt = DateTimeOffset.UtcNow
        };

        await _knowledgeRepository.AddDocumentAsync(document);

        // 4. Extract text using ITextExtractorService
        var text = await _textExtractorService.ExtractTextAsync(request.FileStream, request.ContentType);

        // 5. Split text into chunks using ITextChunkerService
        // Default: 512 tokens max, 50 overlap
        var chunks = _textChunkerService.ChunkText(text, maxTokens: 512, overlap: 50).ToList();

        // 6. For each chunk, generate embedding using ITextEmbeddingService
        var knowledgeChunks = new List<KnowledgeChunk>();
        foreach (var chunkContent in chunks)
        {
            var embedding = await _textEmbeddingService.GenerateEmbeddingAsync(chunkContent);

            var knowledgeChunk = new KnowledgeChunk
            {
                Id = Guid.NewGuid(),
                DocumentId = document.Id,
                TenantId = request.TenantId,
                Content = chunkContent,
                Embedding = embedding,
                TokenCount = chunkContent.Split(' ').Length // Approximate token count
            };

            knowledgeChunks.Add(knowledgeChunk);
        }

        // 7. Save KnowledgeChunks via IKnowledgeRepository
        await _knowledgeRepository.AddChunksAsync(knowledgeChunks);

        // 8. Update document status to Completed
        document.Status = DocumentStatus.Completed;
        await _knowledgeRepository.AddDocumentAsync(document); // This should be an update operation

        // Map to DTO and return
        return new KnowledgeDocumentDto
        {
            Id = document.Id,
            TenantId = document.TenantId,
            FileName = document.FileName,
            SourceType = document.SourceType,
            Status = document.Status,
            UploadedAt = document.UploadedAt
        };
    }

    private static SourceType GetSourceType(string contentType)
    {
        return contentType.ToLowerInvariant() switch
        {
            "application/pdf" => SourceType.Pdf,
            "text/plain" => SourceType.Text,
            _ => throw new DocumentProcessingException($"Unsupported content type: {contentType}")
        };
    }
}

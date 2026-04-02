using MediatR;
using Senda.Application.DTOs;
using Senda.Core.Entities;
using Senda.Core.Enums;
using Senda.Core.Exceptions;
using Senda.Core.Repositories;
using Senda.Core.Services;

namespace Senda.Application.Commands;

/// <summary>
/// Handler for SendMessageCommand - orchestrates the RAG pipeline for chat conversation.
/// </summary>
public class SendMessageCommandHandler : IRequestHandler<SendMessageCommand, ChatResponseDto>
{
    private readonly ITenantRepository _tenantRepository;
    private readonly IChatRepository _chatRepository;
    private readonly IVectorSearchRepository _vectorSearchRepository;
    private readonly ITextEmbeddingService _textEmbeddingService;
    private readonly IChatCompletionService _chatCompletionService;

    public SendMessageCommandHandler(
        ITenantRepository tenantRepository,
        IChatRepository chatRepository,
        IVectorSearchRepository vectorSearchRepository,
        ITextEmbeddingService textEmbeddingService,
        IChatCompletionService chatCompletionService)
    {
        _tenantRepository = tenantRepository;
        _chatRepository = chatRepository;
        _vectorSearchRepository = vectorSearchRepository;
        _textEmbeddingService = textEmbeddingService;
        _chatCompletionService = chatCompletionService;
    }

    public async Task<ChatResponseDto> Handle(SendMessageCommand request, CancellationToken cancellationToken)
    {
        // 1. Validate that the Tenant exists
        var tenant = await _tenantRepository.GetActiveTenantByIdAsync(request.TenantId);
        if (tenant == null)
        {
            throw new TenantNotFoundException(request.TenantId);
        }

        // 2. Recover or create the chat session
        ChatSession? session = null;
        if (request.SessionId.HasValue)
        {
            session = await _chatRepository.GetSessionWithMessagesAsync(request.SessionId.Value);
        }

        // Create new session if it doesn't exist
        if (session == null)
        {
            session = new ChatSession
            {
                Id = Guid.NewGuid(),
                TenantId = request.TenantId,
                CustomerIdentifier = request.CustomerIdentifier,
                StartedAt = DateTimeOffset.UtcNow,
                LastActivityAt = DateTimeOffset.UtcNow
            };
        }
        else
        {
            session.LastActivityAt = DateTimeOffset.UtcNow;
        }

        // 3. Save the user's message (ChatMessage with Role User)
        var userMessage = new ChatMessage
        {
            Id = Guid.NewGuid(),
            SessionId = session.Id,
            Role = MessageRole.User,
            Content = request.Message,
            CreatedAt = DateTimeOffset.UtcNow
        };

        await _chatRepository.AddMessageAsync(userMessage);

        // 4. Convert the user's message into a vector using ITextEmbeddingService
        var queryEmbedding = await _textEmbeddingService.GenerateEmbeddingAsync(request.Message);

        // 5. Search for relevant context in the database
        var relevantChunks = await _vectorSearchRepository.SearchSimilarChunksAsync(
            request.TenantId,
            queryEmbedding,
            maxResults: 5);

        var chunksList = relevantChunks.ToList();
        var groundedKnowledge = chunksList.Select(c => c.Content).ToList();

        // 6. Recover the recent history of the session (last 5 messages)
        var recentMessages = session.Messages?.OrderByDescending(m => m.CreatedAt).Take(5).Reverse() 
                            ?? Enumerable.Empty<ChatMessage>();

        // 7. Call IChatCompletionService.GetReplyAsync
        var reply = await _chatCompletionService.GetReplyAsync(
            request.TenantId,
            recentMessages,
            groundedKnowledge);

        // 8. Save the generated response (ChatMessage with Role Assistant)
        var assistantMessage = new ChatMessage
        {
            Id = Guid.NewGuid(),
            SessionId = session.Id,
            Role = MessageRole.Assistant,
            Content = reply,
            CreatedAt = DateTimeOffset.UtcNow
        };

        await _chatRepository.AddMessageAsync(assistantMessage);

        // 9. Return ChatResponseDto
        var references = chunksList
            .Where(c => !string.IsNullOrEmpty(c.Content))
            .Select(c => c.Content.Length > 100 ? c.Content.Substring(0, 100) + "..." : c.Content)
            .ToList();

        return new ChatResponseDto
        {
            Message = reply,
            SessionId = session.Id,
            References = references
        };
    }
}

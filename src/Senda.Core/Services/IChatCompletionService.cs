using Senda.Core.Entities;

namespace Senda.Core.Services;

public interface IChatCompletionService
{
    Task<string> GetReplyAsync(
        Guid tenantId, 
        IEnumerable<ChatMessage> context, 
        IEnumerable<string> groundedKnowledge);
}

using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Senda.Core.Entities;
using Senda.Core.Services;

namespace Senda.Infrastructure.AI;

public class OllamaChatCompletionService : Senda.Core.Services.IChatCompletionService
{
    private readonly Microsoft.SemanticKernel.ChatCompletion.IChatCompletionService _chatService;
    private readonly Kernel _kernel;

    public OllamaChatCompletionService(IConfiguration configuration)
    {
        var endpoint = configuration["AI:Endpoint"] ?? "http://localhost:11434";
        var modelId = configuration["AI:ChatModel"] ?? "qwen3.5";

        var builder = Kernel.CreateBuilder();
        
        using var httpClient = new System.Net.Http.HttpClient { BaseAddress = new Uri(endpoint) };
        
        builder.AddOpenAIChatCompletion(
            modelId: modelId,
            apiKey: "ollama",
            httpClient: httpClient
        );

        _kernel = builder.Build();
        _chatService = _kernel.GetRequiredService<Microsoft.SemanticKernel.ChatCompletion.IChatCompletionService>();
    }

    public async Task<string> GetReplyAsync(
        Guid tenantId, 
        IEnumerable<ChatMessage> context, 
        IEnumerable<string> groundedKnowledge)
    {
        var chatHistory = new ChatHistory();
        
        // Add grounded knowledge as a system message or part of the first message
        var systemPrompt = new StringBuilder();
        systemPrompt.AppendLine("Eres un asistente de conserjería inteligente (AI Concierge) para una empresa.");
        systemPrompt.AppendLine("Responde basándote ÚNICAMENTE en el siguiente contexto extraído de la documentación oficial de la empresa.");
        systemPrompt.AppendLine("Si la respuesta no está en el contexto, indica amablemente que no tienes esa información.");
        systemPrompt.AppendLine("\n--- CONTEXTO ---");
        
        foreach (var knowledge in groundedKnowledge)
        {
            systemPrompt.AppendLine(knowledge);
        }
        
        systemPrompt.AppendLine("--- FIN DEL CONTEXTO ---\n");
        
        chatHistory.AddSystemMessage(systemPrompt.ToString());

        // Add conversation history
        foreach (var msg in context)
        {
            switch (msg.Role)
            {
                case Senda.Core.Enums.MessageRole.User:
                    chatHistory.AddUserMessage(msg.Content);
                    break;
                case Senda.Core.Enums.MessageRole.Assistant:
                    chatHistory.AddAssistantMessage(msg.Content);
                    break;
                case Senda.Core.Enums.MessageRole.System:
                    chatHistory.AddSystemMessage(msg.Content);
                    break;
            }
        }

        var result = await _chatService.GetChatMessageContentAsync(
            chatHistory, 
            new OpenAIPromptExecutionSettings { MaxTokens = 1000, Temperature = 0.2f }, 
            _kernel);

        return result.ToString();
    }
}

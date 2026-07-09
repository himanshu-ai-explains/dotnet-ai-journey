using Microsoft.AspNetCore.Identity;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;

namespace AIApp3SemanticKernel.Services
{
    public class ChatService
    {
        private readonly IChatCompletionService _chatService;
        private readonly Dictionary<string, ChatHistory> _sessions = new();
        public ChatService(IChatCompletionService chatService) => _chatService = chatService;

        public async Task<string> ChatAsync(string message, string sessionId)
        {
            if(!_sessions.ContainsKey(sessionId))
            {
                var history = new ChatHistory();

                history.AddSystemMessage
                    (
                     "You are a helpful AI assistant built by Himanshu " +
                "as part of his .NET + AI learning journey. " +
                "Answer clearly, concisely, and helpfully."
                    );

                _sessions[sessionId] = history;
            }

            var chatHistory = _sessions[sessionId];

            chatHistory.AddUserMessage(message);

            var response = await _chatService.GetChatMessageContentAsync(chatHistory);

            string aiReply = response.Content ?? "I could not generate a response";

            chatHistory.AddAssistantMessage(aiReply);

            return aiReply;
        }
    }
}

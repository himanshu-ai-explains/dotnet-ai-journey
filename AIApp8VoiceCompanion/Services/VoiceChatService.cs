using AIApp8VoiceCompanion.Hubs;
using AIApp8VoiceCompanion.Models;
using Microsoft.AspNetCore.SignalR;
using Microsoft.SemanticKernel.ChatCompletion;

namespace AIApp8VoiceCompanion.Services
{
    public class VoiceChatService
    {
        private readonly IChatCompletionService _chatService;
        private readonly ConversationStore _conversations;
        private readonly IHubContext<VoiceChatHub> _hub;


        public VoiceChatService(IChatCompletionService chatService, ConversationStore conversations, IHubContext<VoiceChatHub> hub)
            => (_chatService, _conversations, _hub) = (chatService, conversations, hub);


        public async Task RespondAsync(string sessionId, string userMessage)
        {
            // Get this session's history (or create it, first message). The
            // conversation "remembering" from Step 2 happens here, transparently.
            var sessionLock = _conversations.GetLock(sessionId);
            await sessionLock.WaitAsync(); // waits its turn if another request is mid-stream


            try
            {

                var history = _conversations.GetOrCreate(sessionId);
                history.AddUserMessage(userMessage);

                var fullResponse = new System.Text.StringBuilder();

                // THIS is the one line that's genuinely new in this whole app.
                // GetChatMessageContentAsync (every app before this) waits for the
                // ENTIRE answer, then returns it once. GetStreamingChatMessageContentsAsync
                // returns an IAsyncEnumerable -- a sequence you can start reading from
                // before it's finished being produced. `await foreach` pulls one small
                // piece ("chunk") at a time, the instant each is ready, rather than
                // waiting for the whole thing.
                await foreach (var chunk in _chatService.GetStreamingChatMessageContentsAsync(history))
                {
                    string text = chunk.Content ?? string.Empty;
                    if (string.IsNullOrEmpty(text)) continue;


                    fullResponse.Append(text);

                    // Push just this small piece to the browser immediately -- same
                    // SendAsync mechanism as App 7.5, just firing many times per
                    // answer instead of once per agent.
                    await _hub.Clients.Group(sessionId).SendAsync("ReceiveChatChunk", new ChatChunkUpdate
                    {
                        SessionId = sessionId,
                        TextChunk = text,
                        IsFinal = false
                    });
                }

                // Once the stream ends, save the FULL assembled answer to history --
                // not the individual chunks -- so next turn's context is one clean
                // message, exactly like a normal conversation transcript.
                history.AddAssistantMessage(fullResponse.ToString());

                // One last push, empty text, IsFinal = true. This carries no content --
                // it's purely a signal the browser uses to know "the AI has finished
                // talking, start listening for the next question."
                await _hub.Clients.Group(sessionId).SendAsync("ReceiveChatChunk", new ChatChunkUpdate
                {
                    SessionId = sessionId,
                    TextChunk = string.Empty,
                    IsFinal = true
                });
            }

            finally
            {
                sessionLock.Release(); // always release, even if something threw
            }
        }

    }
}

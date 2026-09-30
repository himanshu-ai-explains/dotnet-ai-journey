using Microsoft.AspNetCore.SignalR;

namespace AIApp8VoiceCompanion.Hubs
{
    public class VoiceChatHub : Hub
    {
        public async Task JoinSession(string sessionId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, sessionId);
        }
    }
}

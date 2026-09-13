using Microsoft.AspNetCore.SignalR;
namespace AIApp8MultiAgent.Hubs
{
    public class AgentReasoningHub : Hub
    {
        public async Task JoinSession(string sessionId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, sessionId);
        }
    }
}

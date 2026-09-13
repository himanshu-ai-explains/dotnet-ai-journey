namespace AIApp8MultiAgent.Models
{
    public enum AgentRole
    {
        Researcher,
        Critic,
        Synthesizer
    }

    public class AgentUpdate
    {
        public string SessionId{ get; set; } 
        public AgentRole Role { get; set; }
        public string Content { get; set; } = string.Empty;

        public int Step { get; set; }

        public bool IsFinal { get; set; }

        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }

    public class AgentAskRequest
    {
        public string Question { get; set; } = string.Empty;
        public string SessionId { get; set; } = string.Empty;
    }

    public class AgentSessionResult
    {
        public string Question { get; set; } = string.Empty;
        public List<AgentUpdate> Transcript { get; set; } = new();
        public string FinalAnswer { get; set; } = string.Empty;
    }
}

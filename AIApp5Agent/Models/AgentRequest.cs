namespace AIApp5Agent.Models
{
    public class AgentRequest
    {
        public string Message { get; set; } = string.Empty;

        public string SessionId { get; set; } = "default";
    }

    public class AgentResponse
    {
        public string Answer { get; set; } = string.Empty;
        public List<string> ToolsUsed { get; set; } = new();
        public int ReasoningSteps { get; set; }

        public string SessionId { get; set; } = string.Empty;
    }
}

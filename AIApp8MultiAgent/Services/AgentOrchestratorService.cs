using AIApp8MultiAgent.Hubs;
using AIApp8MultiAgent.Models;
using Microsoft.AspNetCore.SignalR;
using Microsoft.SemanticKernel.ChatCompletion;


namespace AIApp8MultiAgent.Services
{
    public class AgentOrchestratorService
    {
        private readonly IChatCompletionService _chatService;
        private readonly IHubContext<AgentReasoningHub> _hub;
        public AgentOrchestratorService(IChatCompletionService chatService, IHubContext<AgentReasoningHub> hub) => (_chatService, _hub) = (chatService, hub);


    private async Task<string> RunAgentAsync(AgentRole role,int step,string systemPrompt,string userMessage,string sessionId,
        bool isFinal,
        List<AgentUpdate> transcript)
        {
            var history = new ChatHistory();
            history.AddSystemMessage(systemPrompt);
            history.AddUserMessage(userMessage);

            var response = await _chatService.GetChatMessageContentAsync(history);
            string content = response.Content ?? "(no response)";

            var update = new AgentUpdate
            {
                SessionId = sessionId,
                Role = role,
                Content = content,
                Step = step,
                IsFinal = isFinal
            };
            transcript.Add(update);

            await _hub.Clients.Group(sessionId).SendAsync("ReceiveAgentUpdate", update);

            return content;
        }

        public async Task<AgentSessionResult> RunAsync(string question, string sessionId)
        {
            var transcript = new List<AgentUpdate>();

            // Step 1: Researcher — gathers facts and angles. No opinion yet.
            string researcherOutput = await RunAgentAsync(
                role: AgentRole.Researcher,
                step: 1,
                systemPrompt: """
            You are the Researcher in a three-agent reasoning team.
            Given the user's question, lay out the key facts, angles, and
            considerations relevant to answering it well. Do NOT give a final
            answer or opinion -- that is not your job. Be concise: 3-5 bullet points.
            """,
                userMessage: question,
                sessionId: sessionId,
                isFinal: false,
                transcript: transcript);

            // Step 2: Critic — sees the question AND the Researcher's output.
            string criticOutput = await RunAgentAsync(
                role: AgentRole.Critic,
                step: 2,
                systemPrompt: """
            You are the Critic in a three-agent reasoning team.
            You will be given a question and a Researcher's findings on it.
            Challenge weak points, flag missing considerations, or note where
            the Researcher's findings could be wrong or incomplete.
            Be direct and specific. 2-4 sentences.
            """,
                userMessage: $"Question: {question}\n\nResearcher's findings:\n{researcherOutput}",
                sessionId: sessionId,
                isFinal: false,
                transcript: transcript);

            // Step 3: Synthesizer — sees everything, produces the final answer.
            string synthesizerOutput = await RunAgentAsync(
                role: AgentRole.Synthesizer,
                step: 3,
                systemPrompt: """
            You are the Synthesizer in a three-agent reasoning team.
            You will be given a question, a Researcher's findings, and a
            Critic's challenges to those findings. Produce one clear, final
            answer for the user that accounts for the Critic's points.
            Do not mention "Researcher" or "Critic" by name -- just answer well.
            """,
                userMessage: $"Question: {question}\n\nResearcher's findings:\n{researcherOutput}\n\nCritic's challenges:\n{criticOutput}",
                sessionId: sessionId,
                isFinal: true,
                transcript: transcript);

            return new AgentSessionResult
            {
                Question = question,
                Transcript = transcript,
                FinalAnswer = synthesizerOutput
            };
        }


    }
}

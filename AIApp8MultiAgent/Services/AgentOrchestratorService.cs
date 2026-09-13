using AIApp8MultiAgent.Hubs;
using AIApp8MultiAgent.Models;
using Microsoft.AspNetCore.SignalR;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;


namespace AIApp8MultiAgent.Services
{
    public class AgentOrchestratorService
    {
        private readonly IChatCompletionService _chatService;
        private readonly IHubContext<AgentReasoningHub> _hub;
        public AgentOrchestratorService(IChatCompletionService chatService, IHubContext<AgentReasoningHub> hub) => (_chatService, _hub) = (chatService, hub);


        private async Task<string> RunAgentAsync(AgentRole role, int step, string systemPrompt, string userMessage, string sessionId, bool isFinal, List<AgentUpdate> transcript,
        int maxTokens)
        {
            var history = new ChatHistory();
            history.AddSystemMessage(systemPrompt);
            history.AddUserMessage(userMessage);

            var settings = new OpenAIPromptExecutionSettings { MaxTokens = maxTokens };
            var response = await _chatService.GetChatMessageContentAsync(history, settings);
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
       role: AgentRole.Researcher, step: 1,
       systemPrompt: """
        You are the Researcher in a three-agent reasoning team.
        List exactly 3 short bullet points -- the key facts or angles on the
        question. One sentence per bullet. No preamble, no "here are the key
        points" -- start directly with the first bullet. Do NOT give an opinion.
        """,
       userMessage: question, sessionId: sessionId, isFinal: false,
       transcript: transcript, maxTokens: 120);

            // Step 2: Critic — sees the question AND the Researcher's output.
            string criticOutput = await RunAgentAsync(
                role: AgentRole.Critic, step: 2,
                systemPrompt: """
        You are the Critic in a three-agent reasoning team.
        In exactly 2 short sentences, challenge the single weakest or most
        important gap in the Researcher's findings. No preamble -- start
        directly with the critique.
        """,
                userMessage: $"Question: {question}\n\nResearcher's findings:\n{researcherOutput}",
                sessionId: sessionId, isFinal: false, transcript: transcript, maxTokens: 90);
            // Step 3: Synthesizer — sees everything, produces the final answer.
            string synthesizerOutput = await RunAgentAsync(
                role: AgentRole.Synthesizer, step: 3,
                systemPrompt: """
        You are the Synthesizer in a three-agent reasoning team.
        Give ONE final answer in 3 sentences or fewer, accounting for the
        Critic's point. No preamble, no restating the question -- start
        directly with the answer.
        """,
                userMessage: $"Question: {question}\n\nResearcher's findings:\n{researcherOutput}\n\nCritic's challenges:\n{criticOutput}",
                sessionId: sessionId, isFinal: true, transcript: transcript, maxTokens: 150);

            return new AgentSessionResult
            {
                Question = question,
                Transcript = transcript,
                FinalAnswer = synthesizerOutput
            };
        }


    }
}

using AIApp5Agent.Models;
using AIApp5Agent.Plugins;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;

namespace AIApp5Agent.Services;

public class AgentService
{
    private readonly Kernel _kernel;

    // Store conversation histories per session
    // Each user gets their own conversation context
    private static readonly Dictionary<string, ChatHistory> _sessions = new();

    // Track which tools were used in the last call
    // AutoFunctionInvocationFilter populates this
    private readonly List<string> _toolsUsed = new();
    private int _reasoningSteps = 0;

    public AgentService(Kernel kernel)
    {
        _kernel = kernel;
    }

    public async Task<AgentResponse> ChatAsync(string message, string sessionId)
    {
        // Reset tool tracking for this call
        _toolsUsed.Clear();
        _reasoningSteps = 0;

        // Get or create conversation history for this session
        if (!_sessions.TryGetValue(sessionId, out var history))
        {
            history = new ChatHistory();

            // SYSTEM PROMPT — defines the agent's identity, personality, and capabilities
            // This is the most important prompt in the entire app
            history.AddSystemMessage("""
                You are a smart productivity assistant and AI agent with access to several tools.
                You help users manage tasks, do calculations, get date/time information, 
                and provide information about the Indian tech and AI ecosystem.
                
                IMPORTANT BEHAVIOUR RULES:
                - Always use your tools when relevant — don't guess at dates, times, or calculations
                - When asked to do multiple things, do ALL of them using your tools
                - After using tools, explain what you found in a clear, friendly way
                - For tasks: always confirm what you added or changed
                - Be concise but complete
                - Use ₹ for Indian Rupees when discussing money
                - You are built with .NET 8 and Semantic Kernel by Himanshu, a .NET AI developer based in Delhi
                """);

            _sessions[sessionId] = history;
        }

        // Add user's message to history
        history.AddUserMessage(message);

        // ── THE KEY SETTING: FunctionChoiceBehavior.Auto() ──
        // This single setting turns a chatbot into an agent
        // The LLM will automatically:
        // 1. Decide if any tool is needed
        // 2. Call it with the right arguments
        // 3. Read the result
        // 4. Decide if more tools are needed
        // 5. Repeat until it has everything it needs
        // 6. Generate the final answer
        // All of this happens automatically — ReAct pattern in one line
        var executionSettings = new OpenAIPromptExecutionSettings
        {
            Temperature = 0.3, // lower = more deterministic tool use decisions
            FunctionChoiceBehavior = FunctionChoiceBehavior.Auto()
        };

        var chatService = _kernel.GetRequiredService<IChatCompletionService>();

        // Get the agent's response
        // The kernel handles all tool calling automatically
        var response = await chatService.GetChatMessageContentAsync(
            history,
            executionSettings,
            _kernel);

        string answer = response.Content ?? "I could not generate a response.";

        // Add assistant's response to history for next turn
        history.AddAssistantMessage(answer);

        return new AgentResponse
        {
            Answer = answer,
            ToolsUsed = new List<string>(_toolsUsed),
            ReasoningSteps = _reasoningSteps,
            SessionId = sessionId
        };
    }

    // Called by the filter to track tool usage
    public void RecordToolUse(string toolName)
    {
        _toolsUsed.Add(toolName);
        _reasoningSteps++;
    }
}
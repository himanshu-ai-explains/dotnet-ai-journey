using AIApp5Agent.Services;
using Microsoft.SemanticKernel;

namespace AIApp5Agent.Filters;

// AutoFunctionInvocationFilter: fires every time the agent calls a tool
// We use it to track which tools were used — for the API response
// In production you'd also use this for logging, auditing, rate limiting
public class ToolTrackingFilter : IAutoFunctionInvocationFilter
{
    private readonly AgentService _agentService;

    public ToolTrackingFilter(AgentService agentService)
    {
        _agentService = agentService;
    }

    // This method is called BEFORE each tool invocation
    // context.Function has the function being called
    // await next(context) actually calls the function
    public async Task OnAutoFunctionInvocationAsync(
        AutoFunctionInvocationContext context,
        Func<AutoFunctionInvocationContext, Task> next)
    {
        // Record which tool is being called
        string toolName = $"{context.Function.PluginName}.{context.Function.Name}";
        _agentService.RecordToolUse(toolName);

        // Log to console so you can see agent reasoning in real time
        Console.WriteLine($"🔧 Agent calling tool: {toolName}");

        // Actually execute the function
        await next(context);

        Console.WriteLine($" Tool {toolName} completed");
    }
}
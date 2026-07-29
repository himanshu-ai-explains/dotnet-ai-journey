using AIApp5Agent.Models;
using AIApp5Agent.Services;
using Microsoft.AspNetCore.Mvc;

namespace AIApp5Agent.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AgentController : ControllerBase
{
    private readonly AgentService _agentService;

    public AgentController(AgentService agentService)
    {
        _agentService = agentService;
    }

    // POST /api/agent/chat
    // Send a message to the agent — it reasons, uses tools, and responds
    [HttpPost("chat")]
    public async Task<IActionResult> Chat([FromBody] AgentRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Message))
            return BadRequest("Message cannot be empty.");

        try
        {
            var response = await _agentService.ChatAsync(
                request.Message,
                request.SessionId);

            return Ok(response);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }

    // GET /api/agent/capabilities
    // Lists all tools available to the agent
    [HttpGet("capabilities")]
    public IActionResult GetCapabilities()
    {
        return Ok(new
        {
            agent = "Himanshu's .NET AI Agent",
            builtWith = "Semantic Kernel + OpenAI GPT-4o-mini",
            plugins = new[]
            {
                new { name = "DateTimePlugin", tools = new[] { "GetCurrentDateTime", "GetCurrentDate", "GetDayOfWeek" } },
                new { name = "MathPlugin", tools = new[] { "Calculate", "CalculateTip", "ConvertTemperature", "ConvertUsdToInr" } },
                new { name = "TaskPlugin", tools = new[] { "AddTask", "GetAllTasks", "CompleteTask", "DeleteTask", "GetPendingTasks" } },
                new { name = "IndiaInfoPlugin", tools = new[] { "GetCityTechInfo", "GetAIJobMarketInfo", "GetCertificationInfo" } }
            },
            tryAsking = new[]
            {
                "What time is it in India right now?",
                "Calculate 18% GST on ₹5000",
                "Add a high priority task to review my RAG application",
                "What is the AI job market like in Delhi?",
                "Convert $100 to rupees and tell me what day it is today",
                "Add 3 tasks: study agents, build app 5, write LinkedIn post",
                "What are the Microsoft AI certifications I should get?"
            }
        });
    }

    // GET /api/agent/health
    [HttpGet("health")]
    public IActionResult Health() =>
        Ok(new
        {
            status = "healthy",
            app = "AIApp5 - Semantic Kernel AI Agent",
            builder = "Himanshu — .NET + AI Journey",
            pattern = "ReAct (Reason + Act) via FunctionChoiceBehavior.Auto()"
        });
}
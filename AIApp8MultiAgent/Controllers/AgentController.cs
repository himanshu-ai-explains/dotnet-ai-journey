using AIApp8MultiAgent.Models;
using AIApp8MultiAgent.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace AIApp8MultiAgent.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AgentController : ControllerBase
    {
        private readonly AgentOrchestratorService _orchestrator;

        public AgentController(AgentOrchestratorService orchestrator) => _orchestrator = orchestrator;

        [HttpPost("ask")]
        public async Task<ActionResult<AgentSessionResult>> Ask([FromBody] AgentAskRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Question))
                return BadRequest("Question is required.");

            if (string.IsNullOrWhiteSpace(request.SessionId))
                return BadRequest("SessionId is required -- generate one client-side before calling this.");

            var result = await _orchestrator.RunAsync(request.Question, request.SessionId);
            return Ok(result);
        }


    }
}

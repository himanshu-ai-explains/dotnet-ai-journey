using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using AIApp3SemanticKernel.Models;
using AIApp3SemanticKernel.Services;


namespace AIApp3SemanticKernel.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ChatController : ControllerBase
    {
        private readonly ChatService _chatService;

        public ChatController(ChatService chatService) => _chatService = chatService;

        [HttpPost]
        public async Task<IActionResult> Chat([FromBody] ChatRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Message))
                return BadRequest("Message cannot be empty");

            try
            {
                string reply = await _chatService.ChatAsync
                    (
                    request.Message,
                    request.SessionId
                    );

                return Ok(new { reply });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }


        [HttpGet("health")]
        public IActionResult Health()
        {
            return Ok(new
            {
                status = "healthy",
                app = "AIApp3 - Semantic Kernel Chatbot",
                builder = "Himanshu - .Net + AI Journey"
            });
        }
        
    }
}

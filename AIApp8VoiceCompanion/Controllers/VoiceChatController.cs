using AIApp8VoiceCompanion.Models;
using AIApp8VoiceCompanion.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace AIApp8VoiceCompanion.Controllers
{
    
    [ApiController]
    [Route("api/voicechat")]
    public class VoiceChatController : ControllerBase
    {
        private readonly VoiceChatService _voiceChat;

        public VoiceChatController(VoiceChatService voiceChat) => _voiceChat = voiceChat;

        [HttpPost("send")]
        public async Task<IActionResult> Send([FromBody] VoiceChatRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.UserMessage))
                return BadRequest("UserMessage is required.");

            if (string.IsNullOrWhiteSpace(request.SessionId))
                return BadRequest("SessionId is required -- generate one client-side before calling this.");

            // Notice this returns almost immediately, with an empty - ish
            // 202 response -- NOT the AI's answer. That's deliberate: the
            // real answer arrives over SignalR, chunk by chunk, while this
            // HTTP call has already finished. Compare this to App 7.5's
            // controller, which awaited the full result and returned it in
            // the response body as a backup. Here there's no backup value
            // in the HTTP response at all -- SignalR isn't a nice-to-have
            // alongside the real transport, it IS the only transport for
            // the answer. That's the one real structural difference from
            // every controller you've written before this one.

            await _voiceChat.RespondAsync(request.SessionId, request.UserMessage);
            return Accepted();
        }

    }
}

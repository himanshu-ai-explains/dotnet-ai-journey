using AIApp7Voice.Models;
using AIApp7Voice.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace AIApp7Voice.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class VoiceController : ControllerBase
    {
        public readonly VoiceService _voiceService;

        private static readonly string[] AllowedAudioTypes =
    [
        "audio/wav", "audio/mpeg", "audio/mp3",
        "audio/mp4", "audio/m4a", "audio/webm",
        "audio/ogg", "audio/x-m4a",
        "video/webm",  // Chrome records audio as video/webm
        "application/octet-stream"  // Generic fallback
    ];

        private const long MaxAudioSizeBytes = 25 * 1024 * 1024;

        public VoiceController(VoiceService voiceService)
        {
            _voiceService = voiceService;
        }
        [HttpPost("transcribe")]
        [RequestSizeLimit(25 * 1024 * 1024)]
        public async Task<IActionResult> Transcribe(IFormFile audio)
        {
            var validation = ValidateAudio(audio);
            if (validation != null) return validation;

            try
            {
                var result = await _voiceService.TranscribeAsync(audio);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        // POST /api/voice/speak
        // Send text → get back spoken audio as Base64 MP3
        // Use case: text-to-speech for accessibility, audio content generation
        [HttpPost("speak")]
        public async Task<IActionResult> Speak([FromBody] VoiceSpeakRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Text))
                return BadRequest(new { error = "Text cannot be empty." });

            if (request.Text.Length > 4096)
                return BadRequest(new
                {
                    error = "Text too long. Maximum 4096 characters per request."
                });

            try
            {
                var result = await _voiceService.SpeakAsync(request.Text, request.Voice);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        // POST /api/voice/ask
        // The complete voice pipeline — audio in, audio out
        // Stage 1: Whisper transcribes your audio
        // Stage 2: GPT-4o-mini generates an answer
        // Stage 3: TTS converts the answer to speech
        // This is the endpoint that makes a complete voice assistant
        [HttpPost("ask")]
        [RequestSizeLimit(25 * 1024 * 1024)]
        public async Task<IActionResult> Ask(
            IFormFile audio,
            [FromForm] string voice = "nova")
        {
            var validation = ValidateAudio(audio);
            if (validation != null) return validation;

            try
            {
                var result = await _voiceService.AskByVoiceAsync(audio, voice);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        // GET /api/voice/voices
        // Returns available TTS voices with descriptions
        // Helps users pick the right voice for their use case
        [HttpGet("voices")]
        public IActionResult GetVoices()
        {
            return Ok(new
            {
                voices = new[]
                {
                new { name="alloy",   description="Neutral and clear. Good for AI assistants." },
                new { name="echo",    description="Male, clear and professional." },
                new { name="fable",   description="British accent, warm and expressive." },
                new { name="onyx",    description="Deep and authoritative." },
                new { name="nova",    description="Female, warm and conversational. Good for assistants." },
                new { name="shimmer", description="Female, soft and gentle." }
            },
                recommended = "nova for voice assistants, alloy for neutral AI responses"
            });
        }

        // GET /api/voice/health
        [HttpGet("health")]
        public IActionResult Health() => Ok(new
        {
            status = "healthy",
            app = "AIApp7 - Voice AI Pipeline",
            builder = "Himanshu — .NET + AI Journey",
            pipeline = "Audio → Whisper (ASR) → GPT-4o-mini (LLM) → TTS → Audio",
            stages = new[]
            {
            "POST /api/voice/transcribe — audio file to text",
            "POST /api/voice/speak — text to audio",
            "POST /api/voice/ask — full voice pipeline (audio in → audio out)"
        },
            supportedFormats = new[] { "MP3", "WAV", "WebM", "M4A", "OGG" }
        });

        // ── Audio Validation ──
        private IActionResult? ValidateAudio(IFormFile? audio)
        {
            if (audio == null || audio.Length == 0)
                return BadRequest(new { error = "Please upload an audio file." });

            if (audio.Length > MaxAudioSizeBytes)
                return BadRequest(new
                {
                    error = $"File too large. Maximum 25MB. Your file: {audio.Length / 1024 / 1024}MB"
                });

            return null;
        }

    }
}

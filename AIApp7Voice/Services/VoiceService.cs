using AIApp7Voice.Models;
using Microsoft.Extensions.AI;
using Microsoft.SemanticKernel.ChatCompletion;
using OpenAI;
using OpenAI.Audio;

namespace AIApp7Voice.Services
{
    public class VoiceService
    {
        private readonly IChatCompletionService _chatService;
        private readonly AudioClient _whisperClient;
        private readonly AudioClient _ttsClient;
        private readonly ILogger<VoiceService> _logger;

        public VoiceService(IChatCompletionService chatService, OpenAIClient openAiClient, ILogger<VoiceService> logger)
        {
            _chatService = chatService;
            _logger = logger;

            _whisperClient = openAiClient.GetAudioClient("whisper-1");
            _ttsClient = openAiClient.GetAudioClient("tts-1");
        }

        public async Task<TranscribeResult> TranscribeAsync(IFormFile audioFile)
        {
            _logger.LogInformation("Transcribing audio: {FileName} ({Size} bytes)",
            audioFile.FileName, audioFile.Length);

            using var audioStream = audioFile.OpenReadStream();

            var options = new AudioTranscriptionOptions
            {
                ResponseFormat = AudioTranscriptionFormat.Text,
                Temperature = 0
            };

            var transcriptionResult = await _whisperClient.TranscribeAudioAsync
                (
                audioStream,
                audioFile.FileName,
                options
                );

            string transcript = transcriptionResult.Value.Text;
            int wordCount = transcript.Split(' ',

                StringSplitOptions.RemoveEmptyEntries).Length;

            _logger.LogInformation("Transcribed {WordCount} words", wordCount);

            return new TranscribeResult
            {
                Transcript = transcript,
                FileName = audioFile.FileName,
                WordCount = wordCount,
                DetectedLanguage = "auto-dectected"
            };
        }

        public async Task<SpeakResult> SpeakAsync(string text, string voiceName = "alloy")
        {
            _logger.LogInformation("Generating speech for {CharCount} characters", text.Length);

            var voice = voiceName.ToLower() switch
            {
                "echo" => GeneratedSpeechVoice.Echo,
                "fable" => GeneratedSpeechVoice.Fable,
                "onyx" => GeneratedSpeechVoice.Onyx,
                "nova" => GeneratedSpeechVoice.Nova,
                "shimmer" => GeneratedSpeechVoice.Shimmer,
                _ => GeneratedSpeechVoice.Alloy
            };

            var options = new SpeechGenerationOptions
            {
                SpeedRatio = 1.0f
            };

            var response = await _ttsClient.GenerateSpeechAsync(

                text,
                voice,
                options
                );

            byte[] audioBytes = response.Value.ToArray();

            string audioByte64 = Convert.ToBase64String(audioBytes);

            _logger.LogInformation("Generated {Bytes} bytes of audio", audioBytes.Length);

            return new SpeakResult
            {
                AudioBase64 = audioByte64,
                CharacterCount = audioBytes.Length,
                Format = "mp3",
                Voice = voiceName,
            };

        }

        public async Task<VoiceAskResult> AskByVoiceAsync(
        IFormFile audioFile,
        string voiceName = "nova")
        {
            _logger.LogInformation("Voice pipeline started: {FileName}", audioFile.FileName);

            // ── Stage 1: Transcribe user's audio ──
            _logger.LogInformation("Stage 1: Transcribing...");
            var transcription = await TranscribeAsync(audioFile);
            string userText = transcription.Transcript;
            _logger.LogInformation("Transcribed: {Text}", userText);

            // ── Stage 2: Get LLM answer ──
            _logger.LogInformation("Stage 2: Getting LLM answer...");

            var chatHistory = new Microsoft.SemanticKernel.ChatCompletion.ChatHistory();

            // System prompt tuned for voice responses
            // Why shorter sentences: TTS sounds more natural with conversational sentences
            // Bullet points and markdown don't translate well to audio
            chatHistory.AddSystemMessage("""
            You are a helpful voice assistant. 
            Keep responses concise — 2 to 4 sentences maximum.
            Speak naturally as if having a conversation.
            Do not use bullet points, markdown, or formatting.
            Do not use special characters or symbols.
            Spell out numbers and abbreviations.
            """);

            chatHistory.AddUserMessage(userText);

            var llmResponse = await _chatService.GetChatMessageContentAsync(chatHistory);
            string answerText = llmResponse.Content
                ?? "I could not generate a response.";

            _logger.LogInformation("LLM answer: {Answer}", answerText);

            // ── Stage 3: Convert answer to audio ──
            _logger.LogInformation("Stage 3: Converting to speech...");
            var speechResult = await SpeakAsync(answerText, voiceName);

            return new VoiceAskResult
            {
                UserTranscript = userText,
                Answer = answerText,
                AnswerAudioBase64 = speechResult.AudioBase64,
                AudioFormat = "mp3",
                Voice = voiceName
            };
        }
    }
}

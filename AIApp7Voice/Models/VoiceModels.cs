namespace AIApp7Voice.Models
{
    public class TranscribeResult
    {
        public string Transcript { get; set; } = string.Empty;
        public string DetectedLanguage { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
        public int WordCount { get; set; }

    }

    public class SpeakResult
    {
        public string AudioBase64 { get; set; } = string.Empty;
        public string Format { get; set; } = "mp3";
        public string Voice { get; set; } = string.Empty;

        public int CharacterCount { get; set; }
    }

    public class VoiceAskResult
    {
        public string UserTranscript { get; set; } = string.Empty;
        public string Answer { get; set; } = string.Empty;
        public string AnswerAudioBase64 { get; set; } = string.Empty;
        public string AudioFormat { get; set; } = "mp3";
        public string Voice { get; set; } = string.Empty;
    }

    public class VoiceSpeakRequest
    {
        public string Text { get; set; } = string.Empty;
        public string Voice { get; set; } = "alloy";
    }


}

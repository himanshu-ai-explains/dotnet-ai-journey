namespace AIApp8VoiceCompanion.Models
{
    //One small piece of the AI's response, pushed to the browser the
    // instant it's generated. This is the key difference from App 7.5's
    // AgentUpdate: that fired ONCE per agent turn (three times total per
    // question). This fires MANY times per single answer -- maybe 20-30
    // times for one sentence -- because we're streaming word-by-word
    // instead of waiting for the whole response to finish.
    public class ChatChunkUpdate
    {
        public string SessionId { get; set; }
        // Just this piece of text -- not the whole answer so far, just
        // whatever new text arrived since the last chunk. The browser is
        // responsible for stitching these together as they arrive.
        public string TextChunk { get; set; } = string.Empty;
        public bool IsFinal { get; set; }
    }

    // What the browser sends every time the user speaks one turn.
    // Note there's no "conversation so far" field here -- the browser doesn't
    // track history at all. That's a deliberate difference from a typical
    // chat app: the SERVER remembers the conversation, keyed by SessionId.
    // The browser's only job is capturing speech and playing audio back.
    public class VoiceChatRequest
    {
        public string SessionId { get; set; } = string.Empty;
        public string UserMessage { get; set; } = string.Empty;
    }
}

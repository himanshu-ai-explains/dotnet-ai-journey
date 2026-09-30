using System.Collections.Concurrent;
using Microsoft.SemanticKernel.ChatCompletion;

namespace AIApp8VoiceCompanion.Services
{
    public class ConversationStore
    {

        // ConcurrentDictionary instead of a plain Dictionary: if two people
        // are using the demo at the same time, two different requests could
        // read/write this at the same instant. A plain Dictionary isn't
        // safe under that -- it can silently corrupt itself. Concurrent-
        // Dictionary handles that safety for us automatically.
        private readonly ConcurrentDictionary<string, ChatHistory> _sessions = new();

        // One lock per session, same GetOrAdd pattern as GetOrCreate above.
        // SemaphoreSlim(1, 1) means "only one thread may hold this at a time" --
        // exactly the guarantee we need: never let two RespondAsync calls touch
        // the same session's ChatHistory concurrently.
        private readonly ConcurrentDictionary<string, SemaphoreSlim> _locks = new();

        public SemaphoreSlim GetLock(string sessionId) =>
            _locks.GetOrAdd(sessionId, _ => new SemaphoreSlim(1, 1));
        public ChatHistory GetOrCreate(string sessionId)
        {
            // GetOrAdd: if this sessionId already has a history, return it
            // as-is (with everything said so far intact). If it's brand
            // new, build a fresh one with the system prompt below, store
            // it, and return that instead. Either way, one line handles
            // both "continuing a chat" and "starting a chat."

            return _sessions.GetOrAdd(sessionId, _ =>
            {
                var history = new ChatHistory();
                history.AddSystemMessage(
                    "You are a friendly, concise voice assistant. Keep answers " +
                    "short and conversational -- 1-3 sentences -- since your " +
                    "response will be read aloud, not read on screen. Never use " +
                    "markdown, bullet points, or special characters."
                    );

                return history;
            });
        }
    }
}

using System.Diagnostics;
using System.Runtime.CompilerServices;
using Microsoft.SemanticKernel.ChatCompletion;
using System.Text;

namespace AIApp9OfflineAI.Services;

public class OfflineChatService
{
    private readonly IChatCompletionService _chat;
    private readonly ILogger<OfflineChatService> _logger;

    // One model, one CPU: run one generation at a time. Same idea as the
    // per-session lock in App 8, but here the shared resource is the model itself.
    private static readonly SemaphoreSlim _gate = new(1, 1);

    private const string SystemPrompt =
    "You are a helpful assistant running fully offline on the user's own machine. " +
    "Answer clearly in at most 2 sentences.";

    // Small models sometimes fail to stop and start inventing the next "turn"
    // from their training data. If any of these appear, we cut the response there.
    private static readonly string[] StopSequences =
    {
        "<|end|>", "<|user|>", "<|assistant|>", "<|system|>", "\nInstruction", "Instruction 2"
    };
    private static readonly int HoldBack = StopSequences.Max(s => s.Length) - 1;
    public OfflineChatService(IChatCompletionService chat, ILogger<OfflineChatService> logger)
    {
        _chat = chat;
        _logger = logger;
    }

    private static int FindStop(string text)
    {
        var best = -1;
        foreach (var s in StopSequences)
        {
            var i = text.IndexOf(s, StringComparison.Ordinal);
            if (i >= 0 && (best < 0 || i < best)) best = i;
        }
        return best;
    }
    public async IAsyncEnumerable<string> AskAsync(
        string question,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        var history = new ChatHistory(SystemPrompt);
        history.AddUserMessage(question);

        await _gate.WaitAsync(ct);
        try
        {
            var sw = Stopwatch.StartNew();
            long firstMs = 0;
            var chunks = 0;
            var stopped = false;
            var pending = new StringBuilder();

            await foreach (var chunk in _chat.GetStreamingChatMessageContentsAsync(history, cancellationToken: ct))
            {
                if (string.IsNullOrEmpty(chunk.Content)) continue;

                chunks++;
                if (chunks == 1)
                {
                    firstMs = sw.ElapsedMilliseconds;
                    _logger.LogInformation("First token after {Ms} ms", firstMs);
                }

                pending.Append(chunk.Content);
                var text = pending.ToString();

                var stopAt = FindStop(text);
                if (stopAt >= 0)
                {
                    if (stopAt > 0) yield return text[..stopAt].TrimEnd();
                    pending.Clear();
                    stopped = true;
                    _logger.LogInformation("Stop sequence hit; cutting generation");
                    break; // leaving the loop also stops the model generating
                }

                // Hold back the last few characters in case a stop sequence is arriving in pieces.
                if (text.Length > HoldBack)
                {
                    var safe = text.Length - HoldBack;
                    yield return text[..safe];
                    pending.Remove(0, safe);
                }
            }

            if (!stopped && pending.Length > 0) yield return pending.ToString();

            var genMs = Math.Max(1, sw.ElapsedMilliseconds - firstMs);
            _logger.LogInformation("Finished in {Ms} ms, {Chunks} chunks, ~{Rate:F1} tokens/s",
                sw.ElapsedMilliseconds, chunks, chunks * 1000.0 / genMs);
        }
        finally
        {
            _gate.Release();
        }
    }
}
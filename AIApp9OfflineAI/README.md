# App 9: Offline AI (Phi-3-mini on a laptop CPU)

A small .NET 8 web app that answers questions with an AI model running **entirely on your own machine**. No API key, no cloud, no internet needed after setup. Turn Wi-Fi off and it keeps answering.

Part of my public 12-month .NET + AI journey: https://himanshu-ai-explains.github.io

## How it works

```
Browser  ->  GET /api/offline/ask?q=...  ->  OfflineChatService
                                                  |
                                          IChatCompletionService   (Semantic Kernel)
                                                  |
                                          ONNX Runtime GenAI  ->  Phi-3-mini INT4 (CPU)
```

- **Model:** Phi-3-mini-4k-instruct, INT4 quantized ONNX build (about 2.7 GB). MIT licensed by Microsoft.
- **Runtime:** ONNX Runtime GenAI, CPU only.
- **Glue:** Semantic Kernel's ONNX connector. The service codes against `IChatCompletionService`, so moving to Azure OpenAI later is a one-line change in `Program.cs`.
- **Streaming:** the answer streams to the browser as plain text and is read with `ReadableStream`.
- **Offline badge:** the page uses `navigator.onLine` plus the browser's online/offline events.

## Run it yourself

You need: .NET 8 SDK, Git, Git LFS, about 4 GB of free disk and enough free RAM to hold a 2.7 GB model.

**1. Download the model (keep it outside this repo, it's 2.7 GB):**

```bash
git lfs install
git clone --no-checkout https://huggingface.co/microsoft/Phi-3-mini-4k-instruct-onnx
cd Phi-3-mini-4k-instruct-onnx
git sparse-checkout init --cone
git sparse-checkout set cpu_and_mobile/cpu-int4-rtn-block-32-acc-level-4
git checkout main
```

**2. Point the app at the model.** Edit `appsettings.json`:

```json
"Onnx": {
  "ModelPath": "C:\\path\\to\\Phi-3-mini-4k-instruct-onnx\\cpu_and_mobile\\cpu-int4-rtn-block-32-acc-level-4"
}
```

**3. Run (use Release, it matters for speed):**

```bash
dotnet run -c Release
```

Open http://localhost:5185/ and ask something short, like "Explain dependency injection in one sentence."

**Tip for low-RAM machines:** in the model folder's `genai_config.json`, change `"max_length"` under `"search"` from 4096 to 1024. It cuts memory use and disk paging.

## Honest numbers

Measured on one Windows laptop, CPU only, no GPU:

| What | Result |
|---|---|
| First question after launch (cold start) | about 45 to 68 s while the model loads into RAM |
| First word when warm | about 7 to 10 s |
| Speed | roughly 3 tokens/s |
| A typical short answer | 14 to 28 s |

## Known limits

- **It is slow and small.** It's a 3.8B-parameter model on a CPU. It can get facts wrong. In my own test it made up an expansion for "RAG". Don't use it for anything that must be correct.
- **No memory between questions.** Each request starts a fresh chat.
- **One request at a time.** A semaphore protects the single loaded model.
- **Runaway text.** Small models sometimes fail to stop and drift into training-data fragments. The service has a stop-sequence guard that cuts generation when it sees those patterns. A side effect: an answer that legitimately contains "Instruction 2" would be cut.
- **Preview connector.** `Microsoft.SemanticKernel.Connectors.Onnx` is prerelease and experimental, so the `SKEXP0070` warning is suppressed in the `.csproj`.

## What I learned

1. A model is a big file that has to be loaded. Cold start is a real cost, and a warm-up request hides it.
2. INT4 quantization is why a 3.8B model fits in 2.7 GB and runs on a laptop at all.
3. Small models need guardrails (stop sequences, short answers) that big hosted models mostly hide from you.
4. Coding against an interface (`IChatCompletionService`) is what makes "local now, cloud later" cheap.
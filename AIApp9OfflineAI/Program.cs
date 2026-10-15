using Microsoft.SemanticKernel;
using AIApp9OfflineAI.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

var modelPath = builder.Configuration["Onnx:ModelPath"]
    ?? throw new InvalidOperationException("Onnx:ModelPath is not configured.");

// Fail fast with a clear message instead of a cryptic ONNX error later.
if (!Directory.Exists(modelPath))
    throw new DirectoryNotFoundException($"Model folder not found: {modelPath}");

// Same Semantic Kernel interface as Apps 3-8. Only this one line changes:
// OpenAI connector out, local ONNX model in.
builder.Services.AddKernel()
    .AddOnnxRuntimeGenAIChatCompletion("phi-3", modelPath);

builder.Services.AddSingleton<OfflineChatService>();

var app = builder.Build();

app.UseDefaultFiles();
app.UseStaticFiles();

app.MapControllers();

app.Run();
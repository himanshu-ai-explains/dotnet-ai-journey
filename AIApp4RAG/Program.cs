using AIApp4RAG.Services;
using Microsoft.OpenApi;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.InMemory;

try
{
    var builder = WebApplication.CreateBuilder(args);

    // ── Load & Validate API Key ──
    string? apiKey = builder.Configuration["OpenAIKey"]
        ?? builder.Configuration["OPENAI_API_KEY"]
        ?? Environment.GetEnvironmentVariable("OPENAI_API_KEY");

    if (string.IsNullOrWhiteSpace(apiKey))
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine("=================================================================");
        Console.WriteLine(" [ERROR] OpenAI API Key is missing!");
        Console.WriteLine(" Please configure it using one of the following methods:");
        Console.WriteLine("   1. User Secrets: dotnet user-secrets set \"OpenAIKey\" \"sk-...\"");
        Console.WriteLine("   2. Environment Variable: set OPENAI_API_KEY=sk-...");
        Console.WriteLine("   3. appsettings.json: { \"OpenAIKey\": \"sk-...\" }");
        Console.WriteLine("=================================================================");
        Console.ResetColor();
        return;
    }

    // ── Semantic Kernel ──
#pragma warning disable SKEXP0070
    builder.Services.AddKernel()
        .AddOpenAIChatCompletion(
            modelId: "gpt-4o-mini",
            apiKey: apiKey)
        .AddOpenAITextEmbeddingGeneration(
            modelId: "text-embedding-3-small",
            apiKey: apiKey);
#pragma warning restore SKEXP0070

    // ── Vector Store ──
    builder.Services.AddSingleton<InMemoryVectorStore>();

    // ── RAG Service ──
    builder.Services.AddScoped<RagService>();

    // ── API Infrastructure ──
    builder.Services.AddControllers();
    builder.Services.AddEndpointsApiExplorer();

    // ── Swagger ──
    builder.Services.AddSwaggerGen(c =>
    {
        c.SwaggerDoc("v1", new OpenApiInfo
        {
            Title = "AIApp4 - RAG API",
            Version = "v1",
            Description = "Upload a PDF and chat with it using AI"
        });
    });

    // ── File Upload Size ──
    builder.Services.Configure<IISServerOptions>(options =>
    {
        options.MaxRequestBodySize = 52428800;
    });

    // ── CORS ──
    builder.Services.AddCors(options =>
    {
        options.AddDefaultPolicy(policy =>
            policy.AllowAnyOrigin()
                  .AllowAnyMethod()
                  .AllowAnyHeader());
    });

    // ── Build the app ──
    var app = builder.Build();

    // ── Middleware Pipeline ──
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "RAG API V1");
        c.RoutePrefix = "swagger";
    });

    app.MapGet("/", () => Results.Redirect("/swagger"));
    app.UseCors();
    app.UseAuthorization();
    app.MapControllers();

    Console.WriteLine("App starting...");
    app.Run();
}
catch (Exception ex)
{
    Console.WriteLine($"STARTUP CRASH: {ex.Message}");
    Console.WriteLine(ex.StackTrace);
    Console.ReadKey();
}
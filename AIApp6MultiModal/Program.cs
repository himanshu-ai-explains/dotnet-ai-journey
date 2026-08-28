
using AIApp6MultiModal.Services;
using Microsoft.OpenApi;
using Microsoft.SemanticKernel;

try
{


    var builder = WebApplication.CreateBuilder(args);

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

    builder.Services.AddKernel()
    .AddOpenAIChatCompletion
    (
        modelId: "gpt-4o",
        apiKey: apiKey
    );

    builder.Services.AddScoped<VisionService>();

    builder.Services.Configure<IISServerOptions>(options =>
    {
        options.MaxRequestBodySize = 10 * 1024 * 1024;
    });

    builder.WebHost.ConfigureKestrel(options =>
    {
        options.Limits.MaxRequestBodySize = 10 * 1024 * 1024;
    });

    builder.Services.AddControllers();
    builder.Services.AddEndpointsApiExplorer();

    builder.Services.AddSwaggerGen(c =>
    {
        c.SwaggerDoc("v1", new OpenApiInfo
        {
            Title = "AIApp6 - Multi-modal Vision API",
            Version = "v1",
            Description = "Upload images — AI analyzes, extracts text, answers questions, and processes invoices"
        });
    });

    builder.Services.AddCors(options =>
    {
        options.AddDefaultPolicy(policy =>
            policy.AllowAnyOrigin()
                  .AllowAnyMethod()
                  .AllowAnyHeader());
    });


    var app = builder.Build();

    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Vision API V1");
        c.RoutePrefix = "swagger";
    });

    app.MapGet("/", () => Results.Redirect("/swagger"));
    app.UseCors();
    app.UseAuthorization();
    app.MapControllers();

    Console.WriteLine("👁️ Multi-modal Vision API starting...");
    app.Run();
}
catch (Exception ex)
{
    Console.WriteLine($"STARTUP CRASH: {ex.Message}");
    Console.WriteLine(ex.StackTrace);
    Console.ReadKey();
}
using AIApp7Voice.Services;
using Microsoft.OpenApi;
using Microsoft.SemanticKernel;


try
{


var builder = WebApplication.CreateBuilder(args);

string apiKey = builder.Configuration["OpenAIKey"] ?? "";
Console.WriteLine($"API Key: {(string.IsNullOrEmpty(apiKey) ? "MISSING!" : "Loaded OK")}");

builder.Services.AddKernel()
    .AddOpenAIChatCompletion
        (
        modelId: "gpt-4o-mini",
        apiKey: apiKey
        );

builder.Services.AddScoped<VoiceService>();

builder.Services.Configure<IISServerOptions>(options =>
{
    options.MaxRequestBodySize = 25 * 1024 * 1024;
});
builder.WebHost.ConfigureKestrel(options =>
{
    options.Limits.MaxRequestBodySize = 25 * 1024 * 1024;
});


builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "AIApp7 - Voice AI Pipeline",
        Version = "v1",
        Description = "Speak → AI listens → AI thinks → AI speaks back. " +
                      "Built with Whisper + GPT-4o-mini + OpenAI TTS + .NET 8"
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
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Voice API V1");
    c.RoutePrefix = "swagger";
});

app.MapGet("/", () => Results.Redirect("/swagger"));
app.UseCors();
app.UseAuthorization();
app.MapControllers();

Console.WriteLine("🎤 Voice AI Pipeline starting...");

app.Run();

}
catch (Exception ex)
{
    Console.WriteLine($"STARTUP CRASH: {ex.Message}");
    Console.WriteLine(ex.StackTrace);
    Console.ReadKey();
}




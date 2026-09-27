using Microsoft.SemanticKernel;
using AIApp3SemanticKernel.Services;
using Microsoft.AspNetCore.RateLimiting;


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
    .AddOpenAIChatCompletion(
        modelId: "gpt-4o-mini",
        apiKey: apiKey
    );

builder.Services.AddScoped<ChatService>();
// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddRateLimiter(options =>
{
    options.AddFixedWindowLimiter("basic", opt =>
    {
        opt.PermitLimit = 4;
        opt.Window = TimeSpan.FromMinutes(1);
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

// Configure the HTTP request pipeline.

    app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "AI Chat API V1");
    c.RoutePrefix = "swagger"; // Access at /swagger
});
app.MapGet("/", () => Results.Redirect("/swagger"));

app.UseSwaggerUI();
app.UseRateLimiter();
    app.UseCors();


app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();

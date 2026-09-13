using AIApp8MultiAgent.Hubs;
using AIApp8MultiAgent.Services;
using Microsoft.SemanticKernel;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

string apiKey = builder.Configuration["OpenAIKey"]!;

builder.Services.AddControllers()
    .AddJsonOptions(options =>
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

builder.Services.AddOpenAIChatCompletion(
    modelId: "gpt-4o-mini",
    apiKey: apiKey);

builder.Services.AddSignalR()
    .AddJsonProtocol(options =>
        options.PayloadSerializerOptions.Converters.Add(new JsonStringEnumConverter())); builder.Services.AddScoped<AgentOrchestratorService>();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowDemoSite", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});


var app = builder.Build();
app.UseStaticFiles();

app.UseCors("AllowDemoSite");
app.MapControllers();
app.MapHub<AgentReasoningHub>("/hubs/agent-reasoning");

app.Run();




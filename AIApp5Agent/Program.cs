
using AIApp5Agent.Filters;
using AIApp5Agent.Plugins;
using AIApp5Agent.Services;
using Microsoft.OpenApi;
using Microsoft.SemanticKernel;

try
{

    var builder = WebApplication.CreateBuilder(args);

    string apiKey = builder.Configuration["OpenAIKey"] ?? "";
    Console.WriteLine($"Api Key : {(string.IsNullOrEmpty(apiKey) ? "Missing" : "Loaded OK")}");

    var kernelBuilder = Kernel.CreateBuilder();

    kernelBuilder.AddOpenAIChatCompletion(
        modelId: "gpt-4o-mini",
        apiKey: apiKey
        );

    kernelBuilder.Plugins.AddFromType<DateTimePlugin>("DateTime");
    kernelBuilder.Plugins.AddFromType<MathPlugin>("Math");
    kernelBuilder.Plugins.AddFromType<TaskPlugin>("Tasks");
    kernelBuilder.Plugins.AddFromType<IndiaInfoPlugin>("IndiaInfo");

    // Add services to the container.
    // Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi

    var kernel = kernelBuilder.Build();


    builder.Services.AddSingleton(kernel);
    builder.Services.AddScoped<AgentService>();
    builder.Services.AddScoped<ToolTrackingFilter>();


    builder.Services.AddScoped<IAutoFunctionInvocationFilter>(sp =>
    sp.GetRequiredService<ToolTrackingFilter>());

    builder.Services.AddControllers();
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(c =>
    {
        c.SwaggerDoc("v1", new OpenApiInfo
        {
            Title = "AIApp5 - Semantic Kernel AI Agent",
            Version = "v1",
            Description = "An autonomous AI agent with real tools — built with .NET 8 + Semantic Kernel"
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
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Agent API V1");
        c.RoutePrefix = "swagger";
    });

    app.MapGet("/", () => Results.Redirect("/swagger"));
    app.UseCors();
    app.UseAuthorization();
    app.MapControllers();

    Console.WriteLine("🤖 AI Agent starting...");
    app.Run();

}
catch (Exception ex)
{
    Console.WriteLine($"STARTUP CRASH: {ex.Message}");
    Console.WriteLine(ex.StackTrace);
    Console.ReadKey();
}



using AIApp6MultiModal.Services;
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
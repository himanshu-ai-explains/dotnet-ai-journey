using Microsoft.SemanticKernel;
using AIApp3SemanticKernel.Services;
using Microsoft.AspNetCore.RateLimiting;


var builder = WebApplication.CreateBuilder(args);

string apiKey = builder.Configuration["OpenAIKey"]!;

builder.Services.AddKernel()
    .AddOpenAIChatCompletion
    (
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

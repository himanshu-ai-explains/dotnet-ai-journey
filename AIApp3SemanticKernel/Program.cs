using Microsoft.SemanticKernel;
using AIApp3SemanticKernel.Services;


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

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader());
});


var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    app.UseCors();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();

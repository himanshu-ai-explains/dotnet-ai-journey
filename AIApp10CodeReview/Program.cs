using AIApp10CodeReview.Models;
using AIApp10CodeReview.Services;
using Microsoft.SemanticKernel;
using System.Net.Http.Headers;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.HttpOverrides;


var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
   
builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

// A stronger model, used only to double-check findings
builder.Services.AddOpenAIChatCompletion(
    modelId: "gpt-4o",
    apiKey: builder.Configuration["OpenAIKey"]!);

builder.Services.AddScoped<ReviewService>();

builder.Services.AddHttpClient<GitHubService>(client =>
{
    client.BaseAddress = new Uri("https://api.github.com/");

    // GitHub rejects any request that has no User-Agent header
    client.DefaultRequestHeaders.UserAgent.ParseAdd("AIApp10CodeReview");
    client.DefaultRequestHeaders.Add("X-GitHub-Api-Version", "2022-11-28");

    string? token = builder.Configuration["GitHubToken"];
    if (!string.IsNullOrEmpty(token))
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
});
builder.Services.AddSingleton<DiffChunker>();   // stateless, so one instance is fine
builder.Services.AddSingleton<FindingFilter>();
builder.Services.AddSingleton<FindingVerifier>();

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    // Per visitor: each IP address gets its own counter.
    options.AddPolicy("review", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 3,
                Window = TimeSpan.FromHours(1),
                QueueLimit = 0   // reject immediately, never queue
            }));

    // Whole app: one shared counter for review calls only (page loads are free).
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(ctx =>
    {
        if (!ctx.Request.Path.StartsWithSegments("/api/review"))
            return RateLimitPartition.GetNoLimiter("other");

        return RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: "all-visitors",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 60,
                Window = TimeSpan.FromDays(1),
                QueueLimit = 0
            });
    });
    // A friendly JSON message instead of an empty 429.
    options.OnRejected = async (context, cancellationToken) =>
    {
        context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
        await context.HttpContext.Response.WriteAsJsonAsync(new
        {
            title = "Demo limit reached",
            detail = "This free demo allows a few reviews per hour. Please try again later."
        }, cancellationToken);
    };
});


// Behind Azure's front end, the real visitor IP arrives in the X-Forwarded-For header.
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;

    // On App Service, only Azure's own front end can reach this app, so we trust it.
    options.KnownIPNetworks.Clear();
    options.KnownProxies.Clear();
});

builder.Services.AddCors(options =>
{
    options.AddPolicy("portfolio", policy => policy
        .WithOrigins("https://himanshu-ai-explains.github.io")
        .WithMethods("POST")
        .WithHeaders("Content-Type"));
});

var app = builder.Build();

app.UseForwardedHeaders();   // must come before the rate limiter

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseDefaultFiles();   // serves wwwroot/index.html at "/"
app.UseStaticFiles();

app.UseCors("portfolio");

app.UseRateLimiter();

app.UseAuthorization();

app.MapControllers();

app.Run();

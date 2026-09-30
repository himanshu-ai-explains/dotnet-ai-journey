using AIApp8VoiceCompanion.Hubs;
using AIApp8VoiceCompanion.Services;
using Microsoft.SemanticKernel;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

string apiKey = builder.Configuration["OpenAIKey"]!;

builder.Services.AddControllers();

builder.Services.AddOpenAIChatCompletion(

    modelId: "gpt-4o-mini",
    apiKey: apiKey
    );

builder.Services.AddSignalR();

// THIS is the line Step 2 was building toward. Singleton means ONE
// instance of ConversationStore exists for the entire lifetime of the
// running app -- shared by every request, from every user, forever.
// If this were AddScoped (like every other service you've registered so
// far), ASP.NET Core would silently create a BRAND NEW ConversationStore,
// with an empty dictionary, for every single HTTP request -- meaning
// turn 2 would never find turn 1's conversation, because it would be
// asking a completely different, freshly-created, already-empty object.
// The conversation memory from Step 2 only actually works because of
// this one word.

builder.Services.AddSingleton<ConversationStore>();

// VoiceChatService itself stays Scoped, like every other service you've
// written -- it holds no state of its own. It just borrows the shared
// ConversationStore (singleton) to look up the right conversation each
// time. The isolation between different users' conversations comes from
// the dictionary being keyed by SessionId, not from the service's own
// lifetime.

builder.Services.AddScoped<VoiceChatService>();

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
app.MapHub<VoiceChatHub>("/hubs/voice-chat");
app.Run();

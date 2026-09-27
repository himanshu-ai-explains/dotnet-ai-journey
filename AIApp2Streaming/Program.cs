using OpenAI;
using OpenAI.Chat;
using Microsoft.Extensions.Configuration;
using System.ClientModel.Primitives;


IConfigurationRoot config = new ConfigurationBuilder()
    .AddUserSecrets<Program>()
    .Build();

string? apiKey = config["OpenAIKey"]
    ?? Environment.GetEnvironmentVariable("OPENAI_API_KEY")
    ?? Environment.GetEnvironmentVariable("OpenAIKey");

if (string.IsNullOrWhiteSpace(apiKey))
{
    Console.ForegroundColor = ConsoleColor.Yellow;
    Console.WriteLine("No OpenAI API key found in User Secrets or Environment Variables.");
    Console.WriteLine("Tip: You can persist it using: dotnet user-secrets set \"OpenAIKey\" \"sk-...\"\n");
    Console.Write("Enter your OpenAI API Key for this session: ");
    Console.ResetColor();
    apiKey = Console.ReadLine()?.Trim();
}

if (string.IsNullOrWhiteSpace(apiKey))
{
    Console.ForegroundColor = ConsoleColor.Red;
    Console.WriteLine("\n[Error] An OpenAI API Key is required to run the streaming chat application.");
    Console.ResetColor();
    return;
}

ChatClient client = new OpenAIClient(apiKey)
    .GetChatClient("gpt-4o-mini");

List<ChatMessage> messages = new()
{
    ChatMessage.CreateSystemMessage(
        "You are a helpful AI assistant. Answer clearly and concisely. In max 40 words")


};

Console.WriteLine("===============================");
Console.WriteLine(" App 2 - Streming Responses");
Console.WriteLine(" Powered by OpenAI Gpt-4.o-mini");
Console.WriteLine(" Type 'exit' to quit");

Console.WriteLine("===============================");

while(true)
{
    Console.ForegroundColor = ConsoleColor.Green;
    Console.Write("You: ");
    Console.ResetColor();

    string userInput = Console.ReadLine()!;

    if (string.IsNullOrWhiteSpace(userInput)) continue;

    if(userInput.ToLower() == "exit")
    {
        Console.WriteLine("\nGoodbye! Happy learning ");
        break;
    }

    messages.Add(ChatMessage.CreateUserMessage(userInput));

    try
    {
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.Write("AI: ");
        Console.ResetColor();


        var stream =
            client.CompleteChatStreamingAsync(messages);


        string fullReply = "";

        await foreach(StreamingChatCompletionUpdate chunk in stream)
        {
            foreach(ChatMessageContentPart part in chunk.ContentUpdate)
            {
                string token = part.Text;
                if(!string.IsNullOrEmpty(token))
                {
                    Console.Write(token);
                    fullReply += token;

                }
            }
        }

        Console.WriteLine("\n");

        messages.Add(ChatMessage.CreateAssistantMessage(fullReply));

    }

    catch(Exception ex)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"\nError: {ex.Message}");
        Console.ResetColor();
       
    }

}
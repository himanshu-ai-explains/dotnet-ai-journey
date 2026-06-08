using OpenAI;
using OpenAI.Chat;
using Microsoft.Extensions.Configuration;
using System.ClientModel.Primitives;


IConfigurationRoot config = new ConfigurationBuilder().AddUserSecrets<Program>().Build();

string apiKey = config["OpenAIKey"]!;

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
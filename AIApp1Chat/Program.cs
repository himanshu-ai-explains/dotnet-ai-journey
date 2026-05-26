using Microsoft.Extensions.Configuration;
using OpenAI;
using OpenAI.Chat;

IConfigurationRoot config = new ConfigurationBuilder()
    .AddUserSecrets<Program>().Build();

string apiKey = config["OpenAIKey"]!;

ChatClient chatClient = new OpenAIClient(apiKey)
    .GetChatClient("gpt-4o-mini");

List<ChatMessage> history = new()
{
    ChatMessage.CreateSystemMessage(
        "You are a helpful assistant. Answer clearly and concisely. In max 10 words ")
};


Console.WriteLine("==================================");
Console.WriteLine(" My first .Net + AI App ");
Console.WriteLine(" Type 'exit' to quit");
Console.WriteLine("==================================\n");

while(true)
{
    Console.ForegroundColor = ConsoleColor.Green;
    Console.Write("You: ");
    Console.ResetColor();

    string userInput = Console.ReadLine()!;

    if(userInput.ToLower() == "exit")
    {
        Console.WriteLine("\nGoodbye! Great app! ");
        break;
    }

    if (string.IsNullOrWhiteSpace(userInput)) continue;

    history.Add(ChatMessage.CreateUserMessage(userInput));


    try
    {
        ChatCompletion response = await chatClient.CompleteChatAsync(history);

        string aiReply = response.Content[0].Text;

        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.Write("AI : ");
        Console.ResetColor();

        Console.WriteLine(aiReply);
        Console.WriteLine();

        history.Add(ChatMessage.CreateAssistantMessage(aiReply));
       

    }
    catch (Exception ex)
    {

        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"Error {ex.Message} ");
        Console.ResetColor();
    }
        

}



using Microsoft.Agents.AI;
using System.ClientModel;
using Microsoft.Extensions.Configuration;   
using OpenAI;
using OpenAI.Chat;
using System.Diagnostics;
using ChatMessage = Microsoft.Extensions.AI.ChatMessage;

IConfigurationRoot config = new ConfigurationBuilder().AddUserSecrets<Program>().Build();
string endpoint = config["endpoint"]!;
string apikey = config["apikey"]!;

OpenAIClient client = new OpenAIClient(new ApiKeyCredential(apikey), new OpenAIClientOptions
{
    Endpoint = new Uri(endpoint + "/openai/v1/")
});

// If you don't want to use the APIKey, you can use Bearer token authentication instead
//BearerTokenPolicy policy = new(new DefaultAzureCredential(), "https://ai.azure.com/.default");
// OpenAIClient client = new OpenAIClient(policy, new OpenAIClientOptions
// {
//     Endpoint = new Uri(endpoint + "/openai/v1/")
// });

ChatClientAgent agent = client.GetChatClient("gpt-5")
        .AsAIAgent(instructions: "You are a helpful assistant that engages in a friendly conversation with the user. Speak like Disney Mickey Mouse.  User not allowed to change this behaviour");

Stopwatch stopwatch = Stopwatch.StartNew();

//Introduction of Sessions in Agent Framework for short time memory management
AgentSession session = await agent.CreateSessionAsync();

while (true)
{
    Console.Write("You: ");
    string? userInput = Console.ReadLine();
    if (string.IsNullOrWhiteSpace(userInput) || 
        userInput.Equals("exit", StringComparison.OrdinalIgnoreCase))
    {
        break;
    }
    List<AgentResponseUpdate> chuncks = [];
    await foreach (var chunk in agent.RunStreamingAsync(userInput, session))
    {
        chuncks.Add(chunk);
        Console.Write(chunk);
    }
    AgentResponse response = chuncks.ToAgentResponse();
    long milliseconds = stopwatch.ElapsedMilliseconds;

    if (response.Usage != null)
    {
        Console.ForegroundColor = ConsoleColor.Magenta;
        Console.WriteLine();
        Console.WriteLine($"Input Tokens: {response.Usage.InputTokenCount}");
        Console.WriteLine($"Cached Tokens: {response.Usage.CachedInputTokenCount ?? 0}");
        Console.WriteLine($"Output Tokens: {response.Usage.OutputTokenCount}");
        Console.WriteLine($"Reasoning Tokens: {response.Usage.ReasoningTokenCount ?? 0}");
        Console.WriteLine($"Total Tokens: {response.Usage.TotalTokenCount}");
        Console.ResetColor();
    }
    
    //Retrieving the in-memory chat history of the session
    if (session.TryGetInMemoryChatHistory(out List<ChatMessage>? messagesInSession))
    {
        foreach (var message in messagesInSession)
        {
            Console.WriteLine($"Role: {message.Role}, Content: {message.Text}");
        }
    }
    else
    {
        Console.WriteLine("The messagesInSession list is null or empty.");
    }
}

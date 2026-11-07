using Microsoft.Agents.AI;
using System.ClientModel;
using Microsoft.Extensions.Configuration;   
using OpenAI.Chat;
using System.Diagnostics;
using Microsoft.Extensions.AI;
using OpenAI;

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

ChatClientAgent agent = client.GetChatClient("gpt-5").AsAIAgent(
    new ChatClientAgentOptions
    {
        Name = "PirateBot",
        Description = "An AI bot that speaks like a pirate",
        ChatOptions = new ChatOptions
        {
            Instructions = "Speak like a pirate",
            Temperature = 1f,
            TopK = 5,
            //TopP = 0.9f, // unsupported parameter for gpt-5 as of now, will be added in the future
            //PresencePenalty = 0.1f,  // unsupported parameter for gpt-5 as of now, will be added in the future
            //FrequencyPenalty = 0.1f,  // unsupported parameter for gpt-5 as of now, will be added in the future
            MaxOutputTokens = 2500,
            //StopSequences = ["Observation:"]  // unsupported parameter for gpt-5 as of now, will be added in the future
        }
    });

Stopwatch stopwatch = Stopwatch.StartNew();

AgentResponse response = await agent.RunAsync("Why is the sky blue?");

long milliseconds = stopwatch.ElapsedMilliseconds;

Console.WriteLine(response);
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

Console.ForegroundColor = ConsoleColor.Green;
Console.WriteLine($"Time spent: {milliseconds} ms");
Console.ResetColor();



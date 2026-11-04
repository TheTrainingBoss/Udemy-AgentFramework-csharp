using Microsoft.Agents.AI;
using System.ClientModel;
using Microsoft.Extensions.Configuration;   
using Azure.AI.OpenAI;
using OpenAI.Chat;
using System.Diagnostics;
using Microsoft.Extensions.AI;

IConfigurationRoot config = new ConfigurationBuilder().AddUserSecrets<Program>().Build();
string endpoint = config["endpoint"]!;
string apikey = config["apikey"]!;

AzureOpenAIClient client = new AzureOpenAIClient(new Uri(endpoint), new ApiKeyCredential(apikey));

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



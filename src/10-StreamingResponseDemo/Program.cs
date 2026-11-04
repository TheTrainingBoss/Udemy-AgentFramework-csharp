using Microsoft.Agents.AI;
using System.ClientModel;
using Microsoft.Extensions.Configuration;   
using OpenAI;
using OpenAI.Chat;
using System.Diagnostics;

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

ChatClientAgent agent = client.GetChatClient("gpt-5-mini").AsAIAgent();

Stopwatch stopwatch = Stopwatch.StartNew();

// Chat without streaming
// AgentResponse response = await agent.RunAsync("Why is the sky blue?");

// Chat with Streaming
List<AgentResponseUpdate> chuncks = [];
await foreach (var chunk in agent.RunStreamingAsync("Why is the sky blue?"))
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
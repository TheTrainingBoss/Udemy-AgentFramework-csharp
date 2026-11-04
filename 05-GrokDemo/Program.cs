using System.ClientModel;
using System.Diagnostics;
using Microsoft.Agents.AI;
using OpenAI;
using OpenAI.Chat;

OpenAIClient client = new(
        new ApiKeyCredential(
        "xai-asS4ByKFwkVatAh...<enter your API key here>"), 
        new OpenAIClientOptions{
            Endpoint = new Uri("https://api.x.ai/v1")
        });

ChatClientAgent agent = client.GetChatClient("grok-4-fast-non-reasoning").AsAIAgent();

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

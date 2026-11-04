using Anthropic;
using Anthropic.Core;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using System.Diagnostics;

var client = new AnthropicClient(new ClientOptions
{
    ApiKey = "sk-ant-api0...<enter your API key here>",
    
});

ChatClientAgent agent = client.AsIChatClient().AsAIAgent(new ChatClientAgentOptions
{
    ChatOptions = new ChatOptions
    {
        MaxOutputTokens = 2000,
        ModelId="claude-sonnet-4-5-20250929"
    }
});

Stopwatch stopwatch = Stopwatch.StartNew();

AgentResponse response = await agent.RunAsync("Why is the sky blue?");

long milliseconds = stopwatch.ElapsedMilliseconds;

Console.WriteLine(response);
Console.WriteLine();

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

using System.Diagnostics;
using GitHub.Copilot;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;


await using CopilotClient client = new CopilotClient(new CopilotClientOptions
{
    UseLoggedInUser = false,
    GitHubToken = "<YOUR_GITHUB_TOKEN>"
});

await client.StartAsync();

AIAgent agent = client.AsAIAgent(new SessionConfig
{
    Model = "gpt-5-mini",
    WorkingDirectory = Environment.CurrentDirectory,
    OnPermissionRequest = PermissionHandler.ApproveAll,
    Tools = [AIFunctionFactory.Create(GetWeather, "get_weather")]
});

Stopwatch stopwatch = Stopwatch.StartNew();

AgentResponse response = await agent.RunAsync("What is the weather in Orlando?");

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

static string GetWeather(string location)
{
    // Replace this with your actual weather fetching logic
    return $"The weather in {location} is sunny.";
}

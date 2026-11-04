using Microsoft.Agents.AI;
using System.ClientModel;
using Microsoft.Extensions.Configuration;   
using Microsoft.Extensions.AI;
using OpenAI.Chat;
using System.Diagnostics;
using System.ComponentModel;
using System.Text;
using OpenAI.Responses;
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

#pragma warning disable OPENAI001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
ChatClientAgent footballAgent = client.GetResponsesClient().AsAIAgent(model: "gpt-5-mini",
    instructions:"You are an expert in Football statistics. Use the date and time tool whenever current date/time context is required. Use the web search tool for any information that is not available in your training data. Always provide sources for your information.", 
    name:"Football Bot", 
    description:"Football Bot that provides current football statistics",
    tools:
    [
        AIFunctionFactory.Create(GetDateTimeUtc, "get_current_datetime", "Get the current date and time in UTC."),
        new HostedWebSearchTool()
    ]);
#pragma warning restore OPENAI001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

#pragma warning disable OPENAI001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
AIAgent agent = client.GetResponsesClient().AsAIAgent(model: "gpt-5",
    instructions: "You are a helpful assistant. Refer all requests about football statistics to the Football Bot.",
    name: "Main Agent",
    description: "An assistant that provides weather information and football statistics.",
    tools: [footballAgent.AsAIFunction()]
    ).AsBuilder()
    .Use(Middleware)
    .Build();
#pragma warning restore OPENAI001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

Stopwatch stopwatch = Stopwatch.StartNew();

AgentResponse response = await agent.RunAsync("How many games per season are played in the premier league by all teams?");

long milliseconds = stopwatch.ElapsedMilliseconds;

static async ValueTask<object?> Middleware(AIAgent agent, FunctionInvocationContext context,
        Func<FunctionInvocationContext, CancellationToken, ValueTask<object?>> next, CancellationToken cancellationToken)
{
    StringBuilder toolDetails = new();
    toolDetails.Append($"- Tool Call: '{context.Function.Name}'");
    if (context.Arguments.Count > 0)
    {
        toolDetails.Append($" (Args: {string.Join(",", context.Arguments.Select(x => $"[{x.Key} = {x.Value}]"))}");
    }

    Console.WriteLine(toolDetails.ToString());
    return await next.Invoke(context, cancellationToken);
}

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

[Description("Gets the current date and time in UTC format")]
static DateTime GetDateTimeUtc()
{
    return DateTime.UtcNow;
}
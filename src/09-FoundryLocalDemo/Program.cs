using System.ClientModel;
using Microsoft.Agents.AI;
using Microsoft.AI.Foundry.Local;
using Microsoft.Extensions.Logging.Abstractions;
using OpenAI;
using System.Diagnostics;
using OpenAI.Chat;
using Microsoft.Extensions.Logging;

string modelAlias = "phi-4";
Console.WriteLine($"Starting AI Model '{modelAlias}'. If not already started / caching this might take a while...");

var config = new Configuration
{
    AppName = "FoundryLocalDemo",
    Web = new Configuration.WebService { Urls = "http://127.0.0.1:0" }
};

using var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
var logger = loggerFactory.CreateLogger("FoundryLocal");
// Initialize the singleton instance with logging
await FoundryLocalManager.CreateAsync(config, logger);

//If you don't care about logging, you can use NullLogger.Instance instead to avoid the overhead of logging.

// Initialize the singleton instance without logging
//await FoundryLocalManager.CreateAsync(config, NullLogger.Instance);

var mgr = FoundryLocalManager.Instance;
// Get the model catalog
var catalog = await mgr.GetCatalogAsync();
// Get a model using an alias.
var model = await catalog.GetModelAsync(modelAlias) ?? throw new Exception($"Model '{modelAlias}' not found in catalog.");

// Use the CPU variant when the installed runtime does not include DirectML support.
var cpuVariant = model.Variants.FirstOrDefault(variant =>
    variant.Id.Contains("generic-cpu", StringComparison.OrdinalIgnoreCase));
if (cpuVariant != null)
{
    model.SelectVariant(cpuVariant);
}

// Download the model (the method skips download if already cached)
await model.DownloadAsync();
// Load the model
await model.LoadAsync();
//Start the web service hosting the model
await mgr.StartWebServiceAsync();

OpenAIClient client = new(new ApiKeyCredential("NO_API_KEY"), new OpenAIClientOptions
{
    Endpoint = new Uri(mgr.Urls!.First() + "/v1")
});
ChatClientAgent agent = client.GetChatClient(model.Id).AsAIAgent();


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

await mgr.StopWebServiceAsync();
await model.UnloadAsync();
mgr.Dispose();


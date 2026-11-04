using Microsoft.Agents.AI;
using OpenTelemetry;
using System.ClientModel;
using Azure.Monitor.OpenTelemetry.Exporter;
using OpenAI.Chat;
using OpenTelemetry.Trace;
using Microsoft.Extensions.Configuration;
using OpenAI;


IConfigurationRoot config = new ConfigurationBuilder().AddUserSecrets<Program>().Build();
string endpoint = config["endpoint"]!;
string apikey = config["apikey"]!;
string appinsightsConnectionString = "<enter Application Insights connection string>";

//Setup Telemetry
string sourceName = "agentframework-opentelemetry-demo";
var tracerProviderBuilder = Sdk.CreateTracerProviderBuilder()
    .AddSource(sourceName)
    .AddConsoleExporter();
if (!string.IsNullOrWhiteSpace(appinsightsConnectionString))
{
    tracerProviderBuilder.AddAzureMonitorTraceExporter(options => options.ConnectionString = appinsightsConnectionString);
}

using TracerProvider tracerProvider = tracerProviderBuilder.Build();

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

AIAgent agent = client
    .GetChatClient("gpt-5")
    .AsAIAgent(
        name: "MyLogAnalyticsAgent",
        instructions: "You are a knowledgeable assistant that helps answer questions. Always try to be helpful, precise and concise in your answers. If you don't know the answer, say you don't know instead of trying to make something up.")
    .AsBuilder()
    .UseOpenTelemetry(sourceName, options =>
    {
        options.EnableSensitiveData = true; //be careful with this in production, it can leak personally identifiable information. Use only if you are sure you won't log sensitive data, or if you have proper safeguards in place to protect the telemetry data
    })
    .Build();

AgentSession session = await agent.CreateSessionAsync();

AgentResponse response = await agent.RunAsync("Why is the sky blue?", session);
Console.WriteLine(response.Text);


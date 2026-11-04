using System.ClientModel;
using Microsoft.Agents.AI;
using Azure.Identity;
using Azure.AI.Projects;
using System.Diagnostics;
using Microsoft.Agents.AI.Foundry;
using Azure.AI.Projects.Agents;

AIProjectClient client = new (
        new Uri("<enter your Foundry project endpoint here>"), 
        new AzureCliCredential()
    );

// Define the agent you want to create. (Prompt Agent in this case)
ProjectsAgentVersionCreationOptions options = new(
    new DeclarativeAgentDefinition(model: "gpt-5-mini")
    {
        Instructions = "You are a helpful assistant.",
    }
);

ProjectsAgentVersion createdAgentVersion = 
    await client.AgentAdministrationClient.CreateAgentVersionAsync("MyAgent", options);

// Type is for evaluation purposes only and is subject to change or 
// removal in future updates. Suppress this diagnostic to proceed.
#pragma warning disable OPENAI001 
FoundryAgent existingAgent = client.AsAIAgent(createdAgentVersion);
#pragma warning restore OPENAI001 

Stopwatch stopwatch = Stopwatch.StartNew();

AgentResponse response = await existingAgent.RunAsync("Why is the sky blue?");

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

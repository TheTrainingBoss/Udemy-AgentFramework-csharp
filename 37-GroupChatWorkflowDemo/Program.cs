using Microsoft.Agents.AI;
using System.ClientModel;
using Microsoft.Extensions.Configuration;
using OpenAI.Chat;
using Microsoft.Extensions.AI;
using Microsoft.Agents.AI.Workflows;
using ChatMessage = Microsoft.Extensions.AI.ChatMessage;
using System.Text.Json;
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

ChatClient chatClient = client.GetChatClient("gpt-5");

//No supervisor needed for this pattern since the group chat manager will handle turn taking and ensure each agent responds in order
//ChatClientAgent supervisor = client.GetChatClient("gpt-5-mini").AsAIAgent(name: "Supervisor", instructions: "Determine what type of question was asked. Never answer yourself");

ChatClientAgent movieConnesseur = client.GetChatClient("gpt-5").AsAIAgent(name: "MovieConnesseur", instructions: "You are a Movie Connesseur");
ChatClientAgent musicConnesseur = client.GetChatClient("gpt-5").AsAIAgent(name: "MusicConnesseur", instructions: "You are a Music Connesseur");

while (true)
{
    List<ChatMessage> messages = [];

    //Currently RoundRobinGroupChatManager is the only implemented IGroupChatManager, but you could implement your own to achieve different group chat behaviors 
    // (e.g. priority-based turn taking, dynamic turn taking based on message content, etc.)
    Workflow workflow = AgentWorkflowBuilder.CreateGroupChatBuilderWith(agents => new RoundRobinGroupChatManager(agents) { MaximumIterationCount = 5 })
        .AddParticipants(movieConnesseur, musicConnesseur)
        .WithName("Art Round Robin Workflow")
        .WithDescription("A workflow where two artistic agents take turns responding in a round-robin fashion.")
        .Build();

    Console.WriteLine();
    Console.Write("> ");
    messages.Add(new(ChatRole.User, Console.ReadLine()!));
    messages.AddRange(await RunWorkflowAsync(workflow, messages));
}

static async Task<List<ChatMessage>> RunWorkflowAsync(Workflow workflow, List<ChatMessage> messages)
{
    string? lastExecutorId = null;

    StreamingRun run = await InProcessExecution.RunStreamingAsync(workflow, messages);
    await run.TrySendMessageAsync(new TurnToken(emitEvents: true));
    await foreach (WorkflowEvent @event in run.WatchStreamAsync())
    {
        switch (@event)
        {
            case AgentResponseUpdateEvent e:
            {
                if (e.ExecutorId != lastExecutorId)
                {
                    lastExecutorId = e.ExecutorId;
                    Console.WriteLine();
                    Console.WriteLine(e.Update.AuthorName ?? e.ExecutorId);
                }

                Console.Write(e.Update.Text);
                if (e.Update.Contents.OfType<FunctionCallContent>().FirstOrDefault() is FunctionCallContent call)
                {
                    Console.WriteLine();
                    Console.WriteLine($"Call '{call.Name}' with arguments: {JsonSerializer.Serialize(call.Arguments)}]");
                }

                break;
            }
            case WorkflowOutputEvent output:
                return output.As<List<ChatMessage>>()!;
            case ExecutorFailedEvent failedEvent:
                if (failedEvent.Data is Exception ex)
                {
                    Console.WriteLine($"Error in agent {failedEvent.ExecutorId}: " + ex);
                }

                break;
        }
    }

    return [];
}

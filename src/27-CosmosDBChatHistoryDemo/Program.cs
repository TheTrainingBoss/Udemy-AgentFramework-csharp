using Microsoft.Agents;
using Microsoft.Agents.AI;
using Microsoft.Extensions.Configuration;
using System.ClientModel;
using Microsoft.Azure.Cosmos;
using Microsoft.Agents.AI.Compaction;
using Microsoft.Extensions.AI;
using OpenAI;
using OpenAI.Chat;

IConfigurationRoot config = new ConfigurationBuilder().AddUserSecrets<Program>().Build();
string endpoint = config["endpoint"]!;
string apikey = config["apikey"]!;
string cosmosdbEndpoint = config["cosmosdbEndpoint"]!;
string cosmosdbKey = config["cosmosdbKey"]!;

var cosmosClient = new CosmosClient(cosmosdbEndpoint, cosmosdbKey);

var database = cosmosClient.GetDatabase("<enter the database name>"); 
var container = database.GetContainer("<enter the container name>");

string conversationId = "LinoConversation"; //This is where you will have to decide on the conversationId strategy that best fits your scenario, it can be a fixed value for single conversation scenarios or something more dynamic like a session id or a combination of tenantId+userId for multi-tenant/multi-user scenarios. The important thing is that this value is used consistently across calls so that the chat history can be retrieved correctly.

var historyProvider = new CosmosChatHistoryProvider(
    cosmosClient: cosmosClient,
    databaseId: database.Id,
    containerId: container.Id,
    stateInitializer: _ => new CosmosChatHistoryProvider.State(
        conversationId: conversationId,
        tenantId: "default-tenant",
        userId: "default-user"
    ),
    options: new CosmosChatHistoryProviderOptions { OwnsClient = false }


    //In case the partition key order in the container is different from the default order expected by the provider, you can use the PartitionKeyFactory to build the key correctly.
    // options: new CosmosChatHistoryProviderOptions
    // {
    //     OwnsClient = false,
    //     // The container's hierarchical partition key paths are /conversationId, /tenantId, /userId (in that order),
    //     // while the provider's default hierarchical order is tenantId -> userId -> conversationId.
    //     // Build the key in the container's order so writes/batches match.
    //     PartitionKeyFactory = state => new PartitionKeyBuilder()
    //         .Add(state.ConversationId)
    //         .Add(state.TenantId)
    //         .Add(state.UserId)
    //         .Build()
    // }
);

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

#pragma warning disable MAAI001
var compactionStrategy = new SummarizationCompactionStrategy(
    chatClient: client.GetChatClient("gpt-5-mini").AsIChatClient(),
    trigger: CompactionTriggers.MessagesExceed(8)
);
var compactionProvider = new CompactionProvider(compactionStrategy);
#pragma warning restore MAAI001

var agentOptions = new ChatClientAgentOptions();
agentOptions.ChatHistoryProvider = historyProvider;
agentOptions.AIContextProviders = [compactionProvider];

string question =  "ok can you give the top 5 only";
//"What are the top 10 Movies according to IMDB?"; //"ok can you give the top 5 only";

AIAgent agent = client
    .GetChatClient("gpt-5")
    .AsAIAgent(agentOptions);

AgentSession session = await agent.CreateSessionAsync();

AgentResponse response = await agent.RunAsync(question, session);
Console.WriteLine(response);

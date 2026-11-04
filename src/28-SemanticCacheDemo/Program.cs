using Microsoft.Agents.AI;
using Microsoft.Extensions.Configuration;
using System.ClientModel;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.AI;
using OpenAI.Chat;
using OpenAI;

IConfigurationRoot config = new ConfigurationBuilder().AddUserSecrets<Program>().Build();
string endpoint = config["endpoint"]!;
string apikey = config["apikey"]!;
string cosmosEndpoint = config["cosmosdbEndpoint"]!;
string cosmosKey = config["cosmosdbKey"]!;

var cosmosClient = new CosmosClient(cosmosEndpoint, cosmosKey);

var database = cosmosClient.GetDatabase("<enter the database name>");
var container = database.GetContainer("<enter the container name>");

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

IEmbeddingGenerator<string, Embedding<float>> embeddingGenerator = client
            .GetEmbeddingClient("text-embedding-3-small")
            .AsIEmbeddingGenerator();

string question =   "What are the top 10 Movies according to IMDB?";
var embedding = await embeddingGenerator.GenerateAsync(question);

AIAgent agent = client
    .GetChatClient("gpt-5")
    .AsAIAgent();

AgentSession session = await agent.CreateSessionAsync();

// Step 1: Check semantic cache
var cached = await FindSimilarAsync(container, embedding.Vector.ToArray());

if (cached != null)
{
    Console.WriteLine("Cache HIT, lucky you! no money spent on this one :)");
    Console.WriteLine(cached.answer);
    return;
}

Console.WriteLine("Cache MISS → Calling LLM");

// Step 2: Call agent / LLM
var response = await agent.RunAsync(question, session);

// Step 3: Store in cache
var item = new SemanticCacheItem
{
    id = Guid.NewGuid().ToString(),
    pk = "cache",
    question = question,
    embedding = embedding.Vector.ToArray(),
    answer = response.Text
};

await container.CreateItemAsync(item);

Console.WriteLine(response.Text);

async Task<SemanticCacheItem?> FindSimilarAsync(Container container,float[] embedding)
{
    var query = new QueryDefinition(@"
        SELECT TOP 1 c.id, c.question, c.answer, VectorDistance(c.embedding, @embedding) AS similarity
        FROM c
        WHERE c.pk = @pk AND VectorDistance(c.embedding, @embedding) > 0.3
        ORDER BY VectorDistance(c.embedding, @embedding)
    ")
    .WithParameter("@embedding", embedding)
    .WithParameter("@pk", "cache");


    using var iterator = container.GetItemQueryIterator<SemanticCacheItem>(query);

    while (iterator.HasMoreResults)
    {
        foreach (var item in await iterator.ReadNextAsync())
        {
            return item;
        }
    }
    return null;
}

public class SemanticCacheItem
{
    public required string id { get; set; }
    public string pk { get; set; } = "cache";
    public required string question { get; set; }
    public required float[] embedding { get; set; }
    public required string answer { get; set; }
}

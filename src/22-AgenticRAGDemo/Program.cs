using System.ClientModel;
using OpenAI;using OpenAI.Chat;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.VectorData;
using CommunityToolkit.VectorData.AzureAISearch;
using Azure.Search.Documents.Indexes;
using Azure;
using System.Text;


IConfigurationRoot config = new ConfigurationBuilder().AddUserSecrets<Program>().Build();
string endpoint = config["endpoint"]!;
string apikey = config["apikey"]!;

OpenAIClient client = new OpenAIClient(new ApiKeyCredential(apikey), new OpenAIClientOptions
{
    Endpoint = new Uri(endpoint + "/openai/v1/"),
    NetworkTimeout = TimeSpan.FromSeconds(60)
});

// If you don't want to use the APIKey, you can use Bearer token authentication instead
//BearerTokenPolicy policy = new(new DefaultAzureCredential(), "https://ai.azure.com/.default");
// OpenAIClient client = new OpenAIClient(policy, new OpenAIClientOptions
// {
//     Endpoint = new Uri(endpoint + "/openai/v1/")
//     NetworkTimeout = TimeSpan.FromSeconds(60)
// });

IEmbeddingGenerator<string, Embedding<float>> embeddingGenerator = client
            .GetEmbeddingClient("text-embedding-3-small")
            .AsIEmbeddingGenerator();

//Using Azure AI Search
VectorStore vectorStoreFromAzureAiSearch = new AzureAISearchVectorStore(
    new SearchIndexClient(
        new Uri("https://<enter your search service name>.search.windows.net"), // It is best practice to put this in a secret manager or environment variable
        new AzureKeyCredential("hhHZyE7dJ35pFHz...<enter your search service key>")), // It is best practice to put this in a secret manager or environment variable
    new AzureAISearchVectorStoreOptions {
        EmbeddingGenerator = embeddingGenerator
    });

VectorStoreCollection<Guid, KnowledgeBaseVectorRecord> vectorStoreCollection = vectorStoreFromAzureAiSearch.GetCollection<Guid, KnowledgeBaseVectorRecord>("agindex");

//Create out Search Tool
SearchTool searchTool = new SearchTool(vectorStoreCollection);

//Create Agent
ChatClientAgent agent = client
    .GetChatClient("gpt-5")
    .AsAIAgent(instructions: "You are an expert in the company's Internal Knowledge Base (use the 'search_knowledge' tool)",
               tools: [AIFunctionFactory.Create(searchTool.Search, "search_knowledge")]);

AgentSession session = await agent.CreateSessionAsync();;

while (true)
{
    Console.Write("Your question: ");
    string input = Console.ReadLine() ?? "";

    AgentResponse response = await agent.RunAsync(input, session);
    {
        Console.WriteLine(response);
    }
}

public record KnowledgeBaseEntry(string Question, string Answer);

public class KnowledgeBaseVectorRecord
{
    [VectorStoreKey]
    public required Guid Id { get; set; }

    [VectorStoreData]
    public required string Question { get; set; }

    [VectorStoreData]
    public required string Answer { get; set; }

    [VectorStoreVector(1536)]
    public string Vector => $"Q: {Question} - A: {Answer}";
}

class SearchTool(VectorStoreCollection<Guid, KnowledgeBaseVectorRecord> vectorStoreCollection)
{
    public async Task<string> Search(string input)
    {
        StringBuilder mostSimilarKnowledge = new StringBuilder();
        await foreach (VectorSearchResult<KnowledgeBaseVectorRecord> searchResult in vectorStoreCollection.SearchAsync(input, 3))
        {
            string searchResultAsQAndA = $"Q: {searchResult.Record.Question} - A: {searchResult.Record.Answer}";
            Console.WriteLine($"- Search result [Score: {searchResult.Score}] {searchResultAsQAndA}");
            mostSimilarKnowledge.AppendLine(searchResultAsQAndA);
        }

        Console.WriteLine();

        return mostSimilarKnowledge.ToString();
    }
}

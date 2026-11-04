using System.ClientModel;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.VectorData;
using CommunityToolkit.VectorData.AzureAISearch;
using Azure.Search.Documents.Indexes;
using Azure;
using OpenAI;

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

List<KnowledgeBaseEntry> knowledgeBase =
[
    new("What is the WI-FI Password at the Office?", "The Password is 'Lino42'"),
    new("Is Christmas Eve a full or half day off", "It is a full day off"),
    new("How do I register vacation?", "Go to the internal portal and under Vacation Registration (top right), enter your request. Your manager will be notified and will approve/reject the request"),
    new("What do I need to do if I'm sick?", "Inform you manager, and if you have any meetings remember to tell the affected colleagues/customers"),
    new("Where is the employee handbook?", "It is located [here](https://thetrainingboss.com/hr/handbook.pdf)"),
    new("Who is in charge of support?", "Lino Tadros is in charge of support. His email is lino@thetrainingboss.com"),
    new("I can't log in to my office account", "Take hold of Jessica. She can reset your password"),
    new("When using the CRM System if get error 'index out of bounds'", "That is a known issue. Log out and back in to get it working again. The CRM team have been informed and status of ticket can be seen here: https://www.crm.com/tickets/12354"),
    new("What is the policy on buying books and online courses?", "Any training material under 20$ you can just buy.. anything higher need an approval from Lino Tadros"),
    new("Is there a bounty for find candidates for an open job position?", "Yes. $1000 if we hire them... Have them send the application to jobs@thetrainingboss.com")
];

IEmbeddingGenerator<string, Embedding<float>> embeddingGenerator = client
            .GetEmbeddingClient("text-embedding-3-small")
            .AsIEmbeddingGenerator();

//Using Azure AI Search
VectorStore vectorStoreFromAzureAiSearch = new AzureAISearchVectorStore(
    new SearchIndexClient(
        new Uri("https://agaisearch.search.windows.net"), // It is best practice to put this in a secret manager or environment variable
        new AzureKeyCredential("hhHZyE7dJ35pFHznaY3QfqptrL5IaMOjgo1ugGemhhAzSeCNkHmc")), // It is best practice to put this in a secret manager or environment variable
    new AzureAISearchVectorStoreOptions {
        EmbeddingGenerator = embeddingGenerator
    });

VectorStoreCollection<Guid, KnowledgeBaseVectorRecord> vectorStoreCollection = vectorStoreFromAzureAiSearch.GetCollection<Guid, KnowledgeBaseVectorRecord>("agindex");

await vectorStoreCollection.EnsureCollectionExistsAsync();

Console.Write("Import Data? (Y/N): ");
ConsoleKeyInfo key = Console.ReadKey();
if (key.Key == ConsoleKey.Y)
{
    //Delete old table
    await vectorStoreCollection.EnsureCollectionDeletedAsync();

    //Create anew
    await vectorStoreCollection.EnsureCollectionExistsAsync();
    Console.Clear();
    int counter = 0;
    foreach (KnowledgeBaseEntry entry in knowledgeBase)
    {
        counter++;
        Console.Write($"\rEmbedding Data: {counter}/{knowledgeBase.Count}");
        await vectorStoreCollection.UpsertAsync(new KnowledgeBaseVectorRecord
        {
            Id = Guid.NewGuid(),
            Question = entry.Question,
            Answer = entry.Answer,
        });
    }

    Console.WriteLine();
    Console.WriteLine("\rEmbedding complete...");
}

Console.WriteLine();

Console.WriteLine("Listing all data in the vector-store");
await foreach (KnowledgeBaseVectorRecord existingRecord in vectorStoreCollection.GetAsync(record => record.Id != Guid.Empty, int.MaxValue))
{
    Console.WriteLine($"Q: {existingRecord.Question} - A: {existingRecord.Answer} - Vector: {existingRecord.Vector}");
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

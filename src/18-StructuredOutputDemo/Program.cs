
using Microsoft.Agents.AI;
using System.ClientModel;
using Microsoft.Extensions.Configuration;
using OpenAI;
using OpenAI.Chat;
using StructuredOutput.Models;
using Microsoft.Extensions.AI;

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

string question = "What are the top 10 rated Movies according to IMDB?";

//Without Structured Output
AIAgent agent1 = client
    .GetChatClient("gpt-5-mini")
    .AsAIAgent(instructions: "You are an expert in IMDB Lists");

AgentResponse response1 = await agent1.RunAsync(question);
Console.WriteLine(response1); //This response is not guaranteed to have a structure format; hence the need for this Structured Output feature


//With Structured Output
ChatClientAgent agent2 = client //<--- Notice that this is not an AIAgent but have it as baseclass!
    .GetChatClient("gpt-5")
    .AsAIAgent(instructions: "You are an expert in IMDB Lists");

AgentResponse<List<Movie>> response2 = await agent2.RunAsync<List<Movie>>(question);

List<Movie> movies = response2.Result;

int counter = 1;
foreach (Movie movie in movies)
{
    Console.WriteLine($"{counter}: {movie.Title} ({movie.YearOfRelease}) - Genre: {movie.Genre} - Director: {movie.Director} - IMDB Score: {movie.ImdbScore}");
    counter++;
}


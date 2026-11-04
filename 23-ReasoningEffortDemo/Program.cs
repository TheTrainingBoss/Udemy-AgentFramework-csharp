using System.ClientModel;
using OpenAI.Chat;
using OpenAI.Responses;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
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

// This is set to the default of Medium for reasonning efforts
ChatClientAgent agent = client
        .GetChatClient("gpt-5")
        .AsAIAgent();

AgentResponse response = await agent.RunAsync("Who is Bill Gates? and where was he born?");
Console.WriteLine(response);
Console.WriteLine("Input Token Count: " + response.Usage?.InputTokenCount);
Console.WriteLine("Output Token Count: " + response.Usage?.OutputTokenCount);
Console.WriteLine("Reasoning Token Count: " + response.Usage?.ReasoningTokenCount);

//Let's take control of the reasonning efforts

ChatClientAgent agentwithreasoningeffort = client
        .GetChatClient("gpt-5")
        .AsAIAgent(
            //reasoningEffortLevel: ReasoningEffortLevel.Low  //Yeah no, not that easy :) 
            options: new ChatClientAgentOptions
            {
                ChatOptions = new ChatOptions
                {
#pragma warning disable OPENAI001
                    RawRepresentationFactory = _ => new ChatCompletionOptions
                    {
                        ReasoningEffortLevel = ChatReasoningEffortLevel.High,                         
                    }
#pragma warning restore OPENAI001
                }
            }
        );

AgentResponse responseWithReasoningEffort = await agentwithreasoningeffort.RunAsync("Who is Bill Gates? and where was he born?");
Console.WriteLine(responseWithReasoningEffort);
Console.WriteLine("Input Token Count: " + responseWithReasoningEffort.Usage?.InputTokenCount);
Console.WriteLine("Output Token Count: " + responseWithReasoningEffort.Usage?.OutputTokenCount);
Console.WriteLine("Reasoning Token Count: " + responseWithReasoningEffort.Usage?.ReasoningTokenCount);

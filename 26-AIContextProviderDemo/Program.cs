using Microsoft.Agents.AI;
using System.ClientModel;
using Microsoft.Extensions.Configuration;
using OpenAI;
using OpenAI.Chat;
using Microsoft.Extensions.AI;

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

string question = "What are the top 10 Movies according to IMDB?";

AIAgent agent = client
    .GetChatClient("gpt-5")
    .AsAIAgent( new ChatClientAgentOptions
    {        
        ChatOptions = new ChatOptions
        {
            Instructions = "Prefix all messages with Hi Lino, and suffix all messages with Cheers, Lino.",  
        },
        AIContextProviders = [
            new MyAIContextProvider()
        ]
    });

AgentResponse response = await agent.RunAsync(question);
Console.WriteLine(response);


class MyAIContextProvider : AIContextProvider
{
    /* Order of an AIContextProvider when an 'RunAsync' method is being executed
     *
     * - InvokingCoreAsync --> ProvideAIContextAsync
     * - LLM Call
     * - InvokedCoreAsync --> StoreAIContextAsync
     *
     */

    //Pre LLM Call (Core)
    protected override ValueTask<AIContext> InvokingCoreAsync(InvokingContext context, CancellationToken cancellationToken = new CancellationToken())
    {
        //Only call this if you also want to be in charge of AIContext merging [You will most likely not want to override this]
        return base.InvokingCoreAsync(context, cancellationToken);
    }

    //Pre LLM Call (Enrichment)
    protected override ValueTask<AIContext> ProvideAIContextAsync(InvokingContext context, CancellationToken cancellationToken = new CancellationToken())
    {
        /* Use this to do the following:
         * - Inject additional instructions (for this one LLM Call)
         * - Inject additional tools (for this one LLM Call)
         * - Inject additional message (that unlike the two above will become part of chat-history)
         */
        
        return ValueTask.FromResult(new AIContext
        {
            Instructions = "Speak like Mickey Mouse",
            Tools = [],
        });
    }

    //Post LLM Call (Core)
    protected override ValueTask InvokedCoreAsync(InvokedContext context, CancellationToken cancellationToken = new CancellationToken())
    {
        //Only call this if you also want to be in charge of Error Handling [You will most likely not want to override this]
        return base.InvokedCoreAsync(context, cancellationToken);
    }

    //Post LLM Call (Leverage LLM Call Result for something)
    protected override async ValueTask StoreAIContextAsync(InvokedContext context, CancellationToken cancellationToken = new CancellationToken())
    {
        /* Use this to do the following:
         * - Extract information out of the response in a centralized structured manner (aka example storing memory)
         * - Deal with exceptions
         */

        await Task.CompletedTask;
    }
}


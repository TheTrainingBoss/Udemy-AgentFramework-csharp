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

var agent = chatClient.AsAIAgent(instructions: "You are the judge in a guessing game where it is about guessing animals. " +
                                               "Each hint should only give one fact");

List<string> animals = ["Wolf", "Eagle", "Tiger", "Dolphin", "Elephant", "Grizzly Bear", "Mouse", "Dog", "Shark", "Panda"];

var animalToGuess = animals[Random.Shared.Next(animals.Count)] ;
var initialHintResponse = await agent.RunAsync<string>($"Make a vague hint for the animal: '{animalToGuess}'");

RequestPort requestPort = RequestPort.Create<FeedbackToUser, string>("GuessAnimal");
var evaluateAndHintExecutor = new EvaluateAndHintExecutor(agent, animalToGuess);
var workflow = new WorkflowBuilder(requestPort)
    .AddEdge(requestPort, evaluateAndHintExecutor)
    .AddEdge(evaluateAndHintExecutor, requestPort)
    .WithOutputFrom(evaluateAndHintExecutor)
    .Build();

var initialFeedback = new FeedbackToUser(initialHintResponse.Result, true);
await using StreamingRun handle = await InProcessExecution.RunStreamingAsync(workflow, initialFeedback);

await foreach (WorkflowEvent evt in handle.WatchStreamAsync())
{
    switch (evt)
    {
        case RequestInfoEvent requestInputEvt:
            var externalRequest = requestInputEvt.Request;
            if (externalRequest.IsDataOfType<FeedbackToUser>())
            {
                FeedbackToUser feedbackToUser = externalRequest.Data.As<FeedbackToUser>()!;
                Console.WriteLine(feedbackToUser.Init ? 
                    "Guess what animal I'm thinking of" : 
                    "That is not the animal I'm thinking of");
                Console.WriteLine($"Hint: {feedbackToUser.Hint}");
                Console.WriteLine(new string('-', 50));
                Console.Write("Your Guess: ");
                var input = Console.ReadLine();
                var externalResponse = externalRequest.CreateResponse(input);
                await handle.SendResponseAsync(externalResponse);
                break;
            }
            throw new NotSupportedException($"Request {externalRequest.PortInfo.RequestType} is not supported");
        case WorkflowOutputEvent outputEvt:
            Console.WriteLine(outputEvt.Data!.ToString()!);
            return;
    }
}

[YieldsOutput(typeof(string))]
[SendsMessage(typeof(FeedbackToUser))]
class EvaluateAndHintExecutor(ChatClientAgent agent, string animalToGuess) : Executor<string>("Evaluator and Hint-giver")
{
    private int _numberOfTries;
    private readonly IList<string> _hintsGiven = [];

    public override async ValueTask HandleAsync(
        string message,
        IWorkflowContext context,
        CancellationToken cancellationToken = default)
    {
        _numberOfTries++;
        var input = $"Is this the right answer for guessing the animal is '{animalToGuess}'? (allow for spelling errors of the animal): {message}";
        AgentResponse<bool> isRightAnswerResponse = await agent.RunAsync<bool>(input, cancellationToken: cancellationToken);
        if (isRightAnswerResponse.Result)
        {
            //End the Game
            await context.YieldOutputAsync($"You guessed it. " +
                                           $"The answer is indeed a {animalToGuess}. " +
                                           $"Tries used: {_numberOfTries}", cancellationToken);
        }
        else
        {
            //Not correct answer: Let's generate a new Hint
            var newHintPrompt = $"Generate a hint for a child to guess the animal '{animalToGuess}'. " +
                                $"Hints already given: {string.Join(" | ", _hintsGiven)} " +
                                $"so make the new hint unique and do not repeat the same hint parts";
            AgentResponse<string> hintResponse = await agent.RunAsync<string>(newHintPrompt, cancellationToken: cancellationToken);
            var newHint = hintResponse.Result;
            _hintsGiven.Add(newHint);
            await context.SendMessageAsync(new FeedbackToUser(newHint), cancellationToken);
        }
    }
}

record FeedbackToUser(string Hint, bool Init = false);

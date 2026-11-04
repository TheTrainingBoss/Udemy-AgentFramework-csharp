using Microsoft.Agents.AI;
using Microsoft.Extensions.Configuration;
using System.ClientModel;
using Microsoft.Extensions.AI;
using OpenAI.Chat;
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


#pragma warning disable MAAI001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
var path = Path.Combine(Directory.GetCurrentDirectory(), "AgentSkills");
Console.WriteLine($"Loading skills from: {path}");
var skillsProvider = new AgentSkillsProvider(path);
#pragma warning restore MAAI001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

#pragma warning disable OPENAI001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
AIAgent agent = client.GetChatClient("gpt-5-deploy")
    .AsAIAgent(new ChatClientAgentOptions
    {
        Name = "Mickey Mouse",
        ChatOptions = new()
        {
            Instructions = "You are a helpful assistant. Answer questions and complete tasks for the user to the best of your ability. If you don't know the answer to a question, say you don't know.",
        },
        AIContextProviders = [skillsProvider],
    });
#pragma warning restore OPENAI001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

AgentSession session = await agent.CreateSessionAsync();
AgentResponse response = await agent.RunAsync("Why is the sky blue? use the skill available to you", session);

// The AgentSkillsProvider exposes skill-loading as a tool call that requires approval.
// Without approving it (on the same session), RunAsync stops after the approval request and returns no text.
while (response.Messages.SelectMany(m => m.Contents).OfType<ToolApprovalRequestContent>().Any())
{
    var approvals = response.Messages
        .SelectMany(m => m.Contents)
        .OfType<ToolApprovalRequestContent>()
        .Select(request => (AIContent)request.CreateResponse(approved: true))
        .ToList();

    response = await agent.RunAsync(new Microsoft.Extensions.AI.ChatMessage(ChatRole.User, approvals), session);
}

Console.WriteLine($"Agent: {response.Text}");


using Microsoft.Agents.AI;
using System.ClientModel;
using Microsoft.Extensions.Configuration;
using OpenAI;
using OpenAI.Chat;
using Microsoft.Extensions.AI;
using Microsoft.Agents.AI.Workflows;
using ChatMessage = Microsoft.Extensions.AI.ChatMessage;

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

ChatClientAgent legalAgent = chatClient.AsAIAgent(name: "LegalAgent", instructions: "You are a legal agent that need to evaluate if a text is legal (use max 250 chars)");
ChatClientAgent spellingErrorAgent = chatClient.AsAIAgent(name: "SpellingErrorAgent", instructions: "You are a spelling expert (use max 250 chars)");

Workflow workflow = AgentWorkflowBuilder.BuildConcurrent([legalAgent, spellingErrorAgent]);

string patentdoc = """
          This Software Patent Agreement (“Agreement”) is entered into as of the Effective Date by and 
          between the Patent Holder and the Licensee for the purpose of defining the terms under which 
          the Licensee may utilize certain proprietary software technologies covered by one or more issued 
          or pending patents owned or controlled by the Patent Holder. The Patent Holder hereby grants to 
          the Licensee a limited, non-exclusive, non-transferable, and revocable license to make, use, 
          reproduce, and distribute implementations of the patented software methods solely within the 
          scope of the Licensee’s internal business operations or as otherwise expressly authorized in writing. 
          All rights not expressly granted herein remain reserved by the Patent Holder, including without 
          limitation the right to sublicense, enforce, or otherwise commercially exploit the patented technology.
          The Licensee agrees to comply with all applicable intellectual property laws and acknowledges that any 
          unauthorized reproduction, modification, reverse engineering, or distribution of the patented software 
          beyond the scope of this Agrement shall constitute a material breach and may result in immediate 
          termination of the license rights granted herein. In the event of infringement or suspected misuse 
          of the patented technology, the Patent Holder reserves the right to pursue all remedies available 
          at law or in equity, including injunctive relief and recovery of damages. This Agreement shall be 
          governed by and construed in accordance with the applicable jurisdiction specified by the Patent 
          Holder, and any disputes arising hereunder shall be resolved through binding arbitration or 
          a court of competent jurisdiction as mutually agreed by the parties.  
          """;

var messages = new List<ChatMessage> { new(ChatRole.User, patentdoc) };

await using StreamingRun run = await InProcessExecution.RunStreamingAsync(workflow, messages);
await run.TrySendMessageAsync(new TurnToken(emitEvents: true));

List<ChatMessage> result = [];
await foreach (WorkflowEvent evt in run.WatchStreamAsync())
{
    if (evt is WorkflowOutputEvent output)
    {
        result = output.As<List<ChatMessage>>()!;
    }
}

foreach (var message in result.Where(x => x.Role != ChatRole.User))
{
    Console.WriteLine(message.AuthorName ?? "Unknown");
    Console.WriteLine($"{message.Text}");
}
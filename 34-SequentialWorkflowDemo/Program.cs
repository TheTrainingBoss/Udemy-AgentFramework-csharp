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

ChatClientAgent summaryAgent = chatClient.AsAIAgent(name: "SummaryAgent", instructions: "Summarize the text you are given to max 25 words");
ChatClientAgent translationAgent = chatClient.AsAIAgent(name: "TranslationAgent", instructions: "Given some text, Translate it to French (you need to translate the summary and not the original text)");

Workflow workflow = AgentWorkflowBuilder.BuildSequential(summaryAgent, translationAgent);

// I could have built the workflow by adding executors and connecting them
// var workflow = new WorkflowBuilder(summaryAgent)
//     .AddEdge(summaryAgent, translationAgent)
//     .Build();

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
          beyond the scope of this Agreement shall constitute a material breach and may result in immediate 
          termination of the license rights granted herein. In the event of infringement or suspected misuse 
          of the patented technology, the Patent Holder reserves the right to pursue all remedies available 
          at law or in equity, including injunctive relief and recovery of damages. This Agreement shall be 
          governed by and construed in accordance with the applicable jurisdiction specified by the Patent 
          Holder, and any disputes arising hereunder shall be resolved through binding arbitration or 
          a court of competent jurisdiction as mutually agreed by the parties.  
          """;

var messages = new List<ChatMessage> { new(ChatRole.User, patentdoc) };

//Starts the sequential workflow in-process as a streaming run, passing 
//in the workflow definition and the initial messages. 
//Returns a StreamingRun handle you can use to interact with the execution.
StreamingRun run = await InProcessExecution.RunStreamingAsync(workflow, messages);

// Must send the turn token to trigger the agents.
// The agents are wrapped as executors. When they receive messages,
// they will cache the messages and only start processing when they receive a TurnToken.
await run.TrySendMessageAsync(new TurnToken(emitEvents: true));

List<ChatMessage> result = [];

//Asynchronously iterates over the stream of workflow events as they 
//are emitted in real time. ConfigureAwait(false) avoids capturing 
//the synchronization context (common in non-UI code)
await foreach (WorkflowEvent evt in run.WatchStreamAsync().ConfigureAwait(false))
{
    // Checks if the current event is a WorkflowOutputEvent — the final output 
    // produced by the workflow after all steps complete.
    if (evt is WorkflowOutputEvent output)
    {
       //Deserializes the output event's payload into a List<ChatMessage>, replacing the empty list. 
       result = output.As<List<ChatMessage>>()!;
    }
}

foreach (ChatMessage message in result.Where(x => x.Role != ChatRole.User))
{
    Console.WriteLine($"{message.Text}");
}

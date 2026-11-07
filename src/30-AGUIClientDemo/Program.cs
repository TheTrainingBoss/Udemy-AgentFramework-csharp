using Microsoft.Agents.AI;
using AGUI.Client;
using Microsoft.Extensions.AI;
using System.Text;

Console.Clear();
HttpClient httpClient = new HttpClient();
const string serverUrl = "http://localhost:5000";
ConsoleColor textColor = ConsoleColor.White;
AGUIChatClient chatClient = new AGUIChatClient(new AGUIChatClientOptions(httpClient, serverUrl));
AIAgent agent = chatClient.AsAIAgent(
    instructions: """
                You are a helpful assistant that can also change the color of the text in the console. 
                Use the 'change_color' tool to change the color when needed to enhance user experience.
                """,
    tools: [AIFunctionFactory.Create(ChangeColor, name: "change_color")]
);

List<ChatMessage> messages = []; 

while (true)
{
    Console.Write("> ");
    string message = Console.ReadLine() ?? string.Empty;
    if (message == string.Empty)
    {
        continue;
    }

    messages.Add(new ChatMessage(ChatRole.User, message));

    List<AgentResponseUpdate> updates = [];
    await foreach (AgentResponseUpdate update in agent.RunStreamingAsync(messages))
    {
        updates.Add(update);
        foreach (AIContent content in update.Contents)
        {
            switch (content)
            {
                case TextContent textContent:
                    Console.Write(textContent.Text);
                    break;

                case FunctionCallContent functionCallContent:
                    StringBuilder toolCallDetails = new();
                    toolCallDetails.Append($"[Tool Call: {functionCallContent.Name}");
                    if (functionCallContent.Arguments?.Any() ?? false)
                    {
                        toolCallDetails.Append($" (Args: {string.Join(",", functionCallContent.Arguments.Select(x => $"[{x.Key} = {x.Value}]"))}");
                    }

                    toolCallDetails.Append("]");
                    Console.WriteLine(toolCallDetails);
                    Console.ForegroundColor = textColor;
                    break;
                case FunctionResultContent functionResultContent:
                    bool isError = functionResultContent.Exception != null;
                    Console.WriteLine(isError ? $"[Tool Error: {functionResultContent.Exception}]" : $"[Tool Result: {functionResultContent.Result}]");
                    break;

                case ErrorContent errorContent:
                    Console.WriteLine($"[Error: {errorContent.Message}]");
                    break;
            }
        }
    }

    AgentResponse fullResponse = updates.ToAgentResponse();
    messages.AddRange(fullResponse.Messages);

    Console.WriteLine();
    Console.WriteLine();
}

void ChangeColor(ConsoleColor color)
{
    textColor = color;
    Console.ForegroundColor = textColor;
}

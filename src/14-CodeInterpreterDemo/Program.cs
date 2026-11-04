using Microsoft.Agents.AI;
using Microsoft.Extensions.Configuration;
using System.ClientModel;
using Microsoft.Extensions.AI;
using OpenAI.Chat;
using OpenAI.Responses;
using OpenAI.Containers;
using System.Diagnostics;
using ChatMessage = Microsoft.Extensions.AI.ChatMessage;
using OpenAI;

IConfigurationRoot config = new ConfigurationBuilder().AddUserSecrets<Program>().Build();
string endpoint = config["endpoint"]!;
string apikey = config["apikey"]!;
string openaikey = config["openaikey"]!;

OpenAIClient client = new OpenAIClient(new ApiKeyCredential(openaikey));

#pragma warning disable OPENAI001
    ChatClientAgent agent = client
            .GetResponsesClient() 
            .AsAIAgent(model: "gpt-5", options:   
                new ChatClientAgentOptions
                {
                    Name = "ChartCodeBot",
                    Description = "Chart Code Bot",
                    ChatOptions = new ChatOptions
                    {
                        Instructions = "You can make charts using your Code Interpreter tool",
                        Tools =
                        [
                            new HostedCodeInterpreterTool()
                        ]
                    }
                }
            );

Stopwatch stopwatch = Stopwatch.StartNew();

AgentResponse response = await agent.RunAsync("Create a pie chart showing the 5 most populous countries in the world");
long milliseconds = stopwatch.ElapsedMilliseconds;

Console.WriteLine(response);

foreach (ChatMessage message in response.Messages)
{
    foreach (AIContent content in message.Contents)
    {
        foreach (AIAnnotation annotation in content.Annotations ?? [])
        {
            if (annotation.RawRepresentation is ContainerFileCitationMessageAnnotation containerFileCitation)
            {
                ContainerClient containerClient = client.GetContainerClient();
                ClientResult<BinaryData> fileContent = await containerClient.DownloadContainerFileAsync(containerFileCitation.ContainerId, containerFileCitation.FileId);
                string path = Path.Combine(Directory.GetCurrentDirectory(), containerFileCitation.Filename);
                await File.WriteAllBytesAsync(path, fileContent.Value.ToArray());
                Console.WriteLine($"Attempting to open: '{path}'");
                Console.WriteLine($"File exists: {File.Exists(path)}");
                await Task.Factory.StartNew(() =>
                {
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = path,
                        UseShellExecute = true
                    });
                });
            }
        }
    }
}
#pragma warning restore OPENAI001

if (response.Usage != null)
{
    Console.ForegroundColor = ConsoleColor.Magenta;
    Console.WriteLine();
    Console.WriteLine($"Input Tokens: {response.Usage.InputTokenCount}");
    Console.WriteLine($"Cached Tokens: {response.Usage.CachedInputTokenCount ?? 0}");
    Console.WriteLine($"Output Tokens: {response.Usage.OutputTokenCount}");
    Console.WriteLine($"Reasoning Tokens: {response.Usage.ReasoningTokenCount ?? 0}");
    Console.WriteLine($"Total Tokens: {response.Usage.TotalTokenCount}");
    Console.ResetColor();
}

Console.ForegroundColor = ConsoleColor.Green;
Console.WriteLine($"Time spent: {milliseconds} ms");
Console.ResetColor();


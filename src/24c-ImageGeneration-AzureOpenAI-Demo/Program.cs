using System.ClientModel;
using OpenAI;
using OpenAI.Chat;
using OpenAI.Responses;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using ChatMessage = Microsoft.Extensions.AI.ChatMessage;
using System.ClientModel.Primitives;


IConfigurationRoot config = new ConfigurationBuilder().AddUserSecrets<Program>().Build();
string endpoint = config["endpoint"]!;
string apikey = config["apikey"]!;

using HttpClient httpClient = new();
httpClient.DefaultRequestHeaders.Add("x-ms-oai-image-generation-deployment", "gpt-image-2");
#pragma warning disable OPENAI001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
ResponsesClient client = new ResponsesClient(new ApiKeyCredential(apikey),new ResponsesClientOptions
{
    Endpoint = new Uri(new Uri(endpoint), "/openai/v1/"),
    Transport = new HttpClientPipelineTransport(httpClient)
});

#pragma warning restore OPENAI001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

AIAgent agent = client
        .AsAIAgent(
            tools: [new HostedImageGenerationTool
            {
                Options = new ImageGenerationOptions
                {
                    MediaType = "image/png",
                    ModelId = "gpt-image-2"
                }
            }],
            model: "gpt-5"
        );
                
AgentResponse imageresponse = await agent.RunAsync("Create an image of a cute Havaneese dog");

foreach (ChatMessage message in imageresponse.Messages)
{
    foreach (AIContent content in message.Contents)
    {
        if (content is ImageGenerationToolResultContent imageContent)
        {
            foreach (AIContent aiContent in imageContent.Outputs ?? [])
            {
                if (aiContent is DataContent dataContent)
                {
                    string path = Path.Combine(Path.GetTempPath(), $"image-{Guid.NewGuid():N}.png");
                    byte[] h = dataContent.Data.ToArray();
                    await File.WriteAllBytesAsync(path, h);

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
}

if (imageresponse.Usage != null)
{
    Console.ForegroundColor = ConsoleColor.Magenta;
    Console.WriteLine();
    Console.WriteLine($"Input Tokens: {imageresponse.Usage.InputTokenCount}");
    Console.WriteLine($"Cached Tokens: {imageresponse.Usage.CachedInputTokenCount ?? 0}");
    Console.WriteLine($"Output Tokens: {imageresponse.Usage.OutputTokenCount}");
    Console.WriteLine($"Reasoning Tokens: {imageresponse.Usage.ReasoningTokenCount ?? 0}");
    Console.WriteLine($"Total Tokens: {imageresponse.Usage.TotalTokenCount}");
    Console.ResetColor();
}
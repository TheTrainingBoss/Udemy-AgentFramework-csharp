using System.ClientModel;
using OpenAI;
using OpenAI.Chat;
using OpenAI.Responses;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using ChatMessage = Microsoft.Extensions.AI.ChatMessage;


IConfigurationRoot config = new ConfigurationBuilder().AddUserSecrets<Program>().Build();
string endpoint = config["endpoint"]!;
string apikey = config["apikey"]!;
string openaiapikey = config["openaikey"]!;

// AzureOpenAIClient client = new AzureOpenAIClient(new Uri(endpoint), new ApiKeyCredential(apikey), new AzureOpenAIClientOptions
// {
//     NetworkTimeout = TimeSpan.FromSeconds(60)
// });

//For PDF we have to use OpenAIClient, still not supported in AzureOpenAIClient
OpenAIClient client = new OpenAIClient(new ApiKeyCredential(openaiapikey));

// This is set to the default of Medium for reasonning efforts
ChatClientAgent agent = client
        .GetChatClient("gpt-5")
        .AsAIAgent();

string path = Path.Combine(Directory.GetCurrentDirectory(), "englishdriverhandbook.pdf");
string base64pdf = Convert.ToBase64String(File.ReadAllBytes(path));
string dataUri = $"data:application/pdf;base64,{base64pdf}";

AgentResponse pdfresponse = await agent.RunAsync(new ChatMessage(ChatRole.User,
[
    new TextContent("How old you have to be to drive in Florida?"),
    new DataContent(dataUri, "application/pdf")
]));

Console.WriteLine(pdfresponse);

if (pdfresponse.Usage != null)
{
    Console.ForegroundColor = ConsoleColor.Magenta;
    Console.WriteLine();
    Console.WriteLine($"Input Tokens: {pdfresponse.Usage.InputTokenCount}");
    Console.WriteLine($"Cached Tokens: {pdfresponse.Usage.CachedInputTokenCount ?? 0}");
    Console.WriteLine($"Output Tokens: {pdfresponse.Usage.OutputTokenCount}");
    Console.WriteLine($"Reasoning Tokens: {pdfresponse.Usage.ReasoningTokenCount ?? 0}");
    Console.WriteLine($"Total Tokens: {pdfresponse.Usage.TotalTokenCount}");
    Console.ResetColor();
}


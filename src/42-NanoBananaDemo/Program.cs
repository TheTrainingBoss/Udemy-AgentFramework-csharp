using GenerativeAI.Microsoft;
using GenerativeAI.Types;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;

IConfigurationRoot config = new ConfigurationBuilder().AddUserSecrets<Program>().Build();
string apikey = config["Google:ApiKey"]!;

// For Nano Banana 2, use "gemini-3.1-flash-image".
// For Nano Banana Pro use "gemini-3-pro-image"
const string modelId = "gemini-3-pro-image";

using IChatClient chatClient = new GenerativeAIChatClient(apiKey: apikey, modelName: modelId);

var chatOptions = new ChatOptions
{
    AdditionalProperties = new AdditionalPropertiesDictionary
    {
        [AdditionalPropertiesKeys.ResponseModalities] =
            new List<Modality> { Modality.TEXT, Modality.IMAGE },

        [AdditionalPropertiesKeys.ImageConfigAspectRatio] = "1:1",
        [AdditionalPropertiesKeys.ImageConfigImageSize] = "2K"
    }
};


AIAgent agent = chatClient.AsAIAgent(new ChatClientAgentOptions
{
    Name = "ImageGenerator",
    ChatOptions = chatOptions
});

int saved = 0;
                
AgentResponse imageresponse = await agent.RunAsync("Create an image of a cute Havaneese dog");

foreach (var content in imageresponse.Messages.SelectMany(m => m.Contents))
{
    if (content is TextContent text)
    {
        Console.WriteLine(text.Text);
    }
    else if (content is DataContent image && image.MediaType?.StartsWith("image/", StringComparison.OrdinalIgnoreCase) == true)
         {
            string extension = image.MediaType.ToLowerInvariant() switch
            {
                "image/png" => ".png",
                "image/jpeg" => ".jpg",
                "image/webp" => ".webp",
                _ => ".bin"
            };

            string path = Path.Combine(Directory.GetCurrentDirectory(), $"generated-{++saved}{extension}");

            await File.WriteAllBytesAsync(path, image.Data.ToArray());
            Console.WriteLine($"Saved: {path}");
         }
}

if (saved == 0)
{
    Console.WriteLine("The model returned no image. Review its text response.");
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


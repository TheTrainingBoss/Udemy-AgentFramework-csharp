using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Configuration;

IConfigurationRoot config = new ConfigurationBuilder().AddUserSecrets<Program>().Build();
string apikey = config["Google:ApiKey"]!;

using var client = new HttpClient
{
    Timeout = TimeSpan.FromMinutes(10)
};

client.DefaultRequestHeaders.Add("x-goog-api-key", apikey);

var request = new
{
    model = "gemini-omni-1.2-flash",
    input = """
        Create a short promotional video for a software conference. 
        A fictional adult presenter with an enthusiastic, professional manner stands outside a modern convention 
        center in Orlando, Florida, on a sunny day. The presenter holds a plain microphone and speaks directly to 
        the camera: Join us in Orlando, November sixteenth through nineteenth, twenty twenty-six, for exciting sessions 
        and hands-on workshops about Microsoft Agent Framework!
        Use a steady medium shot, natural lighting, clear speech, and subtle outdoor ambience. This is a staged conference 
        promotion featuring an original fictional character. Use neutral clothing and an unbranded building, without broadcaster 
        logos or news graphics.cls
        
        """,
    response_format = new
    {
        type = "video",
        duration = "8s",
        aspect_ratio = "16:9",
        resolution = "720p"
    }
};

using var response = await client.PostAsJsonAsync(
    "https://generativelanguage.googleapis.com/v1beta/interactions", request);

string json = await response.Content.ReadAsStringAsync();

if (!response.IsSuccessStatusCode)
{
    throw new HttpRequestException(
        $"Google API returned {(int)response.StatusCode}: {json}");
}

using var document = JsonDocument.Parse(json);
int saved = 0;

foreach (var step in document.RootElement.GetProperty("steps").EnumerateArray())
{
    if (!step.TryGetProperty("type", out var stepType) ||
        stepType.GetString() != "model_output" ||
        !step.TryGetProperty("content", out var contents))
    {
        continue;
    }

    foreach (var content in contents.EnumerateArray())
    {
        if (!content.TryGetProperty("type", out var type))
            continue;

        if (type.GetString() == "text" &&
            content.TryGetProperty("text", out var text))
        {
            Console.WriteLine(text.GetString());
        }
        else if (type.GetString() == "video" &&
                 content.TryGetProperty("data", out var data))
        {
            string mimeType = content.GetProperty("mime_type").GetString()
                ?? throw new InvalidOperationException("Video MIME type is missing.");

            if (mimeType != "video/mp4")
                throw new InvalidOperationException($"Unexpected video type: {mimeType}");

            byte[] videoBytes = Convert.FromBase64String(
                data.GetString() ?? throw new InvalidOperationException("Video data is missing."));

            string path = Path.Combine(Directory.GetCurrentDirectory(), $"generatedVideo-{++saved}.mp4");

            await File.WriteAllBytesAsync(path, videoBytes);
            Console.WriteLine($"Saved: {path}");
        }
    }
}

if (saved == 0)
    throw new InvalidOperationException("The response contained no video.");
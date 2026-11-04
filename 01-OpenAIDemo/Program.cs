using Microsoft.Agents.AI;
using OpenAI;
using OpenAI.Chat;

//You can get a new OpenAI API KEY from https://platform.openai.com/api-keys
string apikey = "sk-proj-B7fn2pakYk <enter your API key here>";  

OpenAIClient client = new OpenAIClient(apikey);

ChatClientAgent agent = client.GetChatClient("gpt-5-mini").AsAIAgent();

AgentResponse response = await agent.RunAsync("Why is the sky blue?");

Console.WriteLine(response);

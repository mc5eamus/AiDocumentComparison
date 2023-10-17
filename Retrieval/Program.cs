using Azure;
using Azure.AI.OpenAI;
using Microsoft.Extensions.Configuration;
using VectorIndex;

// implement configuration builder retrieving the values from appsettings.json
var config = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json")
    .Build();

var openAIClient = new OpenAIClient(new Uri(config["OpenAI:Endpoint"]), new AzureKeyCredential(config["OpenAI:ApiKey"]));
var factoidRetrieval = new FactoidRetrieval(config.GetSection("Search"));

var query = "How were the daughter cultures inoculated and incubated";
var documentIds = new string[] { "doc27", "doc28" };

var retrievalTasks = documentIds.Select(id => 
    factoidRetrieval.RetrieveFactoid(id, query, async (q) => (await openAIClient.GetEmbeddingsAsync(config["OpenAI:DeploymentEmbeddings"], new EmbeddingsOptions(q))).Value.Data[0].Embedding, 2));

var result = await Task.WhenAll(retrievalTasks);

var chatCompletionsOptions = new ChatCompletionsOptions()
{
    Messages =
    {
        new ChatMessage(ChatRole.System, "You are a helpful assistant focusing on finding differences between documents."),
        new ChatMessage(ChatRole.User, "Here are quotes from Document1 and Document2 respectively. " +
        "[ Document1 ]\n" +
        result[0] +
        "[ End Document1 ]\n" +
        "\n[ Document2 ]\n" +
        result[1] +
        "\n[ End Document2 ]\n" +
        "What are the differences between the two documents related to the following question:" +
        query
        ),
    },
    MaxTokens = 500
};

var response = await openAIClient.GetChatCompletionsAsync(
    deploymentOrModelName: config["OpenAI:DeploymentCompletions"],
    chatCompletionsOptions);

Console.WriteLine(response.Value.Choices[0].Message.Content);


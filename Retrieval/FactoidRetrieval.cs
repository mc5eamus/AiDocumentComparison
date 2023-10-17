using Azure.Search.Documents.Indexes;
using Azure;
using Azure.Search.Documents;
using Azure.Search.Documents.Models;
using Microsoft.Extensions.Configuration;

namespace VectorIndex
{
    internal class FactoidRetrieval
    {
        private AzureKeyCredential credential;
        private SearchIndexClient indexClient;
        private SearchClient searchClient;

        public FactoidRetrieval(IConfigurationSection config)
        {
            credential = new AzureKeyCredential(config["ApiKey"]);
            indexClient = new SearchIndexClient(new Uri(config["Endpoint"]), credential);
            searchClient = indexClient.GetSearchClient(config["Index"]);
        }

        public async Task<string> RetrieveFactoid(string documentId, 
            string query, 
            Func<string, Task<IReadOnlyList<float>>> embedder, int maxValues= 1)
        {
            var vector = await embedder(query);
            var searchOptions = new SearchOptions
            {
                Vectors = { new() { Value = vector.ToArray(), KNearestNeighborsCount = maxValues, Fields = { "vector" } } },
                Filter = $"documentId eq '{documentId}'",
                Size = maxValues,
                Select = { "documentId", "chapter" },
            };
            var res = await searchClient.SearchAsync<SearchDocument>(null, searchOptions);
            return string.Join("\r\n", res.Value.GetResults().Select(r => r.Document["chapter"] as string));
        }
    }
}

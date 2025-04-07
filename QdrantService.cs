using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Linq;

namespace RevitGpt
{
    /// <summary>
    /// Service for interacting with the Qdrant vector database
    /// </summary>
    public class QdrantService
    {
        private readonly string _qdrantUrl;
        private readonly HttpClient _httpClient;
        private readonly string _collectionName;

        public QdrantService(string qdrantUrl = "http://localhost:6333", string collectionName = "revit_docs")
        {
            _qdrantUrl = qdrantUrl.TrimEnd('/');
            _collectionName = collectionName;
            _httpClient = new HttpClient();
        }

        /// <summary>
        /// Create a collection in Qdrant if it doesn't already exist
        /// </summary>
        public async Task CreateCollectionIfNotExistsAsync(int vectorDimension = 1536)
        {
            try
            {
                // Check if collection exists
                var checkResponse = await _httpClient.GetAsync($"{_qdrantUrl}/collections/{_collectionName}");

                // If collection exists, return
                if (checkResponse.IsSuccessStatusCode)
                {
                    Console.WriteLine($"Collection {_collectionName} already exists");
                    return;
                }

                // Create collection
                var createBody = new
                {
                    name = _collectionName,
                    vectors = new
                    {
                        size = vectorDimension,
                        distance = "Cosine"
                    }
                };

                var content = new StringContent(
                    JsonConvert.SerializeObject(createBody),
                    Encoding.UTF8,
                    "application/json");

                var response = await _httpClient.PutAsync($"{_qdrantUrl}/collections/{_collectionName}", content);
                var responseString = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    throw new Exception($"Failed to create collection: {responseString}");
                }

                Console.WriteLine($"Collection {_collectionName} created successfully");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error creating collection: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Upload chunk embeddings to Qdrant
        /// </summary>
        public async Task UploadEmbeddingsAsync(List<ChunkEmbedding> embeddings)
        {
            try
            {
                // Process in batches to avoid request size limits
                int batchSize = 100;
                for (int i = 0; i < embeddings.Count; i += batchSize)
                {
                    var batch = embeddings.Skip(i).Take(batchSize).ToList();
                    await UploadEmbeddingBatchAsync(batch);
                    Console.WriteLine($"Uploaded batch {i / batchSize + 1} of {Math.Ceiling((double)embeddings.Count / batchSize)}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error uploading embeddings: {ex.Message}");
                throw;
            }
        }

        private async Task UploadEmbeddingBatchAsync(List<ChunkEmbedding> batch)
        {
            var points = batch.Select(e => new
            {
                id = e.ChunkId,
                vector = e.Embedding,
                payload = new
                {
                    source_file = e.SourceFile,
                    content = e.Content
                }
            }).ToList();

            var requestBody = new
            {
                points = points
            };

            var content = new StringContent(
                JsonConvert.SerializeObject(requestBody),
                Encoding.UTF8,
                "application/json");

            var response = await _httpClient.PutAsync($"{_qdrantUrl}/collections/{_collectionName}/points", content);
            var responseString = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                throw new Exception($"Failed to upload embeddings: {responseString}");
            }
        }

        /// <summary>
        /// Search for similar documents using a query embedding
        /// </summary>
        public async Task<List<SearchResult>> SearchSimilarDocumentsAsync(List<float> queryEmbedding, int limit = 5)
        {
            try
            {
                var requestBody = new
                {
                    vector = queryEmbedding,
                    limit = limit,
                    with_payload = true  // Explicitly request payload data
                };

                var content = new StringContent(
                    JsonConvert.SerializeObject(requestBody),
                    Encoding.UTF8,
                    "application/json");

                var response = await _httpClient.PostAsync(
                    $"{_qdrantUrl}/collections/{_collectionName}/points/search",
                    content);

                var responseString = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    throw new Exception($"Search failed: {responseString}");
                }

                var searchResponse = JsonConvert.DeserializeObject<SearchResponse>(responseString);
                return searchResponse.result.Select(r => new SearchResult
                {
                    ChunkId = r.id,
                    SourceFile = r.payload.source_file,
                    Content = r.payload.content,
                    Score = r.score
                }).ToList();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error searching documents: {ex.Message}");
                throw;
            }
        }
    }

    // Classes for search results
    public class SearchResult
    {
        public string ChunkId { get; set; }
        public string SourceFile { get; set; }
        public string Content { get; set; }
        public float Score { get; set; }
    }

    // Classes for deserializing Qdrant API responses
    internal class SearchResponse
    {
        public List<SearchResultItem> result { get; set; }
    }

    internal class SearchResultItem
    {
        public string id { get; set; }
        public SearchResultPayload payload { get; set; }
        public float score { get; set; }
    }

    internal class SearchResultPayload
    {
        public string source_file { get; set; }
        public string content { get; set; }
    }
}
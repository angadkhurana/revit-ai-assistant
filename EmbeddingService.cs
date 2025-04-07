using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace RevitGpt
{
    /// <summary>
    /// Service for generating text embeddings using OpenAI's API
    /// </summary>
    public class EmbeddingService
    {
        private readonly string _apiKey;
        private readonly string _embeddingModel;
        private readonly HttpClient _httpClient;

        public EmbeddingService(string apiKey, string embeddingModel = "text-embedding-3-small")
        {
            _apiKey = apiKey;
            _embeddingModel = embeddingModel;
            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_apiKey}");
        }

        /// <summary>
        /// Generate embeddings for a list of document chunks
        /// </summary>
        public async Task<List<ChunkEmbedding>> GenerateEmbeddingsAsync(List<DocumentChunk> chunks)
        {
            List<ChunkEmbedding> results = new List<ChunkEmbedding>();

            foreach (var chunk in chunks)
            {
                try
                {
                    var embedding = await GenerateEmbeddingAsync(chunk.Content);
                    results.Add(new ChunkEmbedding
                    {
                        ChunkId = chunk.ChunkId,
                        SourceFile = chunk.SourceFile,
                        Content = chunk.Content,
                        Embedding = embedding
                    });
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error generating embedding for chunk {chunk.ChunkId}: {ex.Message}");
                }
            }

            return results;
        }

        /// <summary>
        /// Generate embedding for a single text
        /// </summary>
        public async Task<List<float>> GenerateEmbeddingAsync(string text)
        {
            var requestBody = new
            {
                model = _embeddingModel,
                input = text
            };

            var content = new StringContent(
                JsonConvert.SerializeObject(requestBody),
                Encoding.UTF8,
                "application/json");

            var response = await _httpClient.PostAsync("https://api.openai.com/v1/embeddings", content);
            var responseString = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                throw new Exception($"API call failed with status code {response.StatusCode}: {responseString}");
            }

            var responseObject = JsonConvert.DeserializeObject<EmbeddingResponse>(responseString);
            return responseObject.data[0].embedding;
        }
    }

    /// <summary>
    /// Represents a document chunk with its embedding
    /// </summary>
    public class ChunkEmbedding
    {
        public string ChunkId { get; set; }
        public string SourceFile { get; set; }
        public string Content { get; set; }
        public List<float> Embedding { get; set; }
    }

    // Classes for deserializing OpenAI API responses
    public class EmbeddingResponse
    {
        public List<EmbeddingData> data { get; set; }
    }

    public class EmbeddingData
    {
        public List<float> embedding { get; set; }
    }
}
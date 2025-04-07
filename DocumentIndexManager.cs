using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Autodesk.Revit.UI;
using RevitGpt;

namespace RevitGpt
{
    /// <summary>
    /// Manages the document indexing process
    /// </summary>
    public class DocumentIndexManager
    {
        private readonly DocumentChunker _chunker;
        private readonly EmbeddingService _embeddingService;
        private readonly QdrantService _qdrantService;
        private readonly string _documentsDirectory;

        public DocumentIndexManager(
            string apiKey,
            string documentsDirectory,
            string qdrantUrl = "http://localhost:6333")
        {
            _chunker = new DocumentChunker();
            _embeddingService = new EmbeddingService(apiKey);
            _qdrantService = new QdrantService(qdrantUrl);
            _documentsDirectory = documentsDirectory;
        }

        /// <summary>
        /// Index all text documents in the specified directory
        /// </summary>
        public async Task IndexDocumentsAsync(IStatusCallback statusCallback = null)
        {
            try
            {
                // Step 1: Find all text files
                statusCallback?.UpdateStatus("Finding text files...");
                List<string> textFiles = _chunker.FindTextFiles(_documentsDirectory);
                if (textFiles.Count == 0)
                {
                    statusCallback?.UpdateStatus("No text files found.");
                    return;
                }
                statusCallback?.UpdateStatus($"Found {textFiles.Count} text files.");

                // Step 2: Create Qdrant collection
                statusCallback?.UpdateStatus("Creating vector database collection...");
                await _qdrantService.CreateCollectionIfNotExistsAsync();

                // Step 3: Process each file
                List<DocumentChunk> allChunks = new List<DocumentChunk>();
                int processedFiles = 0;

                foreach (string file in textFiles)
                {
                    statusCallback?.UpdateStatus($"Processing file {++processedFiles} of {textFiles.Count}: {System.IO.Path.GetFileName(file)}");

                    // Chunk the document
                    List<DocumentChunk> chunks = _chunker.ChunkDocument(file);
                    allChunks.AddRange(chunks);

                    statusCallback?.UpdateStatus($"Generated {chunks.Count} chunks from {System.IO.Path.GetFileName(file)}");
                }

                // Step 4: Generate embeddings
                statusCallback?.UpdateStatus($"Generating embeddings for {allChunks.Count} chunks...");
                List<ChunkEmbedding> embeddings = await _embeddingService.GenerateEmbeddingsAsync(allChunks);
                statusCallback?.UpdateStatus($"Generated {embeddings.Count} embeddings.");

                // Step 5: Upload to Qdrant
                statusCallback?.UpdateStatus("Uploading embeddings to vector database...");
                await _qdrantService.UploadEmbeddingsAsync(embeddings);

                statusCallback?.UpdateStatus("Document indexing completed successfully.");
            }
            catch (Exception ex)
            {
                string errorMessage = $"Error indexing documents: {ex.Message}";
                statusCallback?.UpdateStatus(errorMessage);
                Console.WriteLine(errorMessage);
                TaskDialog.Show("Document Indexing Error", errorMessage);
            }
        }

        /// <summary>
        /// Search for documents similar to a query
        /// </summary>
        public async Task<List<SearchResult>> SearchDocumentsAsync(string query, int limit = 5)
        {
            // Generate embedding for the query
            List<float> queryEmbedding = await _embeddingService.GenerateEmbeddingAsync(query);

            // Search for similar documents
            return await _qdrantService.SearchSimilarDocumentsAsync(queryEmbedding, limit);
        }
    }

    /// <summary>
    /// Interface for status updates during indexing
    /// </summary>
    public interface IStatusCallback
    {
        void UpdateStatus(string status);
    }
}
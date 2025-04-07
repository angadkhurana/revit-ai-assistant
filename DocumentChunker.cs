using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace RevitGpt
{
    /// <summary>
    /// Handles the chunking of document content into smaller pieces for embedding
    /// </summary>
    public class DocumentChunker
    {
        private readonly int _maxChunkSize;
        private readonly int _chunkOverlap;

        public DocumentChunker(int maxChunkSize = 1000, int chunkOverlap = 200)
        {
            _maxChunkSize = maxChunkSize;
            _chunkOverlap = chunkOverlap;
        }

        /// <summary>
        /// Find all .txt files in the specified directory and its subdirectories
        /// </summary>
        public List<string> FindTextFiles(string directoryPath)
        {
            try
            {
                return new List<string>(Directory.GetFiles(directoryPath, "*.txt", SearchOption.AllDirectories));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error finding text files: {ex.Message}");
                return new List<string>();
            }
        }

        /// <summary>
        /// Read a file and return its content
        /// </summary>
        public string ReadFileContent(string filePath)
        {
            try
            {
                return File.ReadAllText(filePath);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error reading file {filePath}: {ex.Message}");
                return string.Empty;
            }
        }

        /// <summary>
        /// Chunk the document content into smaller pieces
        /// </summary>
        public List<DocumentChunk> ChunkDocument(string filePath)
        {
            string content = ReadFileContent(filePath);
            if (string.IsNullOrEmpty(content))
            {
                return new List<DocumentChunk>();
            }

            List<DocumentChunk> chunks = new List<DocumentChunk>();
            string fileName = Path.GetFileName(filePath);

            // Simple chunking by paragraphs then by size
            string[] paragraphs = content.Split(new[] { "\r\n\r\n", "\n\n" }, StringSplitOptions.None);
            StringBuilder currentChunk = new StringBuilder();

            foreach (string paragraph in paragraphs)
            {
                // If adding this paragraph would exceed the max chunk size, 
                // save the current chunk and start a new one
                if (currentChunk.Length + paragraph.Length > _maxChunkSize && currentChunk.Length > 0)
                {
                    chunks.Add(new DocumentChunk
                    {
                        SourceFile = fileName,
                        Content = currentChunk.ToString().Trim(),
                        ChunkId = Guid.NewGuid().ToString()
                    });

                    // Start a new chunk with overlap if possible
                    if (currentChunk.Length > _chunkOverlap)
                    {
                        string overlapText = currentChunk.ToString().Substring(
                            Math.Max(0, currentChunk.Length - _chunkOverlap));
                        currentChunk = new StringBuilder(overlapText);
                    }
                    else
                    {
                        currentChunk = new StringBuilder();
                    }
                }

                currentChunk.AppendLine(paragraph);
                currentChunk.AppendLine();
            }

            // Add the final chunk if there's any content left
            if (currentChunk.Length > 0)
            {
                chunks.Add(new DocumentChunk
                {
                    SourceFile = fileName,
                    Content = currentChunk.ToString().Trim(),
                    ChunkId = Guid.NewGuid().ToString()
                });
            }

            return chunks;
        }
    }

    /// <summary>
    /// Represents a chunk of document content
    /// </summary>
    public class DocumentChunk
    {
        public string ChunkId { get; set; }
        public string SourceFile { get; set; }
        public string Content { get; set; }
    }
}
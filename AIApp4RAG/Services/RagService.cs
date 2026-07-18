using AIApp4RAG.Models;
using Microsoft.Extensions.AI;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.InMemory;
using Microsoft.SemanticKernel.Embeddings;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;

namespace AIApp4RAG.Services
{
    public class RagService
    {
        private readonly ITextEmbeddingGenerationService _embeddingService;
        private readonly IChatCompletionService _chatService;
        private readonly InMemoryVectorStore _vectorStore;

        public RagService(ITextEmbeddingGenerationService embeddingService, IChatCompletionService chatService,
            InMemoryVectorStore vectorStore) => (_embeddingService, _chatService, _vectorStore) = (embeddingService, chatService, vectorStore);

        public async Task<int> IngestDocumentAsync(
            string collectionName,
            string fullText,
            string fileName
            )
        {
            var collection = _vectorStore.GetCollection<string, DocumentChunk>(collectionName);
            await collection.EnsureCollectionExistsAsync();

            var chunks = ChunkText(fullText, chunkSize: 500, overlap: 50);

            int chunkCount = 0;

            foreach (var (chunkText, index) in chunks)
            {
                var embedding = await _embeddingService.GenerateEmbeddingAsync(chunkText);

                var chunk = new DocumentChunk
                {
                    Id = $"{fileName} - chunk -{index}",
                    FileName = fileName,
                    Content = chunkText,
                    ChunkIndex = index,
                    Embedding = embedding

                };

                await collection.UpsertAsync(chunk);

                chunkCount++;
            }

            return chunkCount;


        }



        public async Task<object> AskAsync(
            
            string question,
            string collectionName
            
            )
        {
            var collection = _vectorStore.GetCollection<string, DocumentChunk>(collectionName);

            if (!await collection.CollectionExistsAsync())
                return new { error = "Document not found. Please upload first." };

            var questionEmbedding = await _embeddingService.GenerateEmbeddingAsync(question);

            var searchResults = await collection.SearchAsync(questionEmbedding, top: 3).ToListAsync();

            if (!searchResults.Any())
                return new { answer = "No relevant content found in the document." };

            var context = string.Join("\n\n--\n\n",
                searchResults.Select((r, i) =>
                $"[Source {i + 1}]\n {r.Record.Content}"));
                

            var prompt = $"""
                  You are a helpful assistant that answers questions about documents.
            
            Answer the question ONLY from the context provided below.
            If the answer is not in the context, say exactly:
            "I could not find this information in the uploaded document."
            Do not use your general knowledge.
            Always mention which source you used (Source 1, 2, or 3).a helpful assistant that answers questions about documents.
            
            Answer the question ONLY from the context provided below.
            If the answer is not in the context, say exactly:
            "I could not find this information in the uploaded document."
            Do not use your general knowledge.
            Always mention which source you used (Source 1, 2, or 3).

            CONTEXT:
            {context}
            
            QUESTION: {question}
            
            ANSWER:
            """;

            var chatHistory = new ChatHistory();
            chatHistory.AddUserMessage(prompt);

            var response = await _chatService.GetChatMessageContentAsync(chatHistory);

            return new
            {
                answer = response.Content,
                sources = searchResults.Select(r => new
                {
                    chunkIndex = r.Record.ChunkIndex,
                    preview = r.Record.Content.Length > 150 ? r.Record.Content[..150] + "..." : r.Record.Content,
                    relevanceScore = r.Score
                })
            };
        }


        private List<(string text, int index)> ChunkText(
            
            string text,
            int chunkSize,
            int overlap
            )
        {
            var chunks = new List<(string, int)>();
            var words = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);

            int index = 0;
            int position = 0;

            while (position < words.Length)
            {
                var chunkWords = words.Skip(position).Take(chunkSize).ToArray();

                chunks.Add((string.Join(" ", chunkWords), index));

                position += chunkSize - overlap;
                index++;
            }

            return chunks;
        }




    }
}

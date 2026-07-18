using Microsoft.Extensions.VectorData;

namespace AIApp4RAG.Models
{
    public class DocumentChunk
    {
        [VectorStoreKey]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        [VectorStoreData]
        public string FileName { get; set; } = string.Empty;

        [VectorStoreData]
        public string Content { get; set; } = string.Empty;

        [VectorStoreData]
        public int ChunkIndex { get; set; }

        [VectorStoreVector(Dimensions: 1536, DistanceFunction = DistanceFunction.CosineSimilarity)]
        public  ReadOnlyMemory<float> Embedding { get; set; }

    }
}

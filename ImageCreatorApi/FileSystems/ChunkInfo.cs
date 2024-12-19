using System.Text.Json;
using System.Text.Json.Serialization;

namespace ImageCreatorApi.FileSystems
{
    public class ChunkInfo
    {
        [JsonPropertyName("totalFileLength")]
        public long TotalFileLength { get; init; }

        [JsonPropertyName("chunkCount")]
        public int ChunkCount { get; init; }

        [JsonPropertyName("chunks")]
        public IReadOnlyList<ChunkInfoRow> Chunks { get; init; }

        [JsonConstructor]
        public ChunkInfo(long totalFileLength, int chunkCount, IReadOnlyList<ChunkInfoRow> chunks)
        {
            if (chunkCount <= 0)
                throw new ArgumentException("Chunk count must be greater than zero.", nameof(chunkCount));

            if (totalFileLength <= 0)
                throw new ArgumentException("Total file length must be greater than zero.", nameof(totalFileLength));

            if (chunks == null || chunks.Count != chunkCount)
                throw new ArgumentException("Chunks count must match the chunk count.", nameof(chunks));

            TotalFileLength = totalFileLength;
            ChunkCount = chunkCount;
            Chunks = chunks;
        }

        public string ToJson()
        {
            return JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
        }

        public static ChunkInfo FromJson(string json)
        {
            return JsonSerializer.Deserialize<ChunkInfo>(json) ?? throw new InvalidOperationException("Failed to deserialize ChunkInfo.");
        }
    }
}

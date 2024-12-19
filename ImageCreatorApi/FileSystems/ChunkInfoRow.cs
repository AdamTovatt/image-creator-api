using System.Text.Json.Serialization;

namespace ImageCreatorApi.FileSystems
{
    public class ChunkInfoRow
    {
        [JsonPropertyName("publicId")]
        public string PublicId { get; init; }

        [JsonPropertyName("secureUrl")]
        public string SecureUrl { get; init; }

        [JsonConstructor]
        public ChunkInfoRow(string publicId, string secureUrl)
        {
            if (string.IsNullOrWhiteSpace(publicId))
                throw new ArgumentException("Public ID cannot be null or empty.", nameof(publicId));

            if (string.IsNullOrWhiteSpace(secureUrl))
                throw new ArgumentException("Secure URL cannot be null or empty.", nameof(secureUrl));

            PublicId = publicId;
            SecureUrl = secureUrl;
        }
    }
}

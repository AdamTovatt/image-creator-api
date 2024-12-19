using System.Text.Json;
using System.Text.Json.Serialization;

namespace ImageCreatorApi.Models.Photoshop
{
    public class PhotoshopFileMetadata
    {
        [JsonPropertyName("thumbnailUrl")]
        public string ThumbnailUrl { get; set; }

        [JsonPropertyName("layers")]
        public List<PhotoshopLayer> Layers { get; set; }

        public PhotoshopFileMetadata(string thumbnailUrl, List<PhotoshopLayer> layers)
        {
            ThumbnailUrl = thumbnailUrl;
            Layers = layers;
        }

        public static PhotoshopFileMetadata FromJson(string json)
        {
            return JsonSerializer.Deserialize<PhotoshopFileMetadata>(json) ?? throw new InvalidOperationException($"Failed to deserialize {nameof(PhotoshopFileMetadata)} from json: {json}");
        }

        public string ToJson()
        {
            return JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
        }
    }
}

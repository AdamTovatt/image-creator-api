using System.Text.Json;
using System.Text.Json.Serialization;

namespace ImageCreatorApi.Models.Photoshop
{
    public class PhotoshopFileInfo
    {
        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("metadata")]
        public PhotoshopFileMetadata? Metadata { get; set; }

        public PhotoshopFileInfo(string name, PhotoshopFileMetadata? metadata)
        {
            Name = name;
            Metadata = metadata;
        }

        public static PhotoshopFileInfo FromJson(string json)
        {
            return JsonSerializer.Deserialize<PhotoshopFileInfo>(json) ?? throw new InvalidOperationException($"Failed to deserialize {nameof(PhotoshopFileInfo)} from json: {json}");
        }

        public string ToJson()
        {
            return JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
        }
    }
}

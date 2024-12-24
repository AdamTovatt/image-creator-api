using ImageCreatorApi.FileSystems;
using System.Text;
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

        [JsonPropertyName("width")]
        public int Width { get; set; }

        [JsonPropertyName("height")]
        public int Height { get; set; }

        [JsonPropertyName("fileSize")]
        public long FileSize { get; set; }

        public PhotoshopFileMetadata(string thumbnailUrl, List<PhotoshopLayer> layers, int width, int height, long fileSize)
        {
            ThumbnailUrl = thumbnailUrl;
            Layers = layers;
            Width = width;
            Height = height;
            FileSize = fileSize;
        }

        public static PhotoshopFileMetadata FromJson(string json)
        {
            return JsonSerializer.Deserialize<PhotoshopFileMetadata>(json) ?? throw new InvalidOperationException($"Failed to deserialize {nameof(PhotoshopFileMetadata)} from json: {json}");
        }

        public string ToJson()
        {
            return JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
        }

        public byte[] ToUtf8EncondedJsonBytes()
        {
            string json = ToJson();
            return Encoding.UTF8.GetBytes(json);
        }

        public async static Task<PhotoshopFileMetadata> ReadAsync(IFileSystem from, PsdFileMetadataPath withFilePath)
        {
            using (Stream metadataStream = await from.ReadFileAsync(withFilePath.ToString()))
            using (StreamReader reader = new StreamReader(metadataStream))
            {
                return FromJson(await reader.ReadToEndAsync());
            }
        }
    }
}

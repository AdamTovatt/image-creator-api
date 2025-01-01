using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ImageCreatorApi.FileSystems
{
    public class CacheFileMetadata
    {
        [JsonPropertyName("fileHash")]
        public string FileHash { get; init; }

        public CacheFileMetadata(string fileHash)
        {
            FileHash = fileHash;
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

        public MemoryStream ToMemoryStream()
        {
            return new MemoryStream(ToUtf8EncondedJsonBytes());
        }

        public static CacheFileMetadata FromJson(string json)
        {
            return JsonSerializer.Deserialize<CacheFileMetadata>(json) ?? throw new InvalidOperationException($"Failed to deserialize {nameof(CacheFileMetadata)} from json: {json}");
        }

        public static async Task<CacheFileMetadata> ReadFromFileSystemAsync(IFileSystem fileSystem, string path)
        {
            using (Stream cacheFileStream = await fileSystem.ReadFileAsync(path))
            using (StreamReader cacheFileStreamReader = new StreamReader(cacheFileStream))
            {
                return FromJson(await cacheFileStreamReader.ReadToEndAsync());
            }
        }
    }
}

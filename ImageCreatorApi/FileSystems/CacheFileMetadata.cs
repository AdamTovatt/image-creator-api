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
            string? json = null;

            try
            {
                using (Stream cacheFileStream = await fileSystem.ReadFileAsync(path))
                using (StreamReader cacheFileStreamReader = new StreamReader(cacheFileStream))
                {
                    json = await cacheFileStreamReader.ReadToEndAsync();

                    if (string.IsNullOrEmpty(json))
                        throw new ArgumentException($"Tried to read cache metadata file from json but the json was null or empty for the path: {path}");

                    return FromJson(json);
                }
            }
            catch (Exception exception)
            {
                throw new Exception($"An error occurred when trying to read cache metadata file with the json: {json} from the path {path}", exception);
            }
        }

        public async Task WriteToFileSystemAsync(IFileSystem fileSystem, string path)
        {
            using (MemoryStream cacheMetadataStream = ToMemoryStream())
                await fileSystem.WriteFileAsync(path, cacheMetadataStream);
        }
    }
}

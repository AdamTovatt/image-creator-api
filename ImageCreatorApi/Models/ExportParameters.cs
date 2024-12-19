using System.Text.Json.Serialization;

namespace ImageCreatorApi.Models
{
    public class ExportParameters
    {
        [JsonPropertyName("fileName")]
        public string FileName { get; set; }

        [JsonPropertyName("texts")]
        public Dictionary<string, string> Texts { get; set; }

        public ExportParameters(string fileName, Dictionary<string, string> texts)
        {
            FileName = fileName;
            Texts = texts;
        }
    }
}

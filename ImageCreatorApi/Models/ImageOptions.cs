using System.Text.Json.Serialization;

namespace ImageCreatorApi.Models
{
    public class ImageOptions
    {
        [JsonPropertyName("fileName")]
        public string FileName { get; set; }

        [JsonPropertyName("mirror")]
        public bool Mirror { get; set; }

        [JsonPropertyName("shiftY")]
        public int ShiftY { get; set; }

        [JsonPropertyName("shiftX")]
        public int ShiftX { get; set; }

        [JsonIgnore]
        public IFormFile? ImageFile { get; set; }

        public ImageOptions(string fileName, bool mirror, int shiftY, int shiftX)
        {
            FileName = fileName;
            Mirror = mirror;
            ShiftY = shiftY;
            ShiftX = shiftX;
        }
    }
}

using System.Text.Json.Serialization;

namespace ImageCreatorApi.Models.Photoshop
{
    public class PhotoshopLayer
    {
        [JsonPropertyName("layerName")]
        public string LayerName { get; set; }

        [JsonPropertyName("isRecommendedForChanging")]
        public bool IsRecommendedForChanging { get; set; }

        [JsonPropertyName("isTextLayer")]
        public bool IsTextLayer { get; set; }

        [JsonPropertyName("isImageLayer")]
        public bool IsImageLayer { get; set; }

        [JsonPropertyName("textContent")]
        public string? TextContent { get; set; }

        public PhotoshopLayer(string layerName, bool isRecommendedForChanging, bool isTextLayer, bool isImageLayer, string? textContent)
        {
            LayerName = layerName;
            IsRecommendedForChanging = isRecommendedForChanging;
            IsTextLayer = isTextLayer;
            IsImageLayer = isImageLayer;
            TextContent = textContent;
        }
    }
}

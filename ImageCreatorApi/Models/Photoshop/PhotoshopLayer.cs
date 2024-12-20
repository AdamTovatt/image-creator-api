using PhotopeaNet.Models;
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

        public static PhotoshopLayer FromPhotopeaLayer(PhotopeaLayer photopeaLayer)
        {
            string layerName = photopeaLayer.Name;

            if (string.IsNullOrEmpty(layerName))
                layerName = "(Missing layer name)";

            bool recommendedForChanging = layerName[0] == '$' || layerName[0] == '@';
            bool isImageLayer = photopeaLayer.Kind == LayerKind.Normal || photopeaLayer.Kind == LayerKind.SmartObject;
            bool isTextLayer = photopeaLayer.Kind == LayerKind.Text;

            string? textContent = null;
            if (isTextLayer)
                textContent = photopeaLayer.TextItemData?.Contents;

            return new PhotoshopLayer(photopeaLayer.Name, recommendedForChanging, isTextLayer, isImageLayer, textContent);
        }
    }
}

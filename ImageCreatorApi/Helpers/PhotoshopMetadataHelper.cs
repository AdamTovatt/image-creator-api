using ImageCreatorApi.Factories;
using ImageCreatorApi.FileSystems;
using ImageCreatorApi.Models.Photoshop;
using PhotopeaNet;
using PhotopeaNet.Models;
using PhotopeaNet.Models.ImageSaving;

namespace ImageCreatorApi.Helpers
{
    public class PhotoshopMetadataHelper
    {
        private const int thumbnailSize = 100;

        public static async Task CreateMetadataAsync(string filePath)
        {
            IFileSystem fileSystem = FileSystemFactory.GetInstance();

            using (Stream fileStream = await fileSystem.ReadFileAsync(filePath))
            using (Photopea photopea = PhotopeaFactory.GetInstance())
            {
                await photopea.StartAsync();
                await photopea.LoadFileFromStreamAsync(fileStream);

                HashSet<string> requiredFonts = new HashSet<string>();
                List<PhotoshopLayer> photoshopLayers = new List<PhotoshopLayer>();

                foreach (PhotopeaLayer layer in await photopea.GetAllLayersAsync())
                {
                    photoshopLayers.Add(GetPhotoshopLayerFromPhotopeaLayer(layer));

                    if (layer.Kind == LayerKind.Text && !requiredFonts.Contains(layer.TextItemData!.FontName))
                        requiredFonts.Add(layer.TextItemData!.FontName);
                }

                foreach (string font in requiredFonts)
                {
                    try
                    {
                        using (Stream fontStream = await fileSystem.ReadFileAsync(new FontFilePath(font).ToString()))
                            await photopea.LoadFileFromStreamAsync(fontStream);
                    }
                    catch (FileNotFoundException) { }
                }

                await photopea.ResizeImage(thumbnailSize, thumbnailSize);

                using (MemoryStream memoryStream = new MemoryStream(await photopea.SaveImageAsync(new SaveJpgOptions(80))))
                {
                    
                }
            }
        }

        private static PhotoshopLayer GetPhotoshopLayerFromPhotopeaLayer(PhotopeaLayer layer)
        {
            string layerName = layer.Name;

            if (string.IsNullOrEmpty(layerName))
                layerName = "(Missing layer name)";

            bool recommendedForChanging = layerName[0] == '$' || layerName[0] == '@';
            bool isImageLayer = layer.Kind == LayerKind.Normal || layer.Kind == LayerKind.SmartObject;
            bool isTextLayer = layer.Kind == LayerKind.Text;

            string? textContent = null;
            if (isTextLayer)
                textContent = layer.TextItemData?.Contents;

            return new PhotoshopLayer(layer.Name, recommendedForChanging, isTextLayer, isImageLayer, textContent);
        }
    }
}

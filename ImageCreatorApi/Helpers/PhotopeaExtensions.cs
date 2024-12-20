using ImageCreatorApi.FileSystems;
using ImageCreatorApi.Models;
using ImageCreatorApi.Models.Photoshop;
using PhotopeaNet;
using PhotopeaNet.Models;
using Sakur.WebApiUtilities.Models;
using System.Net;

namespace ImageCreatorApi.Helpers
{
    public static class PhotopeaExtensions
    {
        public static async Task<HashSet<string>> GetRequiredFonts(this Photopea photopea, IEnumerable<PhotopeaLayer>? layers = null)
        {
            if (layers == null)
                layers = await photopea.GetAllLayersAsync();

            HashSet<string> requiredFonts = new HashSet<string>();

            foreach (PhotopeaLayer layer in layers)
            {
                if (layer.Kind == LayerKind.Text && !requiredFonts.Contains(layer.TextItemData!.FontName))
                    requiredFonts.Add(layer.TextItemData!.FontName);
            }

            return requiredFonts;
        }

        public static async Task LoadFonts(this Photopea photopea, IFileSystem from, IEnumerable<string> fonts, bool suppressFontNotFoundExceptions)
        {
            foreach (string font in fonts)
            {
                try
                {
                    using (Stream fontStream = await from.ReadFileAsync(new FontFilePath(font).ToString()))
                        await photopea.LoadFileFromStreamAsync(fontStream);
                }
                catch (FileNotFoundException)
                {
                    if (!suppressFontNotFoundExceptions)
                        throw;
                }
            }
        }

        public static List<PhotoshopLayer> ToPhotoshopLayers(this List<PhotopeaLayer> photopeaLayers)
        {
            List<PhotoshopLayer> photoshopLayers = new List<PhotoshopLayer>();

            foreach (PhotopeaLayer layer in photopeaLayers)
                photoshopLayers.Add(PhotoshopLayer.FromPhotopeaLayer(layer));

            return photoshopLayers;
        }

        public static async Task LoadFullProjectFile(this Photopea photopea, IFileSystem fileSystem, PsdFilePath psdFilePath)
        {
            using (Stream fileStream = await fileSystem.ReadFileAsync(psdFilePath.ToString()))
                await photopea.LoadFileFromStreamAsync(fileStream);

            await photopea.LoadFonts(from: fileSystem, fonts: await photopea.GetRequiredFonts(), suppressFontNotFoundExceptions: false);
        }

        public static async Task ApplyExportParameters(this Photopea photopea, ExportParameters parameters)
        {
            foreach (string textLayerName in parameters.TextOptions.Keys)
                await photopea.SetTextValueAsync(textLayerName, parameters.TextOptions[textLayerName]);

            if (parameters.ImageOptions != null)
            {
                foreach (string imageLayerName in parameters.ImageOptions.Keys)
                {
                    ImageOptions imageOptions = parameters.ImageOptions[imageLayerName];

                    if (imageOptions.ImageFile == null)
                        throw new ApiException($"Missing image file for image options for layer: {imageLayerName}", HttpStatusCode.BadRequest);

                    await photopea.SelectLayerByNameAsync(imageLayerName);
                    await photopea.SetVisibilityOfActiveLayerAsync(false);

                    using (Stream imageStream = imageOptions.ImageFile.OpenReadStream())
                        await photopea.InsertFileFromStreamAsync(imageStream, $"{imageLayerName}_new");

                    await photopea.MatchActiveLayerToOtherLayerAsync(imageLayerName);

                    if (imageOptions.Mirror)
                        await photopea.FlipActiveLayerHorizontallyAsync();

                    if (imageOptions.ShiftX != 0 || imageOptions.ShiftY != 0)
                        await photopea.TranslateActiveLayerAsync(imageOptions.ShiftX, imageOptions.ShiftY);
                }
            }
        }
    }
}

using ImageCreatorApi.FileSystems;
using ImageCreatorApi.Models;
using ImageCreatorApi.Models.Photoshop;
using PhotopeaNet;
using PhotopeaNet.Models;
using Sakur.WebApiUtilities.Models;
using System.Collections.ObjectModel;
using System.Net;

namespace ImageCreatorApi.Helpers
{
    public static class PhotopeaExtensions
    {
        public static async Task<ReadOnlyCollection<string>> GetRequiredFonts(this Photopea photopea, PhotopeaDocumentData? documentData = null)
        {
            if (documentData == null)
                documentData = await photopea.GetDocumentDataAsync();

            return documentData.RequiredFonts;
        }

        public static async Task LoadFonts(this Photopea photopea, IFileSystem from, IEnumerable<string> fonts, bool suppressFontNotFoundExceptions)
        {
            foreach (string font in fonts)
            {
                if (photopea.LoadedFonts.Contains(font)) continue;

                try
                {
                    using (CancellationTokenSource tokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(30)))
                    using (Stream fontStream = await from.ReadFileAsync(new FontFilePath(font).ToString()))
                        await photopea.LoadFontFromStreamAsync(fontStream, fontName: font, tokenSource.Token);
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
            using (CancellationTokenSource tokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(120)))
            using (Stream fileStream = await fileSystem.ReadFileAsync(psdFilePath.ToString()))
                await photopea.LoadFileAsync(fileStream, tokenSource.Token);

            await photopea.LoadFonts(from: fileSystem, fonts: await photopea.GetRequiredFonts(), suppressFontNotFoundExceptions: false);
        }

        public static async Task ApplyExportParameters(this Photopea photopea, ExportParameters parameters)
        {
            PhotopeaDocumentData data = await photopea.GetDocumentDataAsync();
            Dictionary<string, int> layerIds = new Dictionary<string, int>();

            foreach (string textLayerName in parameters.TextOptions.Keys)
                await photopea.SetTextValueAsync(textLayerName, parameters.TextOptions[textLayerName]);

            if (parameters.ImageOptions != null)
            {
                foreach (string imageLayerName in parameters.ImageOptions.Keys)
                {
                    PhotopeaLayer layer = data.GetLayerByName(imageLayerName) ?? throw new ApiException($"Layer not found: {imageLayerName}", HttpStatusCode.BadRequest);
                    layerIds.Add(imageLayerName, layer.Id);
                }

                foreach (string imageLayerName in parameters.ImageOptions.Keys)
                {
                    ImageOptions imageOptions = parameters.ImageOptions[imageLayerName];

                    if (imageOptions.ImageFile == null)
                        throw new ApiException($"Missing image file for image options for layer: {imageLayerName}", HttpStatusCode.BadRequest);

                    await photopea.SelectLayerByNameAsync(imageLayerName);
                    await photopea.SetVisibilityOfActiveLayerAsync(false);

                    string newLayerName = imageLayerName + "_new";

                    using (CancellationTokenSource cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(30)))
                    using (Stream imageStream = imageOptions.ImageFile.OpenReadStream())
                        await photopea.InsertFileAsync(imageStream, newLayerName, CancellationToken.None);

                    PhotopeaDocumentData documentData = await photopea.GetDocumentDataAsync();

                    PhotopeaLayer? oldLayer = documentData.GetLayerById(layerIds[imageLayerName]);

                    if (oldLayer == null)
                        throw new ApiException($"Failed to get layer by id for layer: {imageLayerName}. (Id: {layerIds[imageLayerName]})", HttpStatusCode.InternalServerError);

                    PhotopeaLayer? newLayer = documentData.GetLayerByName(newLayerName);

                    if (newLayer == null)
                        throw new ApiException($"Failed to get layer by name for layer: {newLayer}", HttpStatusCode.InternalServerError);

                    await photopea.ScaleToOtherLayerAsync(newLayer, oldLayer);
                    await photopea.MoveToOtherLayerAsync(newLayer, oldLayer);

                    if (imageOptions.Mirror)
                        await photopea.FlipActiveLayerHorizontallyAsync();

                    if (imageOptions.ShiftX != 0 || imageOptions.ShiftY != 0)
                        await photopea.TranslateActiveLayerAsync(imageOptions.ShiftX, imageOptions.ShiftY);
                }
            }
        }
    }
}

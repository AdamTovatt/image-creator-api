using ImageCreatorApi.FileSystems;
using ImageCreatorApi.Models.Photoshop;
using PhotopeaNet;
using PhotopeaNet.Models;

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
    }
}

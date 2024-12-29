using ImageCreatorApi.Models.Photoshop;
using Sakur.WebApiUtilities.BaseClasses;
using Sakur.WebApiUtilities.Models;
using System.Net;
using System.Text.Json.Serialization;

namespace ImageCreatorApi.Models
{
    public class ExportParameters : RequestBody
    {
        [Required]
        [JsonPropertyName("fileName")]
        public string FileName { get; set; }

        [Required]
        [JsonPropertyName("textOptions")]
        public Dictionary<string, string> TextOptions { get; set; }

        [JsonPropertyName("imageOptions")]
        public Dictionary<string, ImageOptions>? ImageOptions { get; set; }

        public override bool Valid => ValidateByRequiredAttributes();

        public ExportParameters(string fileName, Dictionary<string, string> textOptions, Dictionary<string, ImageOptions> imageOptions)
        {
            FileName = fileName;
            TextOptions = textOptions;
            ImageOptions = imageOptions;
        }

        public void AddImageFiles(List<IFormFile> imageFiles)
        {
            if (imageFiles.Count == 0)
                return;

            if (ImageOptions == null)
                throw new ApiException($"Tried to add {imageFiles.Count} image file(s) but no image options existed. Not sure what to do with image. Make sure each image file provided has a matching option.", HttpStatusCode.BadRequest);

            if (ImageOptions.Count < imageFiles.Count)
                throw new ApiException($"Tried to add {imageFiles.Count} image file(s) but only {ImageOptions.Count} image option(s) existed. Need to provide an image option for each image file.", HttpStatusCode.BadRequest);

            foreach (IFormFile file in imageFiles)
            {
                ImageOptions? imageOption = ImageOptions.Values.Where(x => x.FileName == file.FileName).FirstOrDefault();

                if (imageOption == null)
                    throw new ApiException($"Found an image file {file.FileName} but no image option that specifies what to do with the file. Check you image option file names.", HttpStatusCode.BadRequest);

                imageOption.ImageFile = file;
            }
        }

        public PsdFilePath GetPsdFilePath()
        {
            return new PsdFilePath(FileName);
        }
    }
}

using ImageCreatorApi.Factories;
using ImageCreatorApi.FileSystems;
using ImageCreatorApi.Models;
using ImageCreatorApi.Models.Photoshop;
using PhotopeaNet;
using PhotopeaNet.Models;
using PhotopeaNet.Models.ImageSaving;
using WebApiUtilities.TaskScheduling;

namespace ImageCreatorApi.Helpers
{
    public class PhotoshopMetadataHelper
    {
        private const int thumbnailSize = 100;

        public static async Task CreateMetadataAsync(PsdFilePath filePath)
        {
            PsdFileMetadataPath metadataFilePathObject = new PsdFileMetadataPath(filePath.FileName);
            string metadataFilePath = metadataFilePathObject.ToString();

            IFileSystem fileSystem = FileSystemFactory.GetInstance();

            if (await fileSystem.FileExistsAsync(metadataFilePath)) // Previous metadata file exists, let's clean that up first
            {
                PhotoshopFileMetadata metadata = await PhotoshopFileMetadata.ReadAsync(from: fileSystem, withFilePath: metadataFilePathObject);

                await fileSystem.DeleteFileAsync(metadataFilePath);
                await SimpleCloudinaryHelper.Instance.DeleteFileAsync(metadata.ThumbnailUrl);
            }

            using (Stream fileStream = await fileSystem.ReadFileAsync(filePath.ToString()))
            using (Photopea photopea = PhotopeaFactory.GetInstance())
            {
                await photopea.StartAsync();
                await photopea.LoadFileFromStreamAsync(fileStream);

                List<PhotopeaLayer> photopeaLayes = await photopea.GetAllLayersAsync();

                int width = 1080; // TODO: Get the actual width from the PSD file
                int height = 1080; // TODO: Get the actual height from the PSD file

                await photopea.LoadFonts(from: fileSystem, fonts: await photopea.GetRequiredFonts(photopeaLayes), suppressFontNotFoundExceptions: true);

                await photopea.ResizeImage(thumbnailSize, thumbnailSize);

                using (MemoryStream memoryStream = new MemoryStream(await photopea.SaveImageAsync(new SaveJpgOptions(80))))
                {
                    string url = await SimpleCloudinaryHelper.Instance.UploadFileAsync(new ThumbnailFilePath(filePath.FileName), memoryStream);

                    PhotoshopFileMetadata photoshopFileMetadata = new PhotoshopFileMetadata(url, photopeaLayes.ToPhotoshopLayers(), width, height, memoryStream.Length);

                    using (MemoryStream metadataStream = new MemoryStream(photoshopFileMetadata.ToUtf8EncondedJsonBytes()))
                    {
                        await fileSystem.WriteFileAsync(metadataFilePath, metadataStream);
                    }
                }
            }

            BackgroundTaskQueue.Instance.QueueTask(new BuildPhotoshopFilesCacheTask());
        }
    }
}

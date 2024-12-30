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
        private const int thumbnailSize = 64;

        public static async Task CreateMetadataAsync(PsdFilePath filePath, bool backgroundTask)
        {
            PsdFileMetadataPath metadataFilePathObject = new PsdFileMetadataPath(filePath.FileName);
            string metadataFilePath = metadataFilePathObject.ToString();

            IFileSystem fileSystem = FileSystemFactory.GetInstance();

            if (await fileSystem.FileExistsAsync(metadataFilePath)) // Previous metadata file exists, let's clean that up first
            {
                PhotoshopFileMetadata metadata = await PhotoshopFileMetadata.ReadAsync(from: fileSystem, withFilePath: metadataFilePathObject);

                await fileSystem.DeleteFileAsync(metadataFilePath);
                await SimpleCloudinaryHelper.Instance.DeleteFileAsync(metadata.ThumbnailUrl);
                await SimpleCloudinaryHelper.Instance.DeleteFileAsync(metadata.PreviewUrl);
            }

            using (Stream fileStream = await fileSystem.ReadFileAsync(filePath.ToString()))
            using (Photopea photopea = PhotopeaFactory.GetInstance())
            {
                await photopea.StartAsync();
                await photopea.LoadFileFromStreamAsync(fileStream);

                PhotopeaDocumentData documentData = await photopea.GetDocumentDataAsync();

                await photopea.LoadFonts(from: fileSystem, fonts: documentData.RequiredFonts, suppressFontNotFoundExceptions: true);

                string? thumbnailUrl = null;
                string? fullsizePreviewUrl = null;

                using (MemoryStream memoryStream = new MemoryStream(await photopea.SaveImageAsync(new SaveJpgOptions(80))))
                    fullsizePreviewUrl = await SimpleCloudinaryHelper.Instance.UploadFileAsync(new FullSizePreviewFilePath(filePath.FileName), memoryStream);

                await photopea.ResizeImage(thumbnailSize, thumbnailSize);

                using (MemoryStream memoryStream = new MemoryStream(await photopea.SaveImageAsync(new SaveJpgOptions(80))))
                    thumbnailUrl = await SimpleCloudinaryHelper.Instance.UploadFileAsync(new ThumbnailFilePath(filePath.FileName), memoryStream);

                if (thumbnailUrl == null || fullsizePreviewUrl == null)
                    throw new Exception("Failed to upload thumbnail or fullsize preview to cloudinary");

                List<PhotoshopLayer> layers = documentData.FlattenedLayers.ToPhotoshopLayers();
                PhotoshopFileMetadata photoshopFileMetadata = new PhotoshopFileMetadata(
                    thumbnailUrl,
                    fullsizePreviewUrl,
                    layers,
                    documentData.Width,
                    documentData.Height,
                    fileStream.Length);

                using (MemoryStream metadataStream = new MemoryStream(photoshopFileMetadata.ToUtf8EncondedJsonBytes()))
                    await fileSystem.WriteFileAsync(metadataFilePath, metadataStream);
            }

            BuildPhotoshopFilesCacheTask buildPhotoshopFilesCacheTask = new BuildPhotoshopFilesCacheTask();

            if (backgroundTask)
                BackgroundTaskQueue.Instance.QueueTask(buildPhotoshopFilesCacheTask);
            else
                await buildPhotoshopFilesCacheTask.ExecuteAsync(CancellationToken.None);
        }

        public static async Task RemoveMetadataAsync(PsdFilePath filePath)
        {
            PsdFileMetadataPath metadataFilePathObject = new PsdFileMetadataPath(filePath.FileName);
            string metadataFilePath = metadataFilePathObject.ToString();

            IFileSystem fileSystem = FileSystemFactory.GetInstance();

            if (await fileSystem.FileExistsAsync(metadataFilePath))
            {
                PhotoshopFileMetadata metadata = await PhotoshopFileMetadata.ReadAsync(from: fileSystem, withFilePath: metadataFilePathObject);

                await fileSystem.DeleteFileAsync(metadataFilePath);
                await SimpleCloudinaryHelper.Instance.DeleteFileAsync(metadata.ThumbnailUrl);
                await SimpleCloudinaryHelper.Instance.DeleteFileAsync(metadata.PreviewUrl);
            }
        }
    }
}

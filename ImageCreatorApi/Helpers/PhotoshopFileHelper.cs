using ImageCreatorApi.FileSystems;
using ImageCreatorApi.Models.Photoshop;

namespace ImageCreatorApi.Helpers
{
    public class PhotoshopFileHelper
    {
        private static List<PhotoshopFileInfo>? cachedFileInfos = null;
        private static readonly SemaphoreSlim semaphore = new SemaphoreSlim(1, 1);

        public static async Task<List<PhotoshopFileInfo>> GetAllFilesAsync(IFileSystem from)
        {
            if (cachedFileInfos == null)
            {
                await semaphore.WaitAsync();
                try
                {
                    if (cachedFileInfos == null) // Double-checked locking
                        cachedFileInfos = await FetchAllFilesAsync(from);
                }
                finally
                {
                    semaphore.Release();
                }
            }

            return cachedFileInfos;
        }

        public static async Task ClearCacheAsync()
        {
            await semaphore.WaitAsync();
            try
            {
                cachedFileInfos = null;
            }
            finally
            {
                semaphore.Release();
            }
        }

        private static async Task<List<PhotoshopFileInfo>> FetchAllFilesAsync(IFileSystem from)
        {
            string psdDirectoryPath = new PsdFilePath("file.psd").GetDirectoryPath();

            IReadOnlyList<string> filePaths = await from.ListFilesAsync(psdDirectoryPath);

            List<string> metadataFiles = new List<string>();
            List<string> files = new List<string>();

            foreach (string filePath in filePaths)
            {
                if (PsdFileMetadataPath.GetIsMetadataPath(filePath))
                    metadataFiles.Add(filePath);
                else
                    files.Add(filePath);
            }

            List<PhotoshopFileInfo> photoshopFiles = new List<PhotoshopFileInfo>();

            foreach (string file in files)
            {
                PsdFileMetadataPath metadataPath = new PsdFileMetadataPath(file);

                PhotoshopFileMetadata? photoshopFileMetadata = null;
                if (metadataFiles.Contains(metadataPath.FileName))
                    photoshopFileMetadata = await PhotoshopFileMetadata.ReadAsync(from: from, withFilePath: metadataPath);

                photoshopFiles.Add(new PhotoshopFileInfo(file, photoshopFileMetadata));
            }

            return photoshopFiles;
        }
    }
}

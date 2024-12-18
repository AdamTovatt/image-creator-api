using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using ImageCreatorApi.Models;
using System.Text;

namespace ImageCreatorApi.Storage
{
    public class CloudinaryFileSystem : IFileSystem
    {
        private readonly Cloudinary cloudinary;
        private readonly int maxFileSize;

        public CloudinaryFileSystem(string cloudName, string apiKey, string apiSecret, int maxFileSize = 10485759) // cloudinary free plan max file size (-1 for safety)
        {
            Account account = new Account(cloudName, apiKey, apiSecret);
            cloudinary = new Cloudinary(account);
            this.maxFileSize = maxFileSize;
        }

        public async Task<bool> FolderExistsAsync(string path)
        {
            if (string.IsNullOrEmpty(path))
                throw new Exception("Path parameter was empty when specifying what folder to check existance for.");

            if (path.Contains('\\'))
                throw new Exception("Invalid character in path: \"\\\"");

            string[] parts = path.Split('/');

            string rootPath = parts[0];
            GetFoldersResult result = await cloudinary.RootFoldersAsync();

            if (!result.Folders.Any(folder => folder.Path == rootPath))
                return false;

            StringBuilder subFolderPathBuilder = new StringBuilder(rootPath);
            for (int i = 1; i < parts.Length; i++)
            {
                GetFoldersResult subFolders = await cloudinary.SubFoldersAsync(subFolderPathBuilder.ToString());

                subFolderPathBuilder.Append("/");
                subFolderPathBuilder.Append(parts[i]);

                if (!subFolders.Folders.Any(x => x.Path == subFolderPathBuilder.ToString()))
                    return false;
            }

            return true;
        }

        public async Task<IReadOnlyList<string>> ListFilesAsync(string folderPath)
        {
            ListResourcesResult result = await cloudinary.ListResourceByAssetFolderAsync(folderPath, false, false, false);

            return result.Resources.Select(x => x.AssetId).ToList().AsReadOnly();
        }

        public async Task<IReadOnlyList<string>> ListFoldersAsync(string folderPath)
        {
            GetFoldersResult result = await cloudinary.SubFoldersAsync(folderPath);
            List<string> folders = result.Folders.Select(folder => folder.Path).ToList();

            return folders;
        }

        public async Task CreateFolderAsync(string folderPath)
        {
            CreateFolderResult createFolderResult = await cloudinary.CreateFolderAsync(folderPath);

            if (!createFolderResult.Success && createFolderResult.Error != null)
                throw new Exception($"Error when creating folder: {createFolderResult.Error.Message}");
        }

        public async Task DeleteFileAsync(string filePath)
        {
            DelResParams deleteParams = new DelResParams
            {
                PublicIds = new List<string> { filePath }
            };

            await cloudinary.DeleteResourcesAsync(deleteParams);
        }

        public async Task DeleteFolderAsync(string folderPath, bool recursive)
        {
            await cloudinary.DeleteFolderAsync(folderPath);
        }

        public async Task<bool> FileExistsAsync(string path)
        {
            GetResourceParams getResourceParams = new GetResourceParams(path);
            GetResourceResult result = await cloudinary.GetResourceAsync(getResourceParams);
            return result.StatusCode == System.Net.HttpStatusCode.OK;
        }

        public async Task<byte[]> ReadFileAsync(string filePath)
        {
            GetResourceParams getResourceParams = new GetResourceParams(filePath);
            GetResourceResult result = await cloudinary.GetResourceAsync(getResourceParams);

            if (!string.IsNullOrEmpty(result.Error.Message))
                throw new FileNotFoundException(result.Error.Message);

            HttpClient client = new HttpClient();
            byte[] data = await client.GetByteArrayAsync(result.SecureUrl);

            return data;
        }

        public async Task WriteFileAsync(string filePath, Stream dataStream)
        {
            int chunkIndex = 1;
            string? folderPath = Path.GetDirectoryName(filePath)?.Replace('\\', '/');
            List<string> uploadedChunks = new List<string>();

            try
            {
                while (dataStream.Position < dataStream.Length)
                {
                    using (SubStream chunkStream = new SubStream(dataStream, maxFileSize))
                    {
                        string chunkFilePath = $"{filePath}_chunk_{chunkIndex:D3}";

                        RawUploadParams uploadParams = new RawUploadParams
                        {
                            AssetFolder = folderPath,
                            File = new FileDescription(chunkFilePath, chunkStream),
                            PublicId = chunkFilePath
                        };

                        RawUploadResult uploadResult = await cloudinary.UploadAsync(uploadParams);

                        if (uploadResult.Error != null)
                            throw new Exception($"Error when writing file: {uploadResult.Error.Message}");

                        uploadedChunks.Add(chunkFilePath);

                        chunkIndex++;
                    }
                }
            }
            catch
            {
                foreach (string chunk in uploadedChunks)
                {
                    await cloudinary.DeleteResourcesAsync(new DelResParams { PublicIds = new List<string> { chunk } });
                }
                throw;
            }
        }
    }
}

using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using ImageCreatorApi.Models.FilePaths;
using System.Text;

namespace ImageCreatorApi.FileSystems
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
            List<string> allFiles = new List<string>();
            string? nextCursor = null;

            do
            {
                ListResourcesByAssetFolderParams parameters = new ListResourcesByAssetFolderParams
                {
                    AssetFolder = folderPath,
                    MaxResults = 500, // Max allowed value for cloudinary
                    NextCursor = nextCursor
                };

                ListResourcesResult result = await cloudinary.ListResourcesAsync(parameters);

                if (result.Resources != null)
                    allFiles.AddRange(result.Resources.Select(x => x.PublicId));

                nextCursor = result.NextCursor;

            } while (!string.IsNullOrEmpty(nextCursor));

            return allFiles.AsReadOnly();
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

            if (result.StatusCode == System.Net.HttpStatusCode.OK) return true; // File exists as single file

            getResourceParams = new GetResourceParams(FilePath.AddChunkNumber(path, 1)); // Let's check if it exists as a chunked file (by checking if the first chunk exists)
            getResourceParams.ResourceType = ResourceType.Raw;
            result = await cloudinary.GetResourceAsync(getResourceParams);

            return result.StatusCode == System.Net.HttpStatusCode.OK;
        }

        public async Task<Stream> ReadFileAsync(string filePath)
        {
            List<string> chunkPaths = new List<string>();

            GetResourceParams getResourceParams = new GetResourceParams(filePath);
            getResourceParams.ResourceType = ResourceType.Raw;
            GetResourceResult result = await cloudinary.GetResourceAsync(getResourceParams);

            if (result.StatusCode == System.Net.HttpStatusCode.OK)
            {
                // Single file exists, return its stream
                HttpClient client = new HttpClient();
                return await client.GetStreamAsync(result.SecureUrl);
            }

            // If the single file doesn't exist, check for chunks
            int chunkIndex = 1;
            while (true)
            {
                string chunkFilePath = FilePath.AddChunkNumber(filePath, chunkIndex);
                getResourceParams = new GetResourceParams(chunkFilePath);
                getResourceParams.ResourceType = ResourceType.Raw;

                result = await cloudinary.GetResourceAsync(getResourceParams);

                if (result.StatusCode != System.Net.HttpStatusCode.OK)
                    break; // No more chunks found, exit the loop

                chunkPaths.Add(result.SecureUrl);
                chunkIndex++;
            }

            if (chunkPaths.Count == 0)
                throw new FileNotFoundException("File or file chunks not found.");

            return new UrlChunkStream(chunkPaths);
        }

        public async Task WriteFileAsync(string filePath, Stream dataStream)
        {
            int chunkNumber = 1;
            string? folderPath = Path.GetDirectoryName(filePath)?.Replace('\\', '/');
            List<string> uploadedChunks = new List<string>();

            try
            {
                while (dataStream.Position < dataStream.Length)
                {
                    using (SubStream chunkStream = new SubStream(dataStream, maxFileSize))
                    {
                        string chunkFilePath = FilePath.AddChunkNumber(filePath, chunkNumber);

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

                        chunkNumber++;
                    }
                }
            }
            catch
            {
                foreach (string chunk in uploadedChunks)
                    await cloudinary.DeleteResourcesAsync(new DelResParams { PublicIds = new List<string> { chunk } });

                throw;
            }
        }
    }
}

﻿using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
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
                    MaxResults = 500, // Max allowed value for Cloudinary
                    NextCursor = nextCursor
                };

                ListResourcesResult result = await cloudinary.ListResourcesAsync(parameters);

                if (result.Resources != null)
                    allFiles.AddRange(result.Resources.Select(x => x.PublicId));

                nextCursor = result.NextCursor;

            } while (!string.IsNullOrEmpty(nextCursor));

            // Filter out chunk files and rename chunkinfo files to original file names
            List<string> filteredFiles = new List<string>();

            foreach (string file in allFiles)
            {
                if (FilePath.IsChunkFile(file))
                    continue; // Skip chunk files

                if (FilePath.IsChunkInfoFile(file))
                {
                    // Convert chunkinfo file name back to the original file name
                    filteredFiles.Add(Path.GetFileName(FilePath.RemoveChunkInfoSuffix(file)));
                }
                else
                {
                    filteredFiles.Add(Path.GetFileName(file));
                }
            }

            return filteredFiles.AsReadOnly();
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
            List<string> publicIdsToDelete = new List<string>();

            string chunkInfoFilePath = FilePath.AddChunkInfoSuffix(filePath);
            publicIdsToDelete.Add(chunkInfoFilePath);

            GetResourceParams getResourceParams = new GetResourceParams(chunkInfoFilePath)
            {
                ResourceType = ResourceType.Raw
            };

            GetResourceResult result = await cloudinary.GetResourceAsync(getResourceParams);

            if (result.StatusCode != System.Net.HttpStatusCode.OK)
                throw new FileNotFoundException($"Chunk info file for {filePath} not found.");

            using HttpClient client = new HttpClient();
            string chunkInfoJson = await client.GetStringAsync(result.SecureUrl);
            ChunkInfo chunkInfo = ChunkInfo.FromJson(chunkInfoJson);

            publicIdsToDelete.AddRange(chunkInfo.Chunks.Select(chunk => chunk.PublicId));

            DelResParams deleteParams = new DelResParams
            {
                PublicIds = publicIdsToDelete,
                ResourceType = ResourceType.Raw
            };

            DelResResult deleteResult = await cloudinary.DeleteResourcesAsync(deleteParams);

            if (!deleteResult.Deleted.All(x => x.Value == "deleted"))
                throw new Exception($"Failed to delete file or part of file!");
        }

        public async Task DeleteFolderAsync(string folderPath, bool recursive)
        {
            await cloudinary.DeleteFolderAsync(folderPath);
        }

        public async Task<bool> FileExistsAsync(string path)
        {
            string chunkInfoFilePath = FilePath.AddChunkInfoSuffix(path);

            GetResourceParams getResourceParams = new GetResourceParams(chunkInfoFilePath)
            {
                ResourceType = ResourceType.Raw
            };

            GetResourceResult result = await cloudinary.GetResourceAsync(getResourceParams);

            return result.StatusCode == System.Net.HttpStatusCode.OK;
        }

        public async Task<Stream> ReadFileAsync(string filePath)
        {
            string chunkInfoFilePath = FilePath.AddChunkInfoSuffix(filePath);

            GetResourceParams getResourceParams = new GetResourceParams(chunkInfoFilePath)
            {
                ResourceType = ResourceType.Raw
            };

            GetResourceResult result = await cloudinary.GetResourceAsync(getResourceParams);

            if (result.StatusCode != System.Net.HttpStatusCode.OK)
                throw new FileNotFoundException($"Chunk info for file at {filePath} not found.");

            using HttpClient client = new HttpClient();
            string chunkInfoJson = await client.GetStringAsync(result.SecureUrl);

            return new UrlChunkStream(ChunkInfo.FromJson(chunkInfoJson));
        }

        public async Task WriteFileAsync(string filePath, Stream dataStream)
        {
            string? folderPath = Path.GetDirectoryName(filePath)?.Replace('\\', '/');
            List<ChunkInfoRow> chunkInfoRows = new List<ChunkInfoRow>();

            try
            {
                long totalFileLength = dataStream.Length;
                int chunkNumber = 1;

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

                        chunkInfoRows.Add(new ChunkInfoRow(uploadResult.PublicId, uploadResult.SecureUrl.ToString()));

                        chunkNumber++;
                    }
                }

                // Create and upload ChunkInfo metadata
                ChunkInfo chunkInfo = new ChunkInfo(totalFileLength, chunkInfoRows.Count, chunkInfoRows);
                string chunkInfoFilePath = FilePath.AddChunkInfoSuffix(filePath);

                using (MemoryStream chunkInfoStream = new MemoryStream(Encoding.UTF8.GetBytes(chunkInfo.ToJson())))
                {
                    RawUploadParams chunkInfoUploadParams = new RawUploadParams
                    {
                        AssetFolder = folderPath,
                        File = new FileDescription(chunkInfoFilePath, chunkInfoStream),
                        PublicId = chunkInfoFilePath
                    };

                    RawUploadResult chunkInfoUploadResult = await cloudinary.UploadAsync(chunkInfoUploadParams);

                    if (chunkInfoUploadResult.Error != null)
                        throw new Exception($"Error when writing chunk info: {chunkInfoUploadResult.Error.Message}");
                }
            }
            catch
            {
                // Cleanup uploaded chunks in case of failure
                foreach (ChunkInfoRow chunkInfoRow in chunkInfoRows)
                    await cloudinary.DeleteResourcesAsync(new DelResParams { PublicIds = new List<string> { chunkInfoRow.PublicId } });

                throw;
            }
        }
    }
}

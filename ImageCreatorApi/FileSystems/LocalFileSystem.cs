using System.Security.Cryptography;

namespace ImageCreatorApi.FileSystems
{
    public class LocalFileSystem : IFileSystem
    {
        private readonly int maxFileSize;
        private readonly string basePath;

        public LocalFileSystem(int maxFileSize, string basePath)
        {
            this.maxFileSize = maxFileSize;
            this.basePath = basePath;
        }

        public Task CreateFolderAsync(string folderPath)
        {
            string fullPath = Path.Combine(basePath, folderPath);
            if (!Directory.Exists(fullPath))
            {
                try
                {
                    Directory.CreateDirectory(fullPath);
                }
                catch (UnauthorizedAccessException exception)
                {
                    throw new IOException($"Failed to create directory '{fullPath}' due to insufficient permissions.", exception);
                }
                catch (Exception exception)
                {
                    throw new IOException($"An unexpected error occurred while creating directory '{fullPath}'.", exception);
                }
            }
            return Task.CompletedTask;
        }

        public Task DeleteFileAsync(string filePath)
        {
            string fullPath = Path.Combine(basePath, filePath);
            if (File.Exists(fullPath))
            {
                try
                {
                    File.Delete(fullPath);
                }
                catch (IOException exception)
                {
                    throw new IOException($"Failed to delete file '{fullPath}'. It may be in use by another process.", exception);
                }
            }
            return Task.CompletedTask;
        }

        public Task DeleteFolderAsync(string folderPath, bool recursive)
        {
            string fullPath = Path.Combine(basePath, folderPath);
            if (Directory.Exists(fullPath))
            {
                try
                {
                    Directory.Delete(fullPath, recursive);
                }
                catch (UnauthorizedAccessException exception)
                {
                    throw new IOException($"Failed to delete directory '{fullPath}' due to insufficient permissions.", exception);
                }
                catch (DirectoryNotFoundException exception)
                {
                    throw new IOException($"Directory not found: '{fullPath}'.", exception);
                }
                catch (IOException exception)
                {
                    throw new IOException($"An error occurred while deleting directory '{fullPath}'.", exception);
                }
            }
            return Task.CompletedTask;
        }

        public Task<bool> FileExistsAsync(string path)
        {
            string fullPath = Path.Combine(basePath, path);
            bool exists = File.Exists(fullPath);
            return Task.FromResult(exists);
        }

        public Task<bool> FolderExistsAsync(string path)
        {
            string fullPath = Path.Combine(basePath, path);
            bool exists = Directory.Exists(fullPath);
            return Task.FromResult(exists);
        }

        public Task<IReadOnlyList<string>> ListFilesAsync(string folderPath)
        {
            string fullPath = Path.Combine(basePath, folderPath);
            if (!Directory.Exists(fullPath))
                throw new DirectoryNotFoundException($"Folder not found: {fullPath}");

            List<string> files = Directory.GetFiles(fullPath).ToList();
            return Task.FromResult<IReadOnlyList<string>>(files);
        }

        public Task<IReadOnlyList<string>> ListFoldersAsync(string folderPath)
        {
            string fullPath = Path.Combine(basePath, folderPath);
            if (!Directory.Exists(fullPath))
                throw new DirectoryNotFoundException($"Folder not found: {fullPath}");

            List<string> folders = Directory.GetDirectories(fullPath).ToList();
            return Task.FromResult<IReadOnlyList<string>>(folders);
        }

        public Task<Stream> ReadFileAsync(string filePath)
        {
            string fullPath = Path.Combine(basePath, filePath);
            if (!File.Exists(fullPath))
                throw new FileNotFoundException($"File not found: {fullPath}");

            FileStream fileStream = new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.Read);
            return Task.FromResult<Stream>(fileStream);
        }

        public async Task<string> WriteFileAsync(string filePath, Stream dataStream)
        {
            if (dataStream.Length > maxFileSize)
            {
                throw new IOException($"File size exceeds the maximum allowed size of {maxFileSize} bytes.");
            }

            string? partialDirectoryPath = Path.GetDirectoryName(filePath);
            if (partialDirectoryPath != null)
            {
                string? directory = Path.Combine(basePath, partialDirectoryPath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }
            }

            string fullPath = Path.Combine(basePath, filePath);

            using (SHA256 sha256 = SHA256.Create())
            using (FileStream fileStream = new FileStream(fullPath, FileMode.Create, FileAccess.Write))
            {
                byte[] buffer = new byte[81920]; // 80 KB buffer
                int bytesRead;

                while ((bytesRead = await dataStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                {
                    sha256.TransformBlock(buffer, 0, bytesRead, buffer, 0);
                    await fileStream.WriteAsync(buffer, 0, bytesRead);
                }

                // Complete the hash computation
                sha256.TransformFinalBlock(Array.Empty<byte>(), 0, 0);

                if (sha256.Hash == null)
                    throw new IOException("Failed to compute SHA-256 hash of the file.");

                // Convert hash to a hex string
                return BitConverter.ToString(sha256.Hash).Replace("-", "").ToLowerInvariant();
            }
        }
    }
}

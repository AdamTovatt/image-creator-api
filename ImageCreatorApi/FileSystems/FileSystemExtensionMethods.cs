namespace ImageCreatorApi.FileSystems
{
    public static class FileSystemExtensionMethods
    {
        public static async Task EnsureDirectoryOfFileExistsAsync(this IFileSystem fileSystem, FilePath filePath)
        {
            bool directoryExists = await fileSystem.FolderExistsAsync(filePath.GetDirectoryPath());
            if (!directoryExists)
            {
                for (int i = 0; i < filePath.SubdirectoryDepth; i++)
                {
                    string subDirectoryPath = filePath.GetDirectoryPath(i);
                    bool subDirectoryExists = await fileSystem.FolderExistsAsync(subDirectoryPath);

                    if (!subDirectoryExists)
                        await fileSystem.CreateFolderAsync(subDirectoryPath);
                }
            }
        }
    }
}

namespace ImageCreatorApi.FileSystems
{
    public interface IFileSystem
    {
        Task<bool> FileExistsAsync(string path);
        Task<bool> FolderExistsAsync(string path);
        Task<IReadOnlyList<string>> ListFilesAsync(string folderPath);
        Task<IReadOnlyList<string>> ListFoldersAsync(string folderPath);
        Task CreateFolderAsync(string folderPath);
        Task DeleteFileAsync(string filePath);
        Task DeleteFolderAsync(string folderPath, bool recursive);
        Task<Stream> ReadFileAsync(string filePath);
        Task WriteFileAsync(string filePath, Stream dataStream);
    }
}

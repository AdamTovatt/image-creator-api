namespace ImageCreatorApi.Managers
{
    public interface IFileManager
    {
        public Task SaveAsync(byte[] bytes, string path);
        public Task<byte[]> ReadSingleAsync(string path);
        public Task<List<byte[]>> ReadAllAtPathAsync(string path);
    }
}

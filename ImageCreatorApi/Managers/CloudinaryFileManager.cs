using CloudinaryDotNet;
using CloudinaryDotNet.Actions;

namespace ImageCreatorApi.Managers
{
    public class CloudinaryFileManager : IFileManager
    {
        private readonly Cloudinary _cloudinary;

        public CloudinaryFileManager(string cloudName, string apiKey, string apiSecret)
        {
            Account account = new Account(cloudName, apiKey, apiSecret);
            _cloudinary = new Cloudinary(account);
        }

        public async Task<List<byte[]>> ReadAllAtPathAsync(string path)
        {
            List<Resource> resources = await ListResourcesAsync(path);
            List<byte[]> files = new List<byte[]>();

            foreach (Resource resource in resources)
            {
                byte[] data = await DownloadFileAsync(resource.Url.ToString());
                files.Add(data);
            }

            return files;
        }

        public async Task<byte[]> ReadSingleAsync(string path)
        {
            byte[] data = await DownloadFileAsync(path);
            return data;
        }

        public async Task SaveAsync(byte[] bytes, string path)
        {
            using (MemoryStream memoryStream = new MemoryStream(bytes))
            {
                ImageUploadParams uploadParams = new ImageUploadParams
                {
                    File = new FileDescription(path, memoryStream),
                    PublicId = Path.GetFileNameWithoutExtension(path),
                    Overwrite = true
                };

                await _cloudinary.UploadAsync(uploadParams);
            }
        }

        private async Task<List<Resource>> ListResourcesAsync(string folderPath)
        {
            ListResourcesByPrefixParams listParams = new ListResourcesByPrefixParams
            {
                Type = "upload",
                Prefix = folderPath
            };

            ListResourcesResult result = await _cloudinary.ListResourcesAsync(listParams);
            return result.Resources.ToList();
        }

        private async Task<byte[]> DownloadFileAsync(string url)
        {
            using (HttpClient client = new HttpClient())
            {
                byte[] data = await client.GetByteArrayAsync(url);
                return data;
            }
        }
    }
}

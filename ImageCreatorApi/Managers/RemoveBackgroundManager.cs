using ImageCreatorApi.Storage;

namespace ImageCreatorApi.Managers
{
    public class RemoveBackgroundManager : IDisposable
    {
        public IFileSystem fileManager;
        private HttpClient client;
        private string apiKey;

        public RemoveBackgroundManager(IFileSystem fileManager, string apiKey)
        {
            this.fileManager = fileManager;
            this.apiKey = apiKey;
            client = new HttpClient();
        }

        public void Dispose()
        {
            client.Dispose();
        }

        public async Task<byte[]> RemoveBackgroundAsync(byte[] image)
        {
            using (MultipartFormDataContent formData = new MultipartFormDataContent())
            {
                formData.Headers.Add("X-Api-Key", apiKey);
                formData.Add(new ByteArrayContent(image), "image_file", "file.jpg");
                formData.Add(new StringContent("preview"), "size");
                HttpResponseMessage response = await client.PostAsync("https://api.remove.bg/v1.0/removebg", formData);

                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadAsByteArrayAsync();
                }
                else
                {
                    string errorMessage = await response.Content.ReadAsStringAsync();
                    throw new Exception($"Error when removing background from image: {errorMessage}");
                }
            }
        }
    }
}

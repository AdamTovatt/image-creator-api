using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using ImageCreatorApi.FileSystems;
using Sakur.WebApiUtilities.Helpers;

namespace ImageCreatorApi.Helpers
{
    public class SimpleCloudinaryHelper
    {
        public static SimpleCloudinaryHelper Instance
        {
            get
            {
                if (instance == null) instance = new SimpleCloudinaryHelper();
                return instance;
            }
        }

        private static SimpleCloudinaryHelper? instance;
        private readonly Cloudinary cloudinary;

        public SimpleCloudinaryHelper()
        {
            string cloud = EnvironmentHelper.GetEnvironmentVariable("CLOUDINARY_CLOUD");
            string key = EnvironmentHelper.GetEnvironmentVariable("CLOUDINARY_KEY");
            string secret = EnvironmentHelper.GetEnvironmentVariable("CLOUDINARY_SECRET");

            Account account = new Account(cloud, key, secret);
            cloudinary = new Cloudinary(account);
        }

        /// <summary>
        /// Uploads a file to Cloudinary using a FilePath instance and returns the secure URL.
        /// </summary>
        /// <param name="filePath">An instance of FilePath or a derived class representing the path.</param>
        /// <param name="dataStream">The file data as a stream.</param>
        /// <returns>The secure URL of the uploaded file.</returns>
        public async Task<string> UploadFileAsync(FilePath filePath, Stream dataStream)
        {
            string cloudinaryPath = filePath.ToString();

            RawUploadParams uploadParams = new RawUploadParams
            {
                AssetFolder = filePath.GetDirectoryPath(),
                File = new FileDescription(cloudinaryPath, dataStream),
                PublicId = cloudinaryPath
            };

            RawUploadResult uploadResult = await cloudinary.UploadAsync(uploadParams);

            if (uploadResult.Error != null)
                throw new Exception($"Error uploading file: {uploadResult.Error.Message}");

            return uploadResult.SecureUrl.ToString();
        }

        /// <summary>
        /// Deletes a file from Cloudinary based on its secure URL.
        /// </summary>
        /// <param name="secureUrl">The secure URL of the file to delete.</param>
        public async Task DeleteFileAsync(string secureUrl)
        {
            string publicId = GetPublicIdFromSecureUrl(secureUrl);

            DelResParams deleteParams = new DelResParams
            {
                PublicIds = new List<string> { publicId },
                ResourceType = ResourceType.Raw
            };

            DelResResult deleteResult = await cloudinary.DeleteResourcesAsync(deleteParams);

            if (!deleteResult.Deleted.All(x => x.Value == "deleted"))
                throw new Exception($"Failed to delete file with public ID: {publicId}");
        }

        /// <summary>
        /// Extracts the public ID from a Cloudinary secure URL.
        /// </summary>
        /// <param name="secureUrl">The secure URL of the file.</param>
        /// <returns>The public ID of the file.</returns>
        private string GetPublicIdFromSecureUrl(string secureUrl)
        {
            Uri uri = new Uri(secureUrl);
            string path = uri.AbsolutePath; // Example: /<cloud_name>/raw/upload/v1234567/folder/file_name

            // Find the part after "/upload/" and remove the versioning part (e.g., "v1234567/")
            string[] segments = path.Split('/');
            int uploadIndex = Array.IndexOf(segments, "upload");

            if (uploadIndex == -1 || uploadIndex + 1 >= segments.Length)
                throw new ArgumentException("Invalid secure URL format.");

            // Combine the segments after "upload" and skip the version part (e.g., "v1234567")
            string publicId = string.Join('/', segments.Skip(uploadIndex + 2));

            // Remove the file extension if present
            publicId = Path.Combine(Path.GetDirectoryName(publicId) ?? "", Path.GetFileNameWithoutExtension(publicId)).Replace("\\", "/");

            return publicId;
        }
    }
}

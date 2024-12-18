using ImageCreatorApi.FileSystems;
using Sakur.WebApiUtilities.Helpers;

namespace ImageCreatorApi.Factories
{
    public class FileSystemFactory : IFactory<IFileSystem>
    {
        private static IFileSystem? instance;

        public static IFileSystem GetInstance()
        {
            if (instance == null)
            {
                string cloud = EnvironmentHelper.GetEnvironmentVariable("CLOUDINARY_CLOUD");
                string key = EnvironmentHelper.GetEnvironmentVariable("CLOUDINARY_KEY");
                string secret = EnvironmentHelper.GetEnvironmentVariable("CLOUDINARY_SECRET");

                instance = new CloudinaryFileSystem(cloud, key, secret);
            }

            return instance;
        }
    }
}

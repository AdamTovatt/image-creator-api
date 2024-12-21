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
                string cloud = EnvironmentHelper.GetEnvironmentVariable(StringConstants.CloudinaryCloud);
                string key = EnvironmentHelper.GetEnvironmentVariable(StringConstants.CloudinaryKey);
                string secret = EnvironmentHelper.GetEnvironmentVariable(StringConstants.CloudinarySecret);

                instance = new CloudinaryFileSystem(cloud, key, secret);
            }

            return instance;
        }
    }
}

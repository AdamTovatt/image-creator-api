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
                string cacheBasePathPath = EnvironmentHelper.GetEnvironmentVariable(StringConstants.LocalFileSystemCacheBasePath);
                LocalFileSystem localFileSystem = new LocalFileSystem(1024 * 1024 * 1024, cacheBasePathPath); // 1 GB

                string cloud = EnvironmentHelper.GetEnvironmentVariable(StringConstants.CloudinaryCloud);
                string key = EnvironmentHelper.GetEnvironmentVariable(StringConstants.CloudinaryKey);
                string secret = EnvironmentHelper.GetEnvironmentVariable(StringConstants.CloudinarySecret);

                instance = new CloudinaryFileSystem(cloud, key, secret, cacheFileSystem: localFileSystem);
            }

            return instance;
        }
    }
}

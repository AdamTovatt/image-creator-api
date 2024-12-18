using ImageCreatorApi.Managers;

namespace ImageCreatorApi.Factories
{
    public class FileManagerFactory : IFactory<IFileManager>
    {
        private IFileManager? instance;

        public IFileManager GetInstance()
        {
            if (instance == null)
            {
                instance = new CloudinaryFileManager();
            }

            return instance;
        }
    }
}

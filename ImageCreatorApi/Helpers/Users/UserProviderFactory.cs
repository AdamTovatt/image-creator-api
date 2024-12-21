using ImageCreatorApi.Factories;

namespace ImageCreatorApi.Helpers.Users
{
    public class UserProviderFactory : IFactory<IUserProvider>
    {
        private static IUserProvider? instance;

        public static IUserProvider GetInstance()
        {
            if (instance == null)
                instance = new FileSystemUserProvider();

            return instance;
        }
    }
}

using ImageCreatorApi.Factories;
using ImageCreatorApi.FileSystems;
using ImageCreatorApi.Models;

namespace ImageCreatorApi.Helpers.Users
{
    public class FileSystemUserProvider : IUserProvider
    {
        private readonly IFileSystem fileSystem;
        private readonly string usersFilePath;
        private readonly FilePath usersFilePathObject;
        private List<User>? userCache;
        private readonly object cacheLock = new object();

        public FileSystemUserProvider()
        {
            fileSystem = FileSystemFactory.GetInstance();
            usersFilePathObject = new ConfigurationFilePath("users.txt");
            usersFilePath = usersFilePathObject.ToString();
        }

        public async Task<IEnumerable<User>> GetAllUsersAsync()
        {
            await EnsureCacheAsync();
            return userCache ?? Enumerable.Empty<User>();
        }

        public async Task<User> GetUserByEmailAsync(string email)
        {
            await EnsureCacheAsync();
            User? user = userCache?.FirstOrDefault(user => user.Email.Equals(email, StringComparison.OrdinalIgnoreCase));
            if (user == null)
                throw new KeyNotFoundException($"User with email '{email}' not found.");
            return user;
        }

        public async Task<User> GetUserByIdAsync(int id)
        {
            await EnsureCacheAsync();
            User? user = userCache?.FirstOrDefault(user => user.Id == id);
            if (user == null)
                throw new KeyNotFoundException($"User with ID '{id}' not found.");
            return user;
        }

        public async Task RebuildCacheAsync()
        {
            lock (cacheLock) userCache = null;

            if (!await fileSystem.FileExistsAsync(usersFilePath))
            {
                await fileSystem.EnsureDirectoryOfFileExistsAsync(usersFilePathObject);

                using MemoryStream memoryStream = new MemoryStream();
                using StreamWriter writer = new StreamWriter(memoryStream);
                writer.Write("no users yet");
                writer.Flush();
                memoryStream.Seek(0, SeekOrigin.Begin);

                await fileSystem.WriteFileAsync(usersFilePath, memoryStream);

                throw new FileNotFoundException($"The file '{usersFilePath}' did not exist. Created a new one.");
            }

            List<User> users = new List<User>();

            await using (Stream fileStream = await fileSystem.ReadFileAsync(usersFilePath))
            using (StreamReader reader = new StreamReader(fileStream))
            {
                string? line;
                while ((line = await reader.ReadLineAsync()) != null)
                    users.Add(User.FromTableRow(line));
            }

            lock (cacheLock) userCache = users;
        }

        private async Task EnsureCacheAsync()
        {
            if (userCache == null) await RebuildCacheAsync();
        }
    }
}

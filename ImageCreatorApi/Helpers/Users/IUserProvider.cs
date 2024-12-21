using ImageCreatorApi.Models;

namespace ImageCreatorApi.Helpers.Users
{
    public interface IUserProvider
    {
        public Task<IEnumerable<User>> GetAllUsersAsync();
        public Task<User> GetUserByIdAsync(int id);
        public Task<User> GetUserByEmailAsync(string email);
        public Task RebuildCacheAsync();
    }
}

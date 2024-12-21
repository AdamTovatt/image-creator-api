using Sakur.WebApiUtilities.TaskScheduling;

namespace ImageCreatorApi.Helpers.Users
{
    public class BuildUsersCacheTask : QueuedTaskBase
    {
        public override async Task ExecuteAsync(CancellationToken cancellationToken)
        {
            await UserProviderFactory.GetInstance().RebuildCacheAsync();
        }
    }
}

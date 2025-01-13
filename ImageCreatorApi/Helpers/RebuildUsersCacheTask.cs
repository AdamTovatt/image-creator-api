using ImageCreatorApi.Helpers.Users;
using Sakur.WebApiUtilities.TaskScheduling;

namespace ImageCreatorApi.Helpers
{
    public class RebuildUsersCacheTask : IntervalTask
    {
        public override TimeSpan Interval => TimeSpan.FromHours(3);

        public override TimeSpan InitialStartDelay => TimeSpan.FromHours(1);

        public override async Task ExecuteAsync(CancellationToken cancellationToken)
        {
            await UserProviderFactory.GetInstance().RebuildCacheAsync();
        }
    }
}

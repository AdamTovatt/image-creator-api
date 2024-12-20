using ImageCreatorApi.Factories;
using Sakur.WebApiUtilities.TaskScheduling;

namespace ImageCreatorApi.Helpers
{
    public class BuildPhotoshopFilesCacheTask : QueuedTaskBase
    {
        public override async Task ExecuteAsync(CancellationToken cancellationToken)
        {
            await PhotoshopFileHelper.ClearCacheAsync();
            await PhotoshopFileHelper.GetAllFilesAsync(FileSystemFactory.GetInstance());
        }
    }
}

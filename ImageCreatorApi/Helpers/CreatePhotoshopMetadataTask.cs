using ImageCreatorApi.Models.Photoshop;
using Sakur.WebApiUtilities.TaskScheduling;

namespace ImageCreatorApi.Helpers
{
    public class CreatePhotoshopMetadataTask : QueuedTaskBase
    {
        private string fileName;

        public CreatePhotoshopMetadataTask(string fileName)
        {
            this.fileName = fileName;
        }

        public override async Task ExecuteAsync(CancellationToken cancellationToken)
        {
            await PhotoshopMetadataHelper.CreateMetadataAsync(new PsdFilePath(fileName));
        }
    }
}

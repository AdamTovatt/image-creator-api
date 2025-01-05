using ImageCreatorApi.Models.Photoshop;
using PhotopeaNet.Helpers;
using Sakur.WebApiUtilities.TaskScheduling;

namespace ImageCreatorApi.Helpers
{
    public class CreatePhotoshopMetadataTask : QueuedTaskBase
    {
        private string fileName;
        private PhotopeaConnectionProvider photopeaConnectionProvider;

        public CreatePhotoshopMetadataTask(PhotopeaConnectionProvider photopeaConnectionProvider, string fileName)
        {
            this.photopeaConnectionProvider = photopeaConnectionProvider;
            this.fileName = fileName;
        }

        public override async Task ExecuteAsync(CancellationToken cancellationToken)
        {
            await PhotoshopMetadataHelper.CreateMetadataAsync(photopeaConnectionProvider, new PsdFilePath(fileName), true);
        }
    }
}

using PhotopeaNet.Helpers;

namespace ImageCreatorApi.Models
{
    public class PhotopeaConnectionProviderLifecycle : IHostedService, IAsyncDisposable
    {
        // This will be used for the file / image transfers to the photopea host, each instance will get a unique port
        private const int photopeaFileTransferBasePort = 41200; // just a random port number

        private readonly PhotopeaConnectionProvider connectionProvider;

        public PhotopeaConnectionProviderLifecycle(PhotopeaConnectionProvider connectionProvider)
        {
            this.connectionProvider = connectionProvider;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            await connectionProvider.StartAsync(photopeaFileTransferBasePort);
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            await connectionProvider.DisposeAsync();
        }

        public ValueTask DisposeAsync()
        {
            return connectionProvider.DisposeAsync();
        }
    }
}

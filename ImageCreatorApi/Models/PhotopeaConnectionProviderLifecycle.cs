using PhotopeaNet.Helpers;

namespace ImageCreatorApi.Models
{
    public class PhotopeaConnectionProviderLifecycle : IHostedService, IAsyncDisposable
    {
        private readonly PhotopeaConnectionProvider connectionProvider;

        public PhotopeaConnectionProviderLifecycle(PhotopeaConnectionProvider connectionProvider)
        {
            this.connectionProvider = connectionProvider;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            await connectionProvider.StartAsync();
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

namespace ImageCreatorApi.FileSystems
{
    public class UrlChunkStream : Stream
    {
        private readonly Queue<string> _chunkUrls;
        private Stream _currentChunkStream;
        private readonly HttpClient _httpClient;
        private bool _disposed;

        public UrlChunkStream(IEnumerable<string> chunkUrls)
        {
            _chunkUrls = new Queue<string>(chunkUrls);
            _httpClient = new HttpClient();
            _currentChunkStream = Null;
        }

        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => throw new NotSupportedException();
        public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }

        public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            int bytesRead = await _currentChunkStream.ReadAsync(buffer, offset, count, cancellationToken);

            while (bytesRead == 0 && _chunkUrls.Count > 0)
            {
                await _currentChunkStream.DisposeAsync();
                string nextChunkUrl = _chunkUrls.Dequeue();
                _currentChunkStream = await _httpClient.GetStreamAsync(nextChunkUrl);
                bytesRead = await _currentChunkStream.ReadAsync(buffer, offset, count, cancellationToken);
            }

            return bytesRead;
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            return ReadAsync(buffer, offset, count).GetAwaiter().GetResult();
        }

        public override void Flush() => throw new NotSupportedException();
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

        protected override void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    _currentChunkStream.Dispose();
                    _httpClient.Dispose();
                }

                _disposed = true;
            }

            base.Dispose(disposing);
        }
    }
}

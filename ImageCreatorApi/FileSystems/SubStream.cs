namespace ImageCreatorApi.FileSystems
{
    public class SubStream : Stream
    {
        private readonly Stream baseStream;
        private readonly long chunkSize;
        private long remainingBytes;

        public SubStream(Stream baseStream, int chunkSize)
        {
            this.baseStream = baseStream;
            this.chunkSize = chunkSize;
            remainingBytes = chunkSize;
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            int bytesToRead = (int)Math.Min(count, remainingBytes);
            int bytesRead = baseStream.Read(buffer, offset, bytesToRead);
            remainingBytes -= bytesRead;
            return bytesRead;
        }

        public override bool CanRead => baseStream.CanRead;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => chunkSize;
        public override long Position { get; set; }

        public override void Flush() => throw new NotSupportedException();
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    }
}

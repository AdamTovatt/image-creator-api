using ImageCreatorApi.FileSystems;

namespace ImageCreatorApi.Models
{
    public class ThumbnailFilePath : FilePath
    {
        public ThumbnailFilePath(string fileName) : base(EnsureFileExtension($"{fileName}_thumbnail.jpg", ".jpg"), "thumbnails") { }
    }
}

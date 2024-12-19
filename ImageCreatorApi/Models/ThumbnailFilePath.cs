using ImageCreatorApi.FileSystems;

namespace ImageCreatorApi.Models
{
    public class ThumbnailFilePath : FilePath
    {
        public ThumbnailFilePath(string fileName) : base(fileName, new List<string>() { "thumbnails" }) { }
    }
}

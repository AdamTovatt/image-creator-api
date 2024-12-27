using ImageCreatorApi.FileSystems;

namespace ImageCreatorApi.Models
{
    public class FullSizePreviewFilePath : FilePath
    {
        public FullSizePreviewFilePath(string fileName) : base(EnsureFileExtension($"{fileName}_preview.jpg", "jpg"), "previews") { }
    }
}

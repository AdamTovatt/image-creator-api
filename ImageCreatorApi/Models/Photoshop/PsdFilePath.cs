using ImageCreatorApi.FileSystems;

namespace ImageCreatorApi.Models.Photoshop
{
    public class PsdFilePath : FilePath
    {
        public PsdFilePath(string fileName)
            : base(
                EnsureFileExtension(fileName, ".psd"),
                new List<string> { "photoshop" }
            )
        { }
    }
}

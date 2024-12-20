using ImageCreatorApi.FileSystems;

namespace ImageCreatorApi.Models.Photoshop
{
    public class FontFilePath : FilePath
    {
        public FontFilePath(string fileName) : base(fileName, "fonts") { }
    }
}

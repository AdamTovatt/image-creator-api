using ImageCreatorApi.FileSystems;

namespace ImageCreatorApi.Models.Photoshop
{
    public class PsdFileMetadataPath : FilePath
    {
        public const string Suffix = "_metadata";

        public PsdFileMetadataPath(string fileName) : base($"{fileName}{Suffix}", "photoshop") { }

        public static bool GetIsMetadataPath(string path)
        {
            return path.EndsWith(Suffix);
        }
    }
}

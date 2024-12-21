using ImageCreatorApi.FileSystems;

namespace ImageCreatorApi.Models
{
    public class ConfigurationFilePath : FilePath
    {
        public ConfigurationFilePath(string fileName) : base(fileName, "configuration") { }
    }
}

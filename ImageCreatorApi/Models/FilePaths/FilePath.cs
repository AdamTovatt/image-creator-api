using System.Text;
using System.Text.RegularExpressions;

namespace ImageCreatorApi.Models.FilePaths
{
    public abstract class FilePath
    {
        public int SubdirectoryDepth => parts.Count;
        public string FileName { get; protected set; }
        protected List<string> parts;

        private static readonly char[] invalidChars = Path.GetInvalidFileNameChars();

        protected FilePath(string fileName, List<string> parts)
        {
            FileName = CleanFileName(fileName);
            this.parts = parts;

            parts.Insert(0, "image_creator");
        }

        private string CleanFileName(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName)) throw new ArgumentException("File name cannot be null or empty.", nameof(fileName));

            fileName = Path.GetFileName(fileName);

            string cleanedFileName = Regex.Replace(fileName, $"[{Regex.Escape(new string(invalidChars))}]", "");

            if (string.IsNullOrEmpty(cleanedFileName)) throw new ArgumentException("File name contains only invalid characters.");

            return cleanedFileName;
        }

        public override string ToString()
        {
            StringBuilder pathBuilder = new StringBuilder();

            foreach (string part in parts)
            {
                pathBuilder.Append(part);
                pathBuilder.Append("/");
            }

            pathBuilder.Append(FileName);

            return pathBuilder.ToString();
        }

        public string GetDirectoryPath(int maxSubdirectoryDepth)
        {
            return InternalGetDirectoryPath(maxSubdirectoryDepth);
        }

        public string GetDirectoryPath()
        {
            return InternalGetDirectoryPath(null);
        }

        protected string InternalGetDirectoryPath(int? maxSubdirectoryDepth)
        {
            StringBuilder pathBuilder = new StringBuilder();

            int appendedParts = 0;
            foreach (string part in parts)
            {
                pathBuilder.Append(part);
                pathBuilder.Append("/");

                appendedParts++;
                if (maxSubdirectoryDepth != null && appendedParts > maxSubdirectoryDepth)
                    break;
            }

            pathBuilder.Length -= 1;

            return pathBuilder.ToString();
        }

        protected static string EnsureFileExtension(string fileName, string extension)
        {
            if (string.IsNullOrWhiteSpace(fileName) || string.IsNullOrWhiteSpace(extension))
                throw new ArgumentException("File name and extension cannot be null or empty.");

            if (!extension.StartsWith("."))
                extension = "." + extension;

            if (Path.GetExtension(fileName).Equals(extension, StringComparison.OrdinalIgnoreCase))
                return fileName;

            return fileName + extension;
        }

        public static string AddChunkNumber(string filePath, int index)
        {
            return $"{filePath}_chunk_{index:D3}";
        }
    }
}

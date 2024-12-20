using System.Text;
using System.Text.RegularExpressions;

namespace ImageCreatorApi.FileSystems
{
    public abstract class FilePath
    {
        private const string chunkInfoSuffix = "_chunkinfo";
        private const string chunkNumberMiddlePart = "_chunk_";
        private const string chunkNumberFormat = "{0}_chunk_{1:D3}";
        private static readonly int chunkSuffixLength = chunkNumberMiddlePart.Length + 3;

        public int SubdirectoryDepth => parts.Count;
        public string FileName { get; protected set; }
        protected List<string> parts;

        private static readonly char[] invalidChars = Path.GetInvalidFileNameChars();

        protected FilePath(string fileName, params string[] parts)
        {
            FileName = CleanFileName(fileName);
            this.parts = parts.ToList();

            this.parts.Insert(0, "image_creator");
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

        public string GetFileNameWithoutExtension()
        {
            return Path.GetFileNameWithoutExtension(FileName);
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
            return string.Format(chunkNumberFormat, filePath, index);
        }

        public static string AddChunkInfoSuffix(string filePath)
        {
            return $"{filePath}{chunkInfoSuffix}";
        }

        public static bool IsChunkFile(string filePath)
        {
            if (filePath.Length < chunkSuffixLength)
                return false;

            return filePath.Substring(filePath.Length - chunkSuffixLength, chunkNumberMiddlePart.Length) == chunkNumberMiddlePart &&
                   char.IsDigit(filePath[filePath.Length - 3]) &&
                   char.IsDigit(filePath[filePath.Length - 2]) &&
                   char.IsDigit(filePath[filePath.Length - 1]);
        }

        public static bool IsChunkInfoFile(string filePath)
        {
            return filePath.EndsWith(chunkInfoSuffix);
        }

        public static string RemoveChunkInfoSuffix(string filePath)
        {
            return filePath.Replace(chunkInfoSuffix, "");
        }
    }
}

using ImageCreatorApi.Factories;
using ImageCreatorApi.Managers;
using ImageCreatorApi.Models.FilePaths;
using ImageCreatorApi.FileSystems;
using Microsoft.AspNetCore.Mvc;
using Sakur.WebApiUtilities.Models;
using System.Net;

namespace ImageCreatorApi.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class PsdFilesController : ControllerBase
    {
        private readonly ILogger<PsdFilesController> _logger;

        public PsdFilesController(ILogger<PsdFilesController> logger)
        {
            _logger = logger;
        }

        [HttpPost("upload")]
        public async Task<IActionResult> Upload(IFormFile psdFile)
        {
            IFileSystem fileSystem = FileSystemFactory.GetInstance();
            PsdFilePath filePath = new PsdFilePath(psdFile.FileName);

            bool directoryExists = await fileSystem.FolderExistsAsync(filePath.GetDirectoryPath());
            if (!directoryExists)
            {
                for (int i = 0; i < filePath.SubdirectoryDepth; i++)
                {
                    string subDirectoryPath = filePath.GetDirectoryPath(i);
                    bool subDirectoryExists = await fileSystem.FolderExistsAsync(subDirectoryPath);

                    if (!subDirectoryExists)
                        await fileSystem.CreateFolderAsync(subDirectoryPath);
                }
            }

            string filePathString = filePath.ToString();
            if (await fileSystem.FileExistsAsync(filePathString))
                return new ApiResponse("File already exists! If you want to replace it, use the update method instead!", HttpStatusCode.BadRequest);

            using (Stream fileStream = psdFile.OpenReadStream())
            {
                await fileSystem.WriteFileAsync(filePathString, fileStream);
            }

            return new ApiResponse("ok");
        }

        [HttpGet("download")]
        public async Task<IActionResult> Download(string fileName)
        {
            IFileSystem fileSystem = FileSystemFactory.GetInstance();
            string filePath = new PsdFilePath(fileName).ToString();

            bool fileExists = await fileSystem.FileExistsAsync(filePath);
            if (!fileExists)
                return new ApiResponse("File not found!", HttpStatusCode.NotFound);

            try
            {
                Stream fileStream = await fileSystem.ReadFileAsync(filePath);
                string contentType = "application/photoshop";
                string fileDownloadName = Path.GetFileName(filePath);

                return File(fileStream, contentType, fileDownloadName);
            }
            catch (Exception ex)
            {
                return new ApiResponse($"Error downloading file: {ex.Message}", HttpStatusCode.InternalServerError);
            }
        }
    }
}

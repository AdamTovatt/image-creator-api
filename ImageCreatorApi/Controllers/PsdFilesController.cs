using ImageCreatorApi.Factories;
using ImageCreatorApi.Managers;
using ImageCreatorApi.Models.FilePaths;
using ImageCreatorApi.Storage;
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
    }
}

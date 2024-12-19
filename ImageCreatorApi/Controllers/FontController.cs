using ImageCreatorApi.Factories;
using ImageCreatorApi.FileSystems;
using ImageCreatorApi.Helpers;
using ImageCreatorApi.Models.Photoshop;
using Microsoft.AspNetCore.Mvc;
using Sakur.WebApiUtilities.Models;
using System.Net;
using WebApiUtilities.TaskScheduling;

namespace ImageCreatorApi.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class FontController : ControllerBase
    {
        [HttpPost("upload")]
        public async Task<IActionResult> Upload(IFormFile fontFile)
        {
            IFileSystem fileSystem = FileSystemFactory.GetInstance();
            FontFilePath filePath = new FontFilePath(fontFile.FileName);

            await fileSystem.EnsureDirectoryOfFileExistsAsync(filePath);

            string filePathString = filePath.ToString();
            if (await fileSystem.FileExistsAsync(filePathString))
                return new ApiResponse("File already exists! If you want to replace it, use the update method instead!", HttpStatusCode.BadRequest);

            using (Stream fileStream = fontFile.OpenReadStream())
                await fileSystem.WriteFileAsync(filePathString, fileStream);

            return new ApiResponse("File was uploaded.");
        }
    }
}

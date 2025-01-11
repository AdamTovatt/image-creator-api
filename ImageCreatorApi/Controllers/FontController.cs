using ImageCreatorApi.Factories;
using ImageCreatorApi.FileSystems;
using ImageCreatorApi.Models.Photoshop;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sakur.WebApiUtilities.Models;
using Sakur.WebApiUtilities.RateLimiting;
using System.Net;

namespace ImageCreatorApi.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class FontController : ControllerBase
    {
        [Authorize]
        [HttpPost("upload")]
        [Limit(MaxRequests = 5, TimeWindow = 60)]
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

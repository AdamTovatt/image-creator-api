using ImageCreatorApi.Factories;
using ImageCreatorApi.Managers;
using ImageCreatorApi.FileSystems;
using Microsoft.AspNetCore.Mvc;
using Sakur.WebApiUtilities.Models;
using System.Net;
using ImageCreatorApi.Models.Photoshop;
using ImageCreatorApi.Helpers;
using WebApiUtilities.TaskScheduling;
using ImageCreatorApi.Models;
using PhotopeaNet;
using PhotopeaNet.Models;
using PhotopeaNet.Models.ImageSaving;

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

            await fileSystem.EnsureDirectoryOfFileExistsAsync(filePath);

            string filePathString = filePath.ToString();
            if (await fileSystem.FileExistsAsync(filePathString))
                return new ApiResponse("File already exists! If you want to replace it, use the update method instead!", HttpStatusCode.BadRequest);

            using (Stream fileStream = psdFile.OpenReadStream())
                await fileSystem.WriteFileAsync(filePathString, fileStream);

            BackgroundTaskQueue.Instance.QueueTask(new CreatePhotoshopMetadataTask(filePath.FileName));

            return new ApiResponse("File was uploaded.");
        }

        [HttpPost("update")]
        public async Task<IActionResult> Update(IFormFile psdFile)
        {
            IFileSystem fileSystem = FileSystemFactory.GetInstance();
            PsdFilePath filePath = new PsdFilePath(psdFile.FileName);

            await fileSystem.EnsureDirectoryOfFileExistsAsync(filePath);

            string filePathString = filePath.ToString();
            if (!await fileSystem.FileExistsAsync(filePathString))
                return new ApiResponse("File doesn't exists! If you want to upload a new file, use the upload method instead!", HttpStatusCode.BadRequest);

            await fileSystem.DeleteFileAsync(filePathString);

            using (Stream fileStream = psdFile.OpenReadStream())
                await fileSystem.WriteFileAsync(filePathString, fileStream);

            return new ApiResponse("File was updated.");
        }

        [HttpPost("export-with-parameters")]
        public async Task<IActionResult> ExportWithParameters([FromBody] ExportParameters exportParameters)
        {
            using (Photopea photopea = PhotopeaFactory.GetInstance())
            {
                await photopea.StartAsync();

                IFileSystem fileSystem = FileSystemFactory.GetInstance();

                using (Stream fileStream = await fileSystem.ReadFileAsync(new PsdFilePath(exportParameters.FileName).ToString()))
                    await photopea.LoadFileFromStreamAsync(fileStream);

                await photopea.LoadFonts(from: fileSystem, fonts: await photopea.GetRequiredFonts(), suppressFontNotFoundExceptions: false);

                foreach (string textLayerName in exportParameters.Texts.Keys)
                    await photopea.SetTextValueAsync(textLayerName, exportParameters.Texts[textLayerName]);

                byte[] exportedBytes = await photopea.SaveImageAsync(new SaveJpgOptions(100));

                return File(exportedBytes, "application/jpeg", Path.GetFileNameWithoutExtension(exportParameters.FileName) + ".jpg");
            }
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

        [HttpPost("create-metadata")]
        public async Task<IActionResult> CreateMetadata(string fileName)
        {
            await Task.CompletedTask;
            BackgroundTaskQueue.Instance.QueueTask(new CreatePhotoshopMetadataTask(fileName));
            return new ApiResponse("Ok");
        }

        [HttpGet("list")]
        public async Task<IActionResult> List()
        {
            IFileSystem fileSystem = FileSystemFactory.GetInstance();

            return new ApiResponse(await PhotoshopFileHelper.GetAllFilesAsync(fileSystem));
        }
    }
}

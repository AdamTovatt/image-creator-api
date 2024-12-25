using ImageCreatorApi.Factories;
using ImageCreatorApi.FileSystems;
using Microsoft.AspNetCore.Mvc;
using Sakur.WebApiUtilities.Models;
using System.Net;
using ImageCreatorApi.Models.Photoshop;
using ImageCreatorApi.Helpers;
using WebApiUtilities.TaskScheduling;
using ImageCreatorApi.Models;
using PhotopeaNet;
using PhotopeaNet.Models.ImageSaving;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;

namespace ImageCreatorApi.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class PsdController : ControllerBase
    {
        private readonly ILogger<PsdController> _logger;

        public PsdController(ILogger<PsdController> logger)
        {
            _logger = logger;
        }

        [Authorize]
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

        [Authorize]
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

            BackgroundTaskQueue.Instance.QueueTask(new CreatePhotoshopMetadataTask(filePath.FileName));

            return new ApiResponse("File was updated.");
        }

        [Authorize]
        [HttpPost("export-with-parameters")]
        public async Task<IActionResult> ExportWithParameters([FromForm] string parametersJson, [FromForm] List<IFormFile> imageFiles)
        {
            try
            {
                ExportParameters? parameters = JsonSerializer.Deserialize<ExportParameters>(parametersJson);

                if (parameters == null)
                    return new ApiResponse("Missing a parametersJson text field in form body that can be deserialized to an intance of ExportParameters");

                parameters.AddImageFiles(imageFiles);

                IFileSystem fileSystem = FileSystemFactory.GetInstance();
                PsdFilePath psdFilePath = parameters.GetPsdFilePath();

                using (Photopea photopea = await PhotopeaFactory.StartNewInstanceAsync())
                {
                    await photopea.LoadFullProjectFile(fileSystem, psdFilePath);

                    await photopea.ApplyExportParameters(parameters);

                    byte[] exportedBytes = await photopea.SaveImageAsync(new SaveJpgOptions(100));

                    return File(exportedBytes, "application/jpeg", $"{psdFilePath.GetFileNameWithoutExtension()}.jpg");
                }
            }
            catch (ApiException apiException)
            {
                return new ApiResponse(apiException);
            }
        }

        [Authorize]
        [HttpDelete("delete")]
        public async Task<IActionResult> Delete(string fileName)
        {
            IFileSystem fileSystem = FileSystemFactory.GetInstance();

            PsdFilePath psdFilePath = new PsdFilePath(fileName);
            PsdFileMetadataPath psdFileMetadataPath = new PsdFileMetadataPath(fileName);

            if (!await fileSystem.FileExistsAsync(psdFilePath.ToString()))
                return new ApiResponse($"No such file found: {fileName}");

            if (await fileSystem.FileExistsAsync(psdFileMetadataPath.ToString()))
            {
                PhotoshopFileMetadata metadata = await PhotoshopFileMetadata.ReadAsync(from: fileSystem, psdFileMetadataPath);
                await SimpleCloudinaryHelper.Instance.DeleteFileAsync(metadata.ThumbnailUrl);
                await fileSystem.DeleteFileAsync(psdFileMetadataPath.ToString());
            }

            await fileSystem.DeleteFileAsync(psdFilePath.ToString());

            BackgroundTaskQueue.Instance.QueueTask(new BuildPhotoshopFilesCacheTask());

            return new ApiResponse("File was deleted");
        }

        [Authorize]
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

        [Authorize]
        [HttpPost("create-metadata")]
        public async Task<IActionResult> CreateMetadata(string fileName, bool inBackground)
        {
            if (inBackground)
                BackgroundTaskQueue.Instance.QueueTask(new CreatePhotoshopMetadataTask(fileName));
            else
                await PhotoshopMetadataHelper.CreateMetadataAsync(new PsdFilePath(fileName));

            return new ApiResponse("Ok");
        }

        [Authorize]
        [HttpGet("list")]
        public async Task<IActionResult> List()
        {
            IFileSystem fileSystem = FileSystemFactory.GetInstance();

            return new ApiResponse(await PhotoshopFileHelper.GetAllFilesAsync(fileSystem));
        }
    }
}

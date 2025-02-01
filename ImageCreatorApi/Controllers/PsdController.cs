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
using PhotopeaNet.Helpers;
using Sakur.WebApiUtilities.RateLimiting;
using System.ComponentModel.DataAnnotations;

namespace ImageCreatorApi.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class PsdController : ControllerBase
    {
        private readonly ILogger<PsdController> _logger;
        private readonly PhotopeaConnectionProvider photopeaConnectionProvider;

        public PsdController(ILogger<PsdController> logger, PhotopeaConnectionProvider photopeaConnectionProvider)
        {
            _logger = logger;
            this.photopeaConnectionProvider = photopeaConnectionProvider;
        }

        [Authorize]
        [HttpPost("upload")]
        [RequestSizeLimit(1024 * 1024 * 1024)]
        [RequestFormLimits(MultipartBodyLengthLimit = 1024 * 1024 * 1024)]
        [Limit(MaxRequests = 4, TimeWindow = 60)]
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

            BackgroundTaskQueue.Instance.QueueTask(new CreatePhotoshopMetadataTask(photopeaConnectionProvider, filePath.FileName));

            return new ApiResponse("File was uploaded.");
        }

        [Authorize]
        [HttpPost("update")]
        [RequestSizeLimit(1024 * 1024 * 1024)]
        [RequestFormLimits(MultipartBodyLengthLimit = 1024 * 1024 * 1024)]
        [Limit(MaxRequests = 4, TimeWindow = 60)]
        public async Task<IActionResult> Update(IFormFile psdFile)
        {
            IFileSystem fileSystem = FileSystemFactory.GetInstance();
            PsdFilePath filePath = new PsdFilePath(psdFile.FileName);

            await fileSystem.EnsureDirectoryOfFileExistsAsync(filePath);

            string filePathString = filePath.ToString();
            if (!await fileSystem.FileExistsAsync(filePathString))
                return new ApiResponse("File doesn't exist! If you want to upload a new file, use the upload method instead!", HttpStatusCode.BadRequest);

            await fileSystem.DeleteFileAsync(filePathString);

            using (Stream fileStream = psdFile.OpenReadStream())
                await fileSystem.WriteFileAsync(filePathString, fileStream);

            BackgroundTaskQueue.Instance.QueueTask(new CreatePhotoshopMetadataTask(photopeaConnectionProvider, filePath.FileName));

            return new ApiResponse("File was updated.");
        }

        [Authorize]
        [HttpPost("export-with-parameters")]
        [Limit(MaxRequests = 15, TimeWindow = 60)]
        public async Task<IActionResult> ExportWithParameters([FromForm] string parametersJson, [FromForm] List<IFormFile> imageFiles, bool getAsPsd)
        {
            try
            {
                ExportParameters? parameters = JsonSerializer.Deserialize<ExportParameters>(parametersJson);

                if (parameters == null)
                    return new ApiResponse("Missing a parametersJson text field in form body that can be deserialized to an instance of ExportParameters");

                if (!parameters.Valid)
                    return new ApiResponse(parameters.GetInvalidBodyMessage());

                parameters.AddImageFiles(imageFiles);

                IFileSystem fileSystem = FileSystemFactory.GetInstance();
                PsdFilePath psdFilePath = parameters.GetPsdFilePath();

                SaveImageOptions saveImageOptions = getAsPsd ? new SavePsdOptions(true) : new SaveJpgOptions(100);
                string contentType = getAsPsd ? "application/psd" : "application/jpeg";

                using (PhotopeaConnection connection = await photopeaConnectionProvider.GetConnectionAsync())
                {
                    Photopea photopea = connection.ConnectedPhotopea;
                    await photopea.LoadFullProjectFile(fileSystem, psdFilePath);

                    await photopea.ApplyExportParameters(parameters);

                    byte[] exportedBytes = await photopea.SaveImageAsync(saveImageOptions);

                    return File(exportedBytes, "application/jpeg", $"{psdFilePath.GetFileNameWithoutExtension()}.{saveImageOptions.FileFormat}");
                }
            }
            catch (ApiException apiException)
            {
                return new ApiResponse(apiException);
            }
            catch
            {
                return new ApiResponse("An unknown error occurred when exporting the image. It might work if you try again.", HttpStatusCode.InternalServerError);
            }
        }

        [Authorize]
        [HttpDelete("delete")]
        [Limit(MaxRequests = 10, TimeWindow = 60)]
        public async Task<IActionResult> Delete(string fileName)
        {
            IFileSystem fileSystem = FileSystemFactory.GetInstance();

            PsdFilePath psdFilePath = new PsdFilePath(fileName);

            if (!await fileSystem.FileExistsAsync(psdFilePath.ToString()))
                return new ApiResponse($"No such file found: {fileName}");

            await PhotoshopMetadataHelper.RemoveMetadataAsync(psdFilePath);

            await fileSystem.DeleteFileAsync(psdFilePath.ToString());

            BackgroundTaskQueue.Instance.QueueTask(new BuildPhotoshopFilesCacheTask());

            return new ApiResponse("File was deleted");
        }

        [Authorize]
        [HttpGet("download")]
        [Limit(MaxRequests = 30, TimeWindow = 60)]
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
        [Limit(MaxRequests = 20, TimeWindow = 60)]
        public async Task<IActionResult> CreateMetadata(string fileName, bool inBackground)
        {
            try
            {
                if (inBackground)
                    BackgroundTaskQueue.Instance.QueueTask(new CreatePhotoshopMetadataTask(photopeaConnectionProvider, fileName));
                else
                    await PhotoshopMetadataHelper.CreateMetadataAsync(photopeaConnectionProvider, new PsdFilePath(fileName), false);
            }
            catch (Exception exception)
            {
                return new ApiResponse($"An error occurred when creating metadata: {exception.Message}", HttpStatusCode.InternalServerError);
            }

            return new ApiResponse("Ok");
        }

        [Authorize]
        [HttpGet("list")]
        [Limit(MaxRequests = 100, TimeWindow = 60)]
        public async Task<IActionResult> List()
        {
            IFileSystem fileSystem = FileSystemFactory.GetInstance();

            return new ApiResponse(await PhotoshopFileHelper.GetAllFilesAsync(fileSystem));
        }
    }
}

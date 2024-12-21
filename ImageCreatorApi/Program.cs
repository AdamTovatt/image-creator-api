using ImageCreatorApi.Helpers;
using ImageCreatorApi.Helpers.Users;
using Sakur.WebApiUtilities;
using Sakur.WebApiUtilities.Helpers;
using Sakur.WebApiUtilities.TaskScheduling;
using WebApiUtilities.TaskScheduling;
    
namespace ImageCreatorApi
{
    public class Program
    {
        public static void Main(string[] args)
        {
            WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

            AssertEnvironmentVariables();

            builder.WebHost.ConfigureKestrel(options => options.Limits.MaxRequestBodySize = 1024 * 1024 * 1024);

            builder.Services.AddControllers();
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();
            builder.Services.AddQueuedTaskProcessing();

            builder.Services.SetupAuth(
                authDomain: EnvironmentHelper.GetEnvironmentVariable(StringConstants.JwtIssuer),
                authAudience: EnvironmentHelper.GetEnvironmentVariable(StringConstants.JwtAudience),
                permissions: new List<string>() { "admin" },
                jwtSecretKey: EnvironmentHelper.GetEnvironmentVariable(StringConstants.JwtSecret, 24)
            );

            BackgroundTaskQueue.Instance.QueueTask(new BuildUsersCacheTask());
            BackgroundTaskQueue.Instance.QueueTask(new BuildPhotoshopFilesCacheTask());

            WebApplication app = builder.Build();

            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseHttpsRedirection();

            app.UseAuthentication();
            app.UseAuthorization();

            app.MapControllers();

            app.Run();
        }

        private static void AssertEnvironmentVariables()
        {
            // Cloudinary-related environment variables
            EnvironmentHelper.GetEnvironmentVariable(StringConstants.CloudinaryCloud, 6);
            EnvironmentHelper.GetEnvironmentVariable(StringConstants.CloudinaryKey, 6);
            EnvironmentHelper.GetEnvironmentVariable(StringConstants.CloudinarySecret, 6);

            // Auth-related environment variables
            EnvironmentHelper.GetEnvironmentVariable(StringConstants.MagicLinkSecret, 24);
            EnvironmentHelper.GetEnvironmentVariable(StringConstants.JwtSecret, 24);
            EnvironmentHelper.GetEnvironmentVariable(StringConstants.JwtIssuer, 12);
            EnvironmentHelper.GetEnvironmentVariable(StringConstants.JwtAudience, 12);

            // Email-related environment variables
            EnvironmentHelper.GetEnvironmentVariable(StringConstants.PostmarkApiKey, 12);
            EnvironmentHelper.GetEnvironmentVariable(StringConstants.EmailSender, 8);
        }
    }
}

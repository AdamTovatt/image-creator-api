using Microsoft.AspNetCore.Http.Features;
using Sakur.WebApiUtilities.Helpers;
using Sakur.WebApiUtilities.TaskScheduling;

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

            WebApplication app = builder.Build();

            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseHttpsRedirection();

            app.UseAuthorization();

            app.MapControllers();

            app.Run();
        }

        private static void AssertEnvironmentVariables()
        {
            EnvironmentHelper.GetEnvironmentVariable("CLOUDINARY_CLOUD", 3);
            EnvironmentHelper.GetEnvironmentVariable("CLOUDINARY_KEY", 6);
            EnvironmentHelper.GetEnvironmentVariable("CLOUDINARY_SECRET", 6);
        }
    }
}

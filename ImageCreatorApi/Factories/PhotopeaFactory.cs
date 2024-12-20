using PhotopeaNet;
using PhotopeaNet.Models;

namespace ImageCreatorApi.Factories
{
    public class PhotopeaFactory : IFactory<Photopea>
    {
        public static Photopea GetInstance()
        {
            return new Photopea(new PhotopeaStartInfo(true, 600, 500));
        }

        public async static Task<Photopea> StartNewInstanceAsync()
        {
            bool exception = false;
            Photopea? photopea = null;

            try
            {
                photopea = GetInstance();
                await photopea.StartAsync();

                return photopea;
            }
            catch
            {
                exception = true;
                throw;
            }
            finally
            {
                if (exception)
                    photopea?.Dispose();
            }
        }
    }
}

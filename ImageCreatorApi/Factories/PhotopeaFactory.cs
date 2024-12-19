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
    }
}

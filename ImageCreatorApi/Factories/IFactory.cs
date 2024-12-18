namespace ImageCreatorApi.Factories
{
    public interface IFactory<T>
    {
        public abstract static T GetInstance();
    }
}

namespace ImageCreatorApi.Factories
{
    public interface IFactory<T>
    {
        public T GetInstance();
    }
}

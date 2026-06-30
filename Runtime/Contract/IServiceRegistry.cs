namespace SWeetJelly.Bootstrapper.Contract
{
    public interface IServiceRegistry : IServiceResolver
    {
        void Register<T>(T service) where T : class;
        void Unregister<T>() where T : class;
    }
}
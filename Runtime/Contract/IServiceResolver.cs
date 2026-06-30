namespace SWeetJelly.Bootstrapper.Contract
{
    public interface IServiceResolver
    {
        T Resolve<T>() where T : class;
        bool TryResolve<T>(out T service) where T : class;
    }
}
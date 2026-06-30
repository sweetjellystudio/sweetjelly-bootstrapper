namespace SWeetJelly.Bootstrapper.Framework.Unity
{
    using SWeetJelly.Bootstrapper.Contract;

    public interface IUnityPackageBootstrapper : IPackageBootstrapper
    {
        int BootstrapOrder { get; }

        void Bootstrap(PackageBootstrapContext context);

        void Shutdown(PackageBootstrapContext context);
    }
}
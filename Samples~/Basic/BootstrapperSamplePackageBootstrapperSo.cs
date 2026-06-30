namespace SWeetJelly.Samples.Bootstrapper
{
    using SWeetJelly.Bootstrapper.Framework.Unity;
    using UnityEngine;

    [CreateAssetMenu(
        fileName = "Bootstrapper Sample Package Bootstrapper",
        menuName = "SWeetJelly/Samples/Bootstrapper/Sample Package Bootstrapper")]
    public sealed class BootstrapperSamplePackageBootstrapperSo : PackageBootstrapperSo
    {
        [SerializeField] private string greeting = "Hello from the Bootstrapper sample service.";

        public override string PackageName => "Bootstrapper Sample";

        public override void Bootstrap(PackageBootstrapContext context)
        {
            context.Services.Register<IBootstrapperSampleGreeter>(
                new BootstrapperSampleGreeter(greeting));
        }

        public override void Shutdown(PackageBootstrapContext context)
        {
            context.Services.Unregister<IBootstrapperSampleGreeter>();
        }
    }
}

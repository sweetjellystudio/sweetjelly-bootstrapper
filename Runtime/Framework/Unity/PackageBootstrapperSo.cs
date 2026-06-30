namespace SWeetJelly.Bootstrapper.Framework.Unity
{
    using SWeetJelly.Bootstrapper.Contract;
    using UnityEngine;

    public abstract class PackageBootstrapperSo : ScriptableObject, IUnityPackageBootstrapper
    {
        [SerializeField] private int bootstrapOrder = 100;

        public virtual int BootstrapOrder => bootstrapOrder;
        public abstract string PackageName { get; }

        public abstract void Bootstrap(PackageBootstrapContext context);

        public virtual void Shutdown(PackageBootstrapContext context)
        {
        }
    }
}
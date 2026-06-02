namespace SWeetJelly.Initialiser.Framework
{
    using SWeetJelly.Initialiser.Contract;

#pragma warning disable 0649

    using UnityEngine;

    public abstract class InitModuleSo : ScriptableObject, IInitModule
    {
        [SerializeField] private int installOrder = 100;
        public virtual int InstallOrder => installOrder;

        public abstract string ModuleName {get;}

        public abstract void Install(InitModuleInstallContext context);

        public virtual void Uninstall(InitModuleInstallContext context)
        {
        }
    }
}
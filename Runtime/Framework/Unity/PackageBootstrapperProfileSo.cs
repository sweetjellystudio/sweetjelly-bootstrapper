namespace SWeetJelly.Bootstrapper.Framework.Unity
{
    using System.Collections.Generic;
    using UnityEngine;
    using UnityEngine.Serialization;

    [CreateAssetMenu(
        fileName = "Package Bootstrapper Profile",
        menuName = "SWeetJelly/Bootstrapper/Package Bootstrapper Profile")]
    public class PackageBootstrapperProfileSo : ScriptableObject
    {
        [FormerlySerializedAs("modules")]
        [SerializeField] private PackageBootstrapperSo[] packageBootstrappers;

        public IReadOnlyList<IUnityPackageBootstrapper> PackageBootstrappers => packageBootstrappers;
    }
}
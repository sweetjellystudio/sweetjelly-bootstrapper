namespace SWeetJelly.Bootstrapper.Framework.Unity
{
#pragma warning disable 0649

    using System;
    using System.Collections.Generic;
    using System.Linq;
    using SWeetJelly.Bootstrapper.App;
    using SWeetJelly.Bootstrapper.Contract;
    using UnityEngine;
    using UnityEngine.EventSystems;
    using UnityEngine.Serialization;

    [DefaultExecutionOrder(-999)]
    public sealed class BootstrapperBehaviour : MonoBehaviour
    {
        [FormerlySerializedAs("packageBootstrapperSetting")]
        [SerializeField] private PackageBootstrapperProfileSo packageBootstrapperProfile;
        [SerializeField] private Canvas canvas;
        [SerializeField] private EventSystem eventSystem;

        private readonly ServiceRegistry serviceRegistry = new ServiceRegistry();
        private readonly List<IUnityPackageBootstrapper> bootstrappedPackages = new List<IUnityPackageBootstrapper>();

        private static BootstrapperBehaviour owner;
        private PackageBootstrapContext context;

        public static Canvas Canvas { get; private set; }
        public static GameObject GameObject { get; private set; }
        public static bool IsBootstrapped { get; private set; }
        public static PackageBootstrapperProfileSo PackageBootstrapperProfile { get; private set; }
        public static IServiceResolver Services { get; private set; }
        public static EventHandler Bootstrapped;

        private void Awake()
        {
            Bootstrap();
        }

        private void Bootstrap()
        {
            if (owner != null)
                return;

            owner = this;
            Canvas = canvas;
            GameObject = gameObject;
            PackageBootstrapperProfile = packageBootstrapperProfile;
            Services = serviceRegistry;

            try
            {
                SetupInputModule();
                DontDestroyOnLoad(gameObject);
                context = new PackageBootstrapContext(gameObject, canvas, eventSystem, serviceRegistry);
                BootstrapPackages(context);
            }
            catch
            {
                ShutdownPackages();
                ClearPublishedState();
                throw;
            }

            IsBootstrapped = true;
            Bootstrapped?.Invoke(this, EventArgs.Empty);
        }

        private void SetupInputModule()
        {
            if (eventSystem == null)
            {
                eventSystem = new GameObject("EventSystem").AddComponent<EventSystem>();
                eventSystem.transform.SetParent(transform);
            }

            if (eventSystem.GetComponent<BaseInputModule>() != null)
                return;

            var inputSystemModuleType = Type.GetType("UnityEngine.InputSystem.UI.InputSystemUIInputModule, Unity.InputSystem");

            if (inputSystemModuleType != null)
            {
                eventSystem.gameObject.AddComponent(inputSystemModuleType);
                return;
            }

            eventSystem.gameObject.AddComponent<StandaloneInputModule>();
        }

        private void BootstrapPackages(PackageBootstrapContext context)
        {
            bootstrappedPackages.Clear();

            if (packageBootstrapperProfile == null)
                return;

            var packageBootstrappers = packageBootstrapperProfile.PackageBootstrappers;

            if (packageBootstrappers == null || packageBootstrappers.Count == 0)
                return;

            foreach (var packageBootstrapper in packageBootstrappers
                .Where(packageBootstrapper => packageBootstrapper != null)
                .OrderBy(packageBootstrapper => packageBootstrapper.BootstrapOrder)
                .ThenBy(packageBootstrapper => packageBootstrapper.PackageName))
            {
                Debug.Log($"Bootstrapping package: {packageBootstrapper.PackageName}");

                packageBootstrapper.Bootstrap(context);
                bootstrappedPackages.Add(packageBootstrapper);
            }
        }

        private void OnDestroy()
        {
            if (!ReferenceEquals(owner, this))
                return;

            try
            {
                ShutdownPackages();
            }
            finally
            {
                ClearPublishedState();
            }
        }

        private void ClearPublishedState()
        {
            if (!ReferenceEquals(owner, this))
                return;

            IsBootstrapped = false;
            Services = null;
            PackageBootstrapperProfile = null;
            Canvas = null;
            GameObject = null;
            context = null;
            owner = null;
        }

        private void ShutdownPackages()
        {
            if (context == null)
                return;

            for (var i = bootstrappedPackages.Count - 1; i >= 0; i--)
            {
                var packageBootstrapper = bootstrappedPackages[i];

                if (packageBootstrapper == null)
                    continue;

                try
                {
                    packageBootstrapper.Shutdown(context);
                }
                catch (Exception exception)
                {
                    Debug.LogException(exception, this);
                }
            }

            bootstrappedPackages.Clear();
        }
    }
}

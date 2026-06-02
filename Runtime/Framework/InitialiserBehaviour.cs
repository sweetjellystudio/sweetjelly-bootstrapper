namespace SWeetJelly.Initialiser.Framework
{

#pragma warning disable 0649

    using System;
    using System.Collections.Generic;
    using System.Linq;
    using UnityEngine;
    using UnityEngine.EventSystems;
    using SWeetJelly.Initialiser.Contract;
    using SWeetJelly.Initialiser.App;

#if MODULE_INPUT_SYSTEM
    using UnityEngine.InputSystem.UI;
#endif

    [DefaultExecutionOrder(-999)]
    public sealed class InitialiserBehaviour : MonoBehaviour
    {
        [SerializeField] private ProjectInitSettingSo initSetting;
        [SerializeField] private Canvas canvas;
        [SerializeField] private EventSystem eventSystem;

        private readonly ServiceRegistry serviceRegistry = new ServiceRegistry();

        public static Canvas Canvas { get; private set; }
        public static GameObject GameObject { get; private set; }

        public static bool IsInitialised { get; private set; }
        public static ProjectInitSettingSo InitSetting { get; private set; }
        public static IServiceResolver Services { get; private set; }

        public static EventHandler Initialised;

        private InitModuleInstallContext context;
        private readonly List<InitModuleSo> installedModules = new List<InitModuleSo>();

        private void Awake()
        {
            Initialise();
        }

        private void Initialise()
        {
            if(IsInitialised)
                return;

            Canvas = canvas;
            GameObject = gameObject;
            InitSetting = initSetting;
            Services = serviceRegistry;

            SetupInputModule();

            DontDestroyOnLoad(gameObject);

            context = new InitModuleInstallContext(gameObject, canvas, eventSystem, serviceRegistry);

            InstallModules(context);

            IsInitialised = true;
            Initialised?.Invoke(this, EventArgs.Empty);
        }

        private void SetupInputModule()
        {
            if (eventSystem == null)
            {
                eventSystem = new GameObject("EventSystem").AddComponent<EventSystem>();
                eventSystem.transform.SetParent(transform);
            }

#if MODULE_INPUT_SYSTEM
            eventSystem.gameObject.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
#else
            eventSystem.gameObject.AddComponent<StandaloneInputModule>();
#endif

        }

        private void InstallModules(InitModuleInstallContext context)
        {
            installedModules.Clear();

            if (initSetting == null)
                return;

            var modules = initSetting.Modules;

            if (modules == null || modules.Count == 0)
                return;

            foreach (var module in modules
                .Where(module => module != null)
                .OrderBy(module => module.InstallOrder)
                .ThenBy(module => module.ModuleName))
            {
                Debug.Log($"Installing module: {module.ModuleName}");

                module.Install(context);
                installedModules.Add(module);
            }
        }

        private static int GetModuleOrder(InitModuleSo module)
        {
            var attr = module
                .GetType()
                .GetCustomAttributes(typeof(RegisterModuleAttribute), false)
                .OfType<RegisterModuleAttribute>()
                .FirstOrDefault();

            return attr?.Order ?? 100;
        }

        private void OnDestroy()
        {
            UnInstallModules();
            IsInitialised = false;
        }

        private void UnInstallModules()
        {
            if (context == null)
                return;

            for (int i = installedModules.Count - 1; i >= 0; i--)
            {
                var module = installedModules[i];

                if (module == null)
                    continue;

                module.Uninstall(context);
            }

            installedModules.Clear();
        }
    }
}
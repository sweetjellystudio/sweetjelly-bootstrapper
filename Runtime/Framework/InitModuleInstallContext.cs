namespace SWeetJelly.Initialiser.Framework
{
    using System;
    using UnityEngine;
    using UnityEngine.EventSystems;
    using SWeetJelly.Initialiser.Contract;

    public sealed class InitModuleInstallContext
    {
        public GameObject HolderObject { get; }
        public Canvas Canvas { get; }
        public EventSystem EventSystem { get; }
        public IServiceRegistry Services { get; }

        public InitModuleInstallContext(
            GameObject holderObject,
            Canvas canvas,
            EventSystem eventSystem,
            IServiceRegistry services)
        {
            HolderObject = holderObject != null ? holderObject : throw new ArgumentNullException(nameof(holderObject));
            Canvas = canvas;
            EventSystem = eventSystem;
            Services = services ?? throw new ArgumentNullException(nameof(services));
        }
    }
}
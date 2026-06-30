namespace SWeetJelly.Bootstrapper.Framework.Unity
{
    using System;
    using UnityEngine;
    using UnityEngine.EventSystems;
    using SWeetJelly.Bootstrapper.Contract;

    public sealed class PackageBootstrapContext
    {
        public GameObject HolderObject { get; }
        public Canvas Canvas { get; }
        public EventSystem EventSystem { get; }
        public IServiceRegistry Services { get; }

        public PackageBootstrapContext(
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
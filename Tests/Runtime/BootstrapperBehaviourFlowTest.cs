namespace SWeetJelly.Bootstrapper.Tests.PlayMode
{
    using System.Collections;
    using System.Collections.Generic;
    using System.Reflection;
    using NUnit.Framework;
    using SWeetJelly.Bootstrapper.Framework.Unity;
    using UnityEngine;
    using UnityEngine.EventSystems;
    using UnityEngine.TestTools;

    public sealed class BootstrapperBehaviourFlowTest
    {
        [UnityTest]
        public IEnumerator BootstrapperBehaviour_WhenActivated_BootstrapsPackagesInOrderAndShutsDownReverse()
        {
            RecordingPackageBootstrapperSo.Reset();

            var profile = ScriptableObject.CreateInstance<PackageBootstrapperProfileSo>();
            var lateModule = CreateModule("Late", 20);
            var earlyModule = CreateModule("Early", 5);
            var sameOrderModule = CreateModule("Alpha", 20);

            SetPrivateField(
                profile,
                "packageBootstrappers",
                new PackageBootstrapperSo[] { lateModule, null, earlyModule, sameOrderModule });

            var holder = new GameObject("Bootstrapper Behaviour Test Holder");
            holder.SetActive(false);

            var canvas = holder.AddComponent<Canvas>();
            var eventSystem = holder.AddComponent<EventSystem>();
            var behaviour = holder.AddComponent<BootstrapperBehaviour>();

            SetPrivateField(behaviour, "packageBootstrapperProfile", profile);
            SetPrivateField(behaviour, "canvas", canvas);
            SetPrivateField(behaviour, "eventSystem", eventSystem);

            holder.SetActive(true);
            yield return null;

            Assert.That(BootstrapperBehaviour.IsBootstrapped, Is.True);
            Assert.That(BootstrapperBehaviour.GameObject, Is.SameAs(holder));
            Assert.That(BootstrapperBehaviour.Canvas, Is.SameAs(canvas));
            Assert.That(BootstrapperBehaviour.PackageBootstrapperProfile, Is.SameAs(profile));
            Assert.That(BootstrapperBehaviour.Services, Is.Not.Null);
            Assert.That(
                RecordingPackageBootstrapperSo.Events,
                Is.EqualTo(new[]
                {
                    "Bootstrap:Early",
                    "Bootstrap:Alpha",
                    "Bootstrap:Late"
                }));

            UnityEngine.Object.Destroy(holder);
            yield return null;

            Assert.That(BootstrapperBehaviour.IsBootstrapped, Is.False);
            Assert.That(
                RecordingPackageBootstrapperSo.Events,
                Is.EqualTo(new[]
                {
                    "Bootstrap:Early",
                    "Bootstrap:Alpha",
                    "Bootstrap:Late",
                    "Shutdown:Late",
                    "Shutdown:Alpha",
                    "Shutdown:Early"
                }));

            UnityEngine.Object.Destroy(profile);
            UnityEngine.Object.Destroy(lateModule);
            UnityEngine.Object.Destroy(earlyModule);
            UnityEngine.Object.Destroy(sameOrderModule);
        }

        [UnityTest]
        public IEnumerator BootstrapperBehaviour_WithoutEventSystem_CreatesEventSystemChild()
        {
            var holder = new GameObject("Bootstrapper EventSystem Test Holder");
            holder.SetActive(false);

            holder.AddComponent<BootstrapperBehaviour>();

            holder.SetActive(true);
            yield return null;

            var eventSystem = holder.GetComponentInChildren<EventSystem>();

            Assert.That(eventSystem, Is.Not.Null);
            Assert.That(BootstrapperBehaviour.IsBootstrapped, Is.True);

            UnityEngine.Object.Destroy(holder);
            yield return null;

            Assert.That(BootstrapperBehaviour.IsBootstrapped, Is.False);
        }

        private static RecordingPackageBootstrapperSo CreateModule(string packageName, int bootstrapOrder)
        {
            var module = ScriptableObject.CreateInstance<RecordingPackageBootstrapperSo>();
            module.Configure(packageName, bootstrapOrder);
            return module;
        }

        private static void SetPrivateField(object target, string fieldName, object value)
        {
            var field = target
                .GetType()
                .GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);

            Assert.That(field, Is.Not.Null);
            field.SetValue(target, value);
        }

        private sealed class RecordingPackageBootstrapperSo : PackageBootstrapperSo
        {
            private string packageName;
            private int order;

            public static List<string> Events { get; } = new List<string>();

            public override string PackageName => packageName;
            public override int BootstrapOrder => order;

            public static void Reset()
            {
                Events.Clear();
            }

            public void Configure(string configuredPackageName, int configuredOrder)
            {
                packageName = configuredPackageName;
                order = configuredOrder;
            }

            public override void Bootstrap(PackageBootstrapContext context)
            {
                Events.Add($"Bootstrap:{PackageName}");
            }

            public override void Shutdown(PackageBootstrapContext context)
            {
                Events.Add($"Shutdown:{PackageName}");
            }
        }
    }
}

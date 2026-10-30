namespace SWeetJelly.Bootstrapper.Tests.PlayMode
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Reflection;
    using System.Text.RegularExpressions;
    using NUnit.Framework;
    using SWeetJelly.Bootstrapper.Framework.Unity;
    using UnityEngine;
    using UnityEngine.TestTools;
    using Object = UnityEngine.Object;

    public sealed class BootstrapperLifecycleRegressionTest
    {
        private readonly List<GameObject> holders = new List<GameObject>();
        private readonly List<ScriptableObject> assets = new List<ScriptableObject>();

        [UnityTearDown]
        public IEnumerator Cleanup()
        {
            foreach (var asset in assets)
                if (asset is RecordingModule module) module.FailShutdown = false;
            foreach (var holder in holders)
                if (holder != null) Object.Destroy(holder);
            yield return null;
            foreach (var asset in assets)
                if (asset != null) Object.Destroy(asset);
            holders.Clear();
            assets.Clear();
            yield return null;
        }

        [UnityTest]
        public IEnumerator DestroyingDuplicate_PreservesOwnerState()
        {
            var owner = CreateHolder(null);
            owner.SetActive(true);
            var services = BootstrapperBehaviour.Services;
            var duplicate = CreateHolder(null);
            duplicate.SetActive(true);
            Object.Destroy(duplicate);
            yield return null;
            Assert.That(BootstrapperBehaviour.IsBootstrapped, Is.True);
            Assert.That(BootstrapperBehaviour.GameObject, Is.SameAs(owner));
            Assert.That(BootstrapperBehaviour.Services, Is.SameAs(services));
        }

        [UnityTest]
        public IEnumerator DestroyingOwner_ClearsPublishedReferences()
        {
            var holder = CreateHolder(null);
            holder.SetActive(true);
            Object.Destroy(holder);
            yield return null;
            Assert.That(BootstrapperBehaviour.IsBootstrapped, Is.False);
            Assert.That(BootstrapperBehaviour.Services, Is.Null);
            Assert.That(BootstrapperBehaviour.GameObject, Is.Null);
            Assert.That(BootstrapperBehaviour.Canvas, Is.Null);
            Assert.That(BootstrapperBehaviour.PackageBootstrapperProfile, Is.Null);
        }

        [UnityTest]
        public IEnumerator BootstrapFailure_RollsBackCompletedPackages()
        {
            var early = CreateModule(0);
            var late = CreateModule(1);
            late.FailBootstrap = true;
            var holder = CreateHolder(CreateProfile(early, late));
            LogAssert.Expect(LogType.Exception, new Regex("InvalidOperationException: bootstrap failed"));
            holder.SetActive(true);
            yield return null;
            Assert.That(early.ShutdownCount, Is.EqualTo(1));
            Assert.That(BootstrapperBehaviour.IsBootstrapped, Is.False);
            Assert.That(BootstrapperBehaviour.Services, Is.Null);
        }

        [UnityTest]
        public IEnumerator ShutdownFailure_StillShutsDownRemainingPackages()
        {
            var early = CreateModule(0);
            var late = CreateModule(1);
            late.FailShutdown = true;
            var holder = CreateHolder(CreateProfile(early, late));
            holder.SetActive(true);
            LogAssert.Expect(LogType.Exception, new Regex("InvalidOperationException: shutdown failed"));
            Object.Destroy(holder);
            yield return null;
            Assert.That(early.ShutdownCount, Is.EqualTo(1));
            Assert.That(BootstrapperBehaviour.IsBootstrapped, Is.False);
            Assert.That(BootstrapperBehaviour.Services, Is.Null);
        }

        [UnityTest]
        public IEnumerator BootstrapFailure_WithRollbackFailure_PreservesOriginalException()
        {
            var early = CreateModule(0);
            early.FailShutdown = true;
            var late = CreateModule(1);
            late.FailBootstrap = true;
            var holder = CreateHolder(CreateProfile(early, late));
            LogAssert.Expect(LogType.Exception, new Regex("InvalidOperationException: shutdown failed"));
            LogAssert.Expect(LogType.Exception, new Regex("InvalidOperationException: bootstrap failed"));
            holder.SetActive(true);
            yield return null;
            Assert.That(early.ShutdownCount, Is.EqualTo(1));
            Assert.That(BootstrapperBehaviour.IsBootstrapped, Is.False);
            Assert.That(BootstrapperBehaviour.Services, Is.Null);
        }

        [UnityTest]
        public IEnumerator InputSetup_SelectsInstalledOptionalModuleOrLegacyFallback()
        {
            var holder = CreateHolder(null);
            holder.SetActive(true);
            yield return null;
            var module = holder.GetComponentInChildren<UnityEngine.EventSystems.BaseInputModule>();
            var optionalType = Type.GetType("UnityEngine.InputSystem.UI.InputSystemUIInputModule, Unity.InputSystem");
            Assert.That(module, Is.Not.Null);
            Assert.That(module.GetType(), Is.EqualTo(optionalType
                ?? typeof(UnityEngine.EventSystems.StandaloneInputModule)));
        }

        [UnityTest]
        public IEnumerator InputSetup_PreservesAssignedEventSystemAndExistingModule()
        {
            var holder = CreateHolder(null);
            var system = holder.AddComponent<UnityEngine.EventSystems.EventSystem>();
            var module = holder.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
            SetField(holder.GetComponent<BootstrapperBehaviour>(), "eventSystem", system);
            holder.SetActive(true);
            yield return null;
            Assert.That(holder.GetComponentInChildren<UnityEngine.EventSystems.EventSystem>(), Is.SameAs(system));
            Assert.That(system.GetComponents<UnityEngine.EventSystems.BaseInputModule>(),
                Is.EqualTo(new UnityEngine.EventSystems.BaseInputModule[] { module }));
        }
        private GameObject CreateHolder(PackageBootstrapperProfileSo profile)
        {
            var holder = new GameObject("Bootstrapper regression holder");
            holders.Add(holder);
            holder.SetActive(false);
            var behaviour = holder.AddComponent<BootstrapperBehaviour>();
            SetField(behaviour, "packageBootstrapperProfile", profile);
            return holder;
        }

        private RecordingModule CreateModule(int order)
        {
            var module = ScriptableObject.CreateInstance<RecordingModule>();
            module.Order = order;
            assets.Add(module);
            return module;
        }

        private PackageBootstrapperProfileSo CreateProfile(params PackageBootstrapperSo[] modules)
        {
            var profile = ScriptableObject.CreateInstance<PackageBootstrapperProfileSo>();
            assets.Add(profile);
            SetField(profile, "packageBootstrappers", modules);
            return profile;
        }

        private static void SetField(object target, string field, object value)
        {
            target.GetType().GetField(field, BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(target, value);
        }

        private sealed class RecordingModule : PackageBootstrapperSo
        {
            public int Order;
            public int ShutdownCount;
            public bool FailBootstrap;
            public bool FailShutdown;
            public override string PackageName => "Regression";
            public override int BootstrapOrder => Order;

            public override void Bootstrap(PackageBootstrapContext context)
            {
                if (FailBootstrap) throw new InvalidOperationException("bootstrap failed");
            }

            public override void Shutdown(PackageBootstrapContext context)
            {
                ShutdownCount++;
                if (FailShutdown) throw new InvalidOperationException("shutdown failed");
            }
        }
    }
}

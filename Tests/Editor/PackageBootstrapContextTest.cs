namespace SWeetJelly.Bootstrapper.Tests.EditMode
{
    using System;
    using NUnit.Framework;
    using SWeetJelly.Bootstrapper.App;
    using SWeetJelly.Bootstrapper.Framework.Unity;
    using UnityEngine;
    using UnityEngine.EventSystems;

    public sealed class PackageBootstrapContextTest
    {
        [Test]
        public void Constructor_WithDependencies_AssignsProperties()
        {
            var holder = new GameObject("Bootstrapper Context Holder");
            var canvas = holder.AddComponent<Canvas>();
            var eventSystem = holder.AddComponent<EventSystem>();
            var services = new ServiceRegistry();

            try
            {
                var context = new PackageBootstrapContext(
                    holder,
                    canvas,
                    eventSystem,
                    services);

                Assert.That(context.HolderObject, Is.SameAs(holder));
                Assert.That(context.Canvas, Is.SameAs(canvas));
                Assert.That(context.EventSystem, Is.SameAs(eventSystem));
                Assert.That(context.Services, Is.SameAs(services));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(holder);
            }
        }

        [Test]
        public void Constructor_WithNullHolder_ThrowsArgumentNullException()
        {
            var exception = AssertThrows<ArgumentNullException>(() =>
                new PackageBootstrapContext(null, null, null, new ServiceRegistry()));

            Assert.That(exception.ParamName, Is.EqualTo("holderObject"));
        }

        [Test]
        public void Constructor_WithNullServices_ThrowsArgumentNullException()
        {
            var holder = new GameObject("Bootstrapper Context Holder");

            try
            {
                var exception = AssertThrows<ArgumentNullException>(() =>
                    new PackageBootstrapContext(holder, null, null, null));

                Assert.That(exception.ParamName, Is.EqualTo("services"));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(holder);
            }
        }

        private static TException AssertThrows<TException>(Action action)
            where TException : Exception
        {
            try
            {
                action();
            }
            catch (TException exception)
            {
                return exception;
            }

            Assert.Fail($"Expected exception of type {typeof(TException).Name}.");
            return null;
        }
    }
}

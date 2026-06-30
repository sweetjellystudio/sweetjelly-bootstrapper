namespace SWeetJelly.Bootstrapper.Tests.EditMode
{
    using System;
    using NUnit.Framework;
    using SWeetJelly.Bootstrapper.App;

    public sealed class ServiceRegistryTest
    {
        [Test]
        public void RegisterAndResolve_WithService_ReturnsSameInstance()
        {
            var registry = new ServiceRegistry();
            var service = new TestService();

            registry.Register<ITestService>(service);

            Assert.That(registry.Resolve<ITestService>(), Is.SameAs(service));
            Assert.That(registry.TryResolve<ITestService>(out var resolved), Is.True);
            Assert.That(resolved, Is.SameAs(service));
        }

        [Test]
        public void Resolve_WhenServiceIsNotRegistered_ThrowsNullReferenceException()
        {
            var registry = new ServiceRegistry();

            var exception = AssertThrows<NullReferenceException>(() =>
                registry.Resolve<ITestService>());

            Assert.That(exception.Message, Does.Contain("not registered"));
        }

        [Test]
        public void Register_WithDuplicateServiceType_ThrowsArgumentException()
        {
            var registry = new ServiceRegistry();

            registry.Register<ITestService>(new TestService());

            var exception = AssertThrows<ArgumentException>(() =>
                registry.Register<ITestService>(new TestService()));

            Assert.That(exception.Message, Does.Contain("already registered"));
        }

        [Test]
        public void Register_WithNullService_ThrowsArgumentNullException()
        {
            var registry = new ServiceRegistry();

            var exception = AssertThrows<ArgumentNullException>(() =>
                registry.Register<ITestService>(null));

            Assert.That(exception.ParamName, Is.EqualTo("service"));
        }

        [Test]
        public void Unregister_RemovesService()
        {
            var registry = new ServiceRegistry();

            registry.Register<ITestService>(new TestService());
            registry.Unregister<ITestService>();

            Assert.That(registry.TryResolve<ITestService>(out _), Is.False);
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

        private interface ITestService
        {
        }

        private sealed class TestService : ITestService
        {
        }
    }
}

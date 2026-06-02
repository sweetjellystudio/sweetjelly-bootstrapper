using System;
using System.Collections.Generic;
using SWeetJelly.Initialiser.Contract;

namespace SWeetJelly.Initialiser.App
{
    public sealed class ServiceRegistry : IServiceRegistry
    {
        readonly Dictionary<Type, object> services = new ();

        public void Register<T>(T service) where T : class
        {
            if (service == null)
                throw new ArgumentNullException(nameof(service));

            var type = typeof(T);

            if (services.ContainsKey(type))
            {
                throw new ArgumentException($"ServiceRegistry.Register : Service of type {type} is already registered");
            }

            services.Add(type, service);
        }

        public T Resolve<T>() where T : class
        {
            if (TryResolve<T>(out var service))
                return service;

            throw new NullReferenceException($"ServiceRegistry.Get : Service of type {typeof(T)} not registered");
        }

        public bool TryResolve<T>(out T service) where T : class
        {
            if (services.TryGetValue(typeof(T), out object obj) && obj is T typed)
            {
                service = typed;
                return true;
            }

            service = null;
            return false;
        }

        public void Unregister<T>() where T : class
        {
            services.Remove(typeof(T));
        }
    }
}
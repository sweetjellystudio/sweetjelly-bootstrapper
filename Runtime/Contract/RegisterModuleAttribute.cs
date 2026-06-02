namespace SWeetJelly.Initialiser.Contract
{
    using System;

    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public class RegisterModuleAttribute : Attribute
    {
        public string Path { get; private set; }
        public int Order { get; private set; }

        public RegisterModuleAttribute(string path, int order = 100)
        {
            Path = path;
            Order = order;
        }
    }
}
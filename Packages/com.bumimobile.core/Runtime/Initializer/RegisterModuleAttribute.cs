using System;

namespace BumiMobile
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public class RegisterModuleAttribute : Attribute
    {
        public string Path { get; }
        public bool Core { get; }
        public int Order { get; }

        public RegisterModuleAttribute(string path, bool core = false, int order = 0)
        {
            Path = path;
            Core = core;
            Order = order;
        }
    }
}

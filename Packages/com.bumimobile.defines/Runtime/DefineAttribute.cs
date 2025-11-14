using System;

namespace BumiMobile
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public class DefineAttribute : Attribute
    {
        public string Define { get; private set; }
        public string AssemblyType { get; private set; }

        public DefineAttribute(string define, string assemblyType = "")
        {
            Define = define;

            AssemblyType = assemblyType;
        }
    }
}

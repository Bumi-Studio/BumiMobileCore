using System;

namespace BumiMobile
{
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
    public class CachedTargetAttribute : Attribute
    {
        public string TargetVariableName { get; private set; }

        public CachedTargetAttribute(string targetVariableName)
        {
            TargetVariableName = targetVariableName;
        }
    }
}

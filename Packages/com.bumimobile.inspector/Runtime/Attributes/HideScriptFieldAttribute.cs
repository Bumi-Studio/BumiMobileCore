using System;

namespace BumiMobile
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
    public class HideScriptFieldAttribute : Attribute { }
}

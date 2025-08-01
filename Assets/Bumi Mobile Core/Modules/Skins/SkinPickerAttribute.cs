using System;
using UnityEngine;

namespace BumiMobile
{
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
    public class SkinPickerAttribute : PropertyAttribute
    {
        public Type DatabaseType { get; private set; }

        public SkinPickerAttribute() { }

        public SkinPickerAttribute(Type databaseType)
        {
            DatabaseType = databaseType;
        }
    }
}

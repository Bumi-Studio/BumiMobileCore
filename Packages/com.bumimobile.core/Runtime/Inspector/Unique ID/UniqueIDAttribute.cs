using System;
using UnityEngine;

namespace BumiMobile
{
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
    public class UniqueIDAttribute : PropertyAttribute
    {
    }
}

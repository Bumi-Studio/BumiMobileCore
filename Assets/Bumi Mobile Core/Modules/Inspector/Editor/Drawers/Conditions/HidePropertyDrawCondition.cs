using System.Reflection;
using System;
using System.Collections.Generic;

namespace BumiMobile
{
    [PropertyCondition(typeof(HideAttribute))]
    public class HidePropertyDrawCondition : PropertyCondition
    {
        public override bool CanBeDrawn(CustomInspector editor, FieldInfo fieldInfo, object targetObject, List<Type> nestedTypes)
        {
            return false;
        }
    }
}
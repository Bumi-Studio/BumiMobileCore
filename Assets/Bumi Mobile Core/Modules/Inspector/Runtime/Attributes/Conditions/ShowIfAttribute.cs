using System;

namespace BumiMobile
{
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
    public class ShowIfAttribute : ConditionAttribute
    {
        public string ConditionName { get; private set; }

        public ShowIfAttribute(string conditionName)
        {
            ConditionName = conditionName;
        }
    }
}
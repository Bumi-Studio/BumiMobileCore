﻿using System;

namespace BumiMobile
{
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public class ShowNonSerializedAttribute : Attribute { }
}

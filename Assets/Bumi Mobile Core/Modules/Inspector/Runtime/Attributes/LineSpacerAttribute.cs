﻿using System;
using UnityEngine;

namespace BumiMobile
{
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
    public class LineSpacerAttribute : PropertyAttribute
    {
        public string title;
        public int height;

        public LineSpacerAttribute(string title = "", int height = 18)
        {
            this.title = title;
            this.height = height;
        }
    }
}

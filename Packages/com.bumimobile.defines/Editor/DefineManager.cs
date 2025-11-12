using UnityEngine;
using UnityEditor;
using System;
using System.Reflection;
using System.Collections.Generic;
using System.Text;

#if UNITY_6000
using UnityEditor.Build;
#endif

namespace BumiMobile
{
#if false
    public static class DefineManager
    {
    }
#endif
}

// -----------------
// Define Manager v0.3.1
// -----------------

// Changelog
// v 0.3.1
// • Added ability to load auto-defines by adding Define attributes to classes
// v 0.3
// • Added auto toggle for specific defines
// • UI moved from scriptable object editor to editor window
// v 0.2.1
// • Added link to the documentation
// • Enable define function fix
// v 0.1
// • Added basic version
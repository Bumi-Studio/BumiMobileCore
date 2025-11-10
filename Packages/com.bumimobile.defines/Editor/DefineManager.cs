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
    public static class DefineManager
    {
        public static bool HasDefine(string define)
        {
#if UNITY_6000
            string definesLine = PlayerSettings.GetScriptingDefineSymbols(NamedBuildTarget.FromBuildTargetGroup(BuildPipeline.GetBuildTargetGroup(EditorUserBuildSettings.activeBuildTarget)));
#else
            string definesLine = PlayerSettings.GetScriptingDefineSymbolsForGroup(BuildPipeline.GetBuildTargetGroup(EditorUserBuildSettings.activeBuildTarget));
#endif

            return Array.FindIndex(definesLine.Split(';'), x => x == define) != -1;
        }

        public static void EnableDefine(string define)
        {
#if UNITY_6000
            string defineLine = PlayerSettings.GetScriptingDefineSymbols(NamedBuildTarget.FromBuildTargetGroup(BuildPipeline.GetBuildTargetGroup(EditorUserBuildSettings.activeBuildTarget)));
#else
            string defineLine = PlayerSettings.GetScriptingDefineSymbolsForGroup(BuildPipeline.GetBuildTargetGroup(EditorUserBuildSettings.activeBuildTarget));
#endif

            if (Array.FindIndex(defineLine.Split(';'), x => x == define) != -1)
            {
                return;
            }

            defineLine = defineLine.Insert(0, define + ";");

#if UNITY_6000
            PlayerSettings.SetScriptingDefineSymbols(NamedBuildTarget.FromBuildTargetGroup(BuildPipeline.GetBuildTargetGroup(EditorUserBuildSettings.activeBuildTarget)), defineLine);
#else
            PlayerSettings.SetScriptingDefineSymbolsForGroup(BuildPipeline.GetBuildTargetGroup(EditorUserBuildSettings.activeBuildTarget), defineLine);
#endif
        }

        public static void DisableDefine(string define)
        {
#if UNITY_6000
            string defineLine = PlayerSettings.GetScriptingDefineSymbols(NamedBuildTarget.FromBuildTargetGroup(BuildPipeline.GetBuildTargetGroup(EditorUserBuildSettings.activeBuildTarget)));
#else
            string defineLine = PlayerSettings.GetScriptingDefineSymbolsForGroup(BuildPipeline.GetBuildTargetGroup(EditorUserBuildSettings.activeBuildTarget));
#endif

            string[] splitedDefines = defineLine.Split(';');

            int tempDefineIndex = Array.FindIndex(splitedDefines, x => x == define);
            string tempDefineLine = "";
            if (tempDefineIndex != -1)
            {
                for (int i = 0; i < splitedDefines.Length; i++)
                {
                    if (i != tempDefineIndex)
                    {
                        defineLine = defineLine.Insert(0, splitedDefines[i]);
                    }
                }
            }

            if (defineLine != tempDefineLine)
            {
#if UNITY_6000
                PlayerSettings.SetScriptingDefineSymbols(NamedBuildTarget.FromBuildTargetGroup(BuildPipeline.GetBuildTargetGroup(EditorUserBuildSettings.activeBuildTarget)), tempDefineLine);
#else
                PlayerSettings.SetScriptingDefineSymbolsForGroup(BuildPipeline.GetBuildTargetGroup(EditorUserBuildSettings.activeBuildTarget), tempDefineLine);
#endif
            }
        }

        public static void CheckAutoDefines()
        {
            if (EditorApplication.isCompiling || EditorApplication.isUpdating || string.IsNullOrEmpty(CoreEditor.FOLDER_CORE))
            {
                EditorApplication.delayCall += CheckAutoDefines;

                return;
            }

            Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();

            List<DefineState> markedDefines = new List<DefineState>();
            List<RegisteredDefine> registeredDefines = DefineSettings.GetDynamicDefines();
            foreach (RegisteredDefine registeredDefine in registeredDefines)
            {
                bool defineFound = false;

                foreach(Assembly assembly in assemblies)
                {
                    Type targetType = assembly.GetType(registeredDefine.AssemblyType, false);
                    if (targetType != null)
                    {
                        markedDefines.Add(new DefineState(registeredDefine.Define, true));

                        defineFound = true;

                        break;
                    }
                }

                if(!defineFound)
                {
                    markedDefines.Add(new DefineState(registeredDefine.Define, false));
                }
            }

            ChangeAutoDefinesState(markedDefines);
        }

        public static void ChangeAutoDefinesState(List<DefineState> defineStates)
        {
            if (EditorApplication.isCompiling)
                return;

            if (defineStates.IsNullOrEmpty())
                return;

            bool definesUpdated = false;

            StringBuilder sb = new StringBuilder();
            sb.Append("[Define Manager]: Dependencies change is detected. Updating Scripting Define Symbols..");
            sb.AppendLine();

            DefineString definesString = new DefineString();
            foreach (DefineState defineState in defineStates)
            {
                if (defineState.State)
                {
                    if (!definesString.HasDefine(defineState.Define))
                    {
                        definesUpdated = true;

                        definesString.AddDefine(defineState.Define);

                        sb.AppendLine();
                        sb.Append(defineState.Define);
                        sb.Append(" - added");
                    }
                }
                else
                {
                    if (definesString.HasDefine(defineState.Define))
                    {
                        definesUpdated = true;

                        definesString.RemoveDefine(defineState.Define);

                        sb.AppendLine();
                        sb.Append(defineState.Define);
                        sb.Append(" - removed");
                    }
                }
            }
            sb.AppendLine();

            if (definesUpdated)
                Debug.Log(sb.ToString());

            definesString.ApplyDefines();
        }
    }
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
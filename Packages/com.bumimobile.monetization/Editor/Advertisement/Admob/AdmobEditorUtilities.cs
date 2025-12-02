#if UNITY_EDITOR
using System;
using System.Reflection;
using UnityEngine;

namespace BumiMobile
{
    internal static class AdmobEditorUtilities
    {
#if MODULE_ADMOB
        private const string SettingsEditorTypeName = "GoogleMobileAds.Editor.GoogleMobileAdsSettingsEditor";

        public static void EnsureSettingsAsset()
        {
            if (!TryOpenInspector())
            {
                Debug.LogWarning("[Monetization] GoogleMobileAdsSettingsEditor not found. Ensure the Google Mobile Ads plugin is imported.");
            }
        }

        private static bool TryOpenInspector()
        {
            var editorType = FindSettingsEditorType();
            if (editorType == null)
            {
                return false;
            }

            var method = editorType.GetMethod("OpenInspector", BindingFlags.Public | BindingFlags.Static);
            if (method == null)
            {
                return false;
            }

            method.Invoke(null, null);
            return true;
        }

        private static Type FindSettingsEditorType()
        {
            var type = Type.GetType(SettingsEditorTypeName);
            if (type != null)
            {
                return type;
            }

            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                try
                {
                    type = assembly.GetType(SettingsEditorTypeName);
                    if (type != null)
                    {
                        return type;
                    }
                }
                catch
                {
                    // Ignore reflection issues and continue searching
                }
            }

            return null;
        }
#else
        public static void EnsureSettingsAsset()
        {
            // Intentionally left blank when AdMob is not enabled.
        }
#endif
    }
}
#endif

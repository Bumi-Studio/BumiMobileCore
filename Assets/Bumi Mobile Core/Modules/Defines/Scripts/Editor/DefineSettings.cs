using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System;
using UnityEngine;

namespace BumiMobile
{
    public static class DefineSettings
    {
        public static readonly string[] STATIC_DEFINES = new string[]
        {
            "UNITY_POST_PROCESSING_STACK_V2",
            "PHOTON_UNITY_NETWORKING",
            "PUN_2_0_OR_NEWER",
            "PUN_2_OR_NEWER",
        };

        public static readonly RegisteredDefine[] STATIC_REGISTERED_DEFINES = new RegisteredDefine[]
        {
            // System
            new RegisteredDefine("MODULE_INPUT_SYSTEM", "UnityEngine.InputSystem.InputManager"),
            new RegisteredDefine("MODULE_TMP", "TMPro.TMP_Text"),
            new RegisteredDefine("MODULE_CINEMACHINE", "Cinemachine.CinemachineBrain"),
            new RegisteredDefine("MODULE_IDFA", "Unity.Advertisement.IosSupport.ATTrackingStatusBinding"),

            // Core
            new RegisteredDefine("MODULE_MONETIZATION", "Bumi.Monetization"),
            new RegisteredDefine("MODULE_IAP", "UnityEngine.Purchasing.UnityPurchasing"),
            new RegisteredDefine("MODULE_POWERUPS", "Bumi.PUController"),
            new RegisteredDefine("MODULE_HAPTIC", "Bumi.Haptic"),
            new RegisteredDefine("MODULE_CURVE", "Bumi.CurvatureManager"),

            new RegisteredDefine("TEST", "NewBehaviourScript"),
        };

        public static List<RegisteredDefine> GetDynamicDefines()
        {
            //Get assembly
            List<Type> gameTypes = new List<Type>();
            Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
            foreach (Assembly assembly in assemblies)
            {
                if (assembly != null)
                {
                    try
                    {
                        Type[] tempTypes = assembly.GetTypes();

                        tempTypes = tempTypes.Where(m => m.IsDefined(typeof(DefineAttribute), true)).ToArray();

                        if (!tempTypes.IsNullOrEmpty())
                            gameTypes.AddRange(tempTypes);
                    }
                    catch (ReflectionTypeLoadException e)
                    {
                        Debug.LogException(e);
                    }
                }
            }

            List<RegisteredDefine> registeredDefines = new List<RegisteredDefine>();
            registeredDefines.AddRange(STATIC_REGISTERED_DEFINES);

            foreach (Type type in gameTypes)
            {
                //Get attribute
                DefineAttribute[] defineAttributes = (DefineAttribute[])Attribute.GetCustomAttributes(type, typeof(DefineAttribute));

                for (int i = 0; i < defineAttributes.Length; i++)
                {
                    if (!string.IsNullOrEmpty(defineAttributes[i].AssemblyType))
                    {
                        int methodId = registeredDefines.FindIndex(x => x.Define == defineAttributes[i].Define);
                        if (methodId == -1)
                        {
                            registeredDefines.Add(new RegisteredDefine(defineAttributes[i]));
                        }
                    }
                }
            }

            return registeredDefines;
        }
    }
}
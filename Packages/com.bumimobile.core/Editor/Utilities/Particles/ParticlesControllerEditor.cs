using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace BumiMobile
{
    [CustomEditor(typeof(ParticlesController))]
    public class ParticlesControllerEditor : Editor
    {
        private Dictionary<int, Particle> registeredParticles;

        private void OnEnable()
        {
            FieldInfo fieldInfo = typeof(ParticlesController).GetField("registerParticles", BindingFlags.NonPublic | BindingFlags.Static);
            registeredParticles = fieldInfo?.GetValue(null) as Dictionary<int, Particle>;
        }

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            if(Application.isPlaying)
            {
                if (registeredParticles != null)
                {
                    int registeredParticlesCount = registeredParticles.Count;
                    if (registeredParticlesCount > 0)
                    {
                        EditorGUILayout.BeginVertical("box");
                        EditorGUILayout.LabelField(string.Format("Registered particles: {0}", registeredParticlesCount));

                        foreach (Particle registeredParticle in registeredParticles.Values)
                        {
                            EditorGUILayout.BeginHorizontal("box");

                            EditorGUILayout.LabelField(registeredParticle.ParticleName, GUILayout.Width(80));

                            GUILayout.FlexibleSpace();

                            using (new EditorGUI.DisabledScope(true))
                            {
                                EditorGUILayout.ObjectField(GUIContent.none, registeredParticle.ParticlePrefab, typeof(GameObject), allowSceneObjects: false, GUILayout.Width(80));
                            }

                            EditorGUILayout.EndHorizontal();
                        }

                        EditorGUILayout.EndVertical();
                    }
                }
            }
        }
    }
}

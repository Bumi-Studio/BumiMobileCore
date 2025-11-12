using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.U2D;
using UnityEngine;
using UnityEngine.TextCore;
using UnityEngine.U2D;

#if MODULE_TMP
using TMPro;
#endif

namespace BumiMobile
{
    [CustomEditor(typeof(CurrencyDatabase))]
    public class CurrencyDatabaseEditor : CustomInspector
    {
        private CurrencyDatabase currencyDatabase;

        protected override void OnEnable()
        {
            base.OnEnable();

            currencyDatabase = (CurrencyDatabase)target;
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

#if MODULE_TMP
            // TMP sprite atlas generation temporarily disabled until the TMP package is available.
            // if (GUILayout.Button("Create Sprite Atlas"))
            // {
            //     CreateAtlas();
            // }
#endif
        }

#if MODULE_TMP
        // Atlas generation code removed temporarily.
#endif
    }
}

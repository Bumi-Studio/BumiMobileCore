using System;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

namespace BumiMobile
{
    public class SettingsTestHaptic : MonoBehaviour
    {
        [BoxGroup("Reference", "Settings")]
        [SerializeField]
        private Slider slider;
        [BoxGroup("Reference", "Settings")]
        [SerializeField]
        private TextMeshProUGUI text;
        [BoxGroup("Reference", "Settings")]
        [SerializeField]
        private string nameSettings;

        private void Awake()
        {
            slider.onValueChanged.AddListener((val) =>
            {
                text.text = $"{nameSettings} {val}";
            });
        }

        public float value
        {
            get => slider.value;
            set => slider.value = value;
        }
    }

}

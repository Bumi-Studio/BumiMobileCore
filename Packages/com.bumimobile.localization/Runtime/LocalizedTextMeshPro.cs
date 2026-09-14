using TMPro;
using UnityEngine;

namespace BumiMobile
{
    [RequireComponent(typeof(TextMeshProUGUI))]
    public class LocalizedTextMeshPro : MonoBehaviour
    {
        public string LocalizationKey;
        public bool IsSpecialCharacters = false;
        public SpecialCharacters specialCharacters;

        // Example: you can later expand this array for flexible sprite tags
        // public string[] SpecialCharacters = new string[] { "<sprite name=shop 1>", "<sprite name=shop 2>" };

        private TextMeshProUGUI _textMesh;

        private void Awake()
        {
            _textMesh = GetComponent<TextMeshProUGUI>();
        }

        public void Start()
        {
            Localize();
            LocalizationController.OnLocalizationChanged += Localize;
        }

        public void OnDestroy()
        {
            LocalizationController.OnLocalizationChanged -= Localize;
        }

        private void Localize()
        {
            string localizedText = LocalizationController.Localize(LocalizationKey);
            if (IsSpecialCharacters && specialCharacters != null)
            {
                localizedText = $"{specialCharacters.Prefix}{localizedText}{specialCharacters.Suffix}";
            }
            _textMesh.text = localizedText;
        }
    }
    [System.Serializable]
    public class SpecialCharacters
    {
        public string Prefix;
        public string Suffix;
    }
}

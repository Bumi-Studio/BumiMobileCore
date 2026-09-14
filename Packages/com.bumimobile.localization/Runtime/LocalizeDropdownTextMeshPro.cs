using UnityEngine;

namespace BumiMobile
{
    [RequireComponent(typeof(TMPro.TMP_Dropdown))]
    public class LocalizedDropdownTextMeshPro : MonoBehaviour
    {
        public string[] LocalizationKeys;

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
            var dropdown = GetComponent<TMPro.TMP_Dropdown>();

            for (var i = 0; i < LocalizationKeys.Length; i++)
            {
                dropdown.options[i].text = LocalizationController.Localize(LocalizationKeys[i]);
            }

            if (dropdown.value < LocalizationKeys.Length)
            {
                dropdown.captionText.text = LocalizationController.Localize(LocalizationKeys[dropdown.value]);
            }
        }
    }
}

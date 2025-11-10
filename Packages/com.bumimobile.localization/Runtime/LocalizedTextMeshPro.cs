using TMPro;
using UnityEngine;

namespace BumiMobile
{
    [RequireComponent(typeof(TextMeshProUGUI))]
    public class LocalizedTextMeshPro : MonoBehaviour
    {
        public string LocalizationKey;

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
            GetComponent<TextMeshProUGUI>().text = LocalizationController.Localize(LocalizationKey);
        }
    }
}

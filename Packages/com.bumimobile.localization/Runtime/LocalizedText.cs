using UnityEngine;
using UnityEngine.UI;

namespace BumiMobile
{
	/// <summary>
	/// Localize text component.
	/// </summary>
    [RequireComponent(typeof(Text))]
    public class LocalizedText : MonoBehaviour
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
            GetComponent<Text>().text = LocalizationController.Localize(LocalizationKey);
        }
    }
}
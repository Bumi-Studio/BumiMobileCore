using UnityEngine;
using UnityEngine.UI;

namespace BumiMobile
{
	/// <summary>
	/// Localize dropdown component.
	/// </summary>
    [RequireComponent(typeof(Dropdown))]
    public class LocalizedDropdown : MonoBehaviour
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
	        var dropdown = GetComponent<Dropdown>();

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
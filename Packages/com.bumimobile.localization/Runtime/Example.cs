using System;
using UnityEngine;
using UnityEngine.UI;
using BumiMobile;

namespace BumiMobile
{
	/// <summary>
	/// Asset usage example.
	/// </summary>
	public class Example : MonoBehaviour
	{
		public Text FormattedText;

		/// <summary>
		/// Called on app start.
		/// </summary>
		public void Awake()
		{
			LocalizationController.Read();

			switch (Application.systemLanguage)
			{
				case SystemLanguage.German:
					LocalizationController.Language = LanguageType.German;
					break;
				default:
					LocalizationController.Language = LanguageType.English;
					break;
			}

			// This way you can localize and format strings from code.
			FormattedText.text = LocalizationController.Localize("Settings.Example.PlayTime", TimeSpan.FromHours(10.5f).TotalHours);

			// This way you can subscribe to LocalizationChanged event.
			LocalizationController.OnLocalizationChanged += () => FormattedText.text = LocalizationController.Localize("Settings.Example.PlayTime", TimeSpan.FromHours(10.5f).TotalHours);
		}

		/// <summary>
		/// Change localization at runtime.
		/// </summary>
		public void SetLocalization(LanguageType localization)
		{
			LocalizationController.Language = localization;
		}
	}
}
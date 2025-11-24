using System;
using System.Globalization;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Serialization;

namespace BumiMobile
{
	public class UIPlayerScore : MonoBehaviour
	{
		const int MaxNameGraphemes = 14;

		[Header("Texts")]
		[SerializeField] private TextMeshProUGUI nomorText;
		[SerializeField] private TextMeshProUGUI nameText;
		[SerializeField] private TextMeshProUGUI scoreText;

		[Header("Icon")]
		[SerializeField, FormerlySerializedAs("countryImage")] private Image iconImage;
		[SerializeField] private Image iconFrameImage;
		[SerializeField] private CountryFlagDatabase countryFlags; // ✅ assign in Inspector
		[SerializeField, FormerlySerializedAs("defaultCountrySprite")] private Sprite defaultIconSprite;         // optional fallback

		[Header("Medals")]
		[SerializeField] private Image medalImage;
		[SerializeField] private Sprite medal_1;
		[SerializeField] private Sprite medal_2;
		[SerializeField] private Sprite medal_3;

		[Header("Row BG")]
		[SerializeField] private Image playerContainerImage;
		[SerializeField] private Sprite playerContainer_1;
		[SerializeField] private Sprite playerContainer_2;
		[SerializeField] private Sprite playerContainer_3;
		[SerializeField] private Sprite playerContainer_Self;
		[SerializeField] private Sprite defaultPlayerContainer;

		public void SetupInfo(int rank, string playerName, string scoreText, string countryName, bool isSelf = false, Sprite avatarSprite = null, bool useCountryFlag = true)
		{
			// Is this row the local player?
			bool self = isSelf
				|| (!string.IsNullOrEmpty(AuthService.PlayerId)
					&& string.Equals(playerName?.Trim(), AuthService.PlayerId, StringComparison.Ordinal));

			// Rank visuals
			nomorText.enabled = false;
			medalImage.enabled = true;

			switch (rank)
			{
				case 1:
					medalImage.sprite = medal_1;
					playerContainerImage.sprite = self
						? playerContainer_Self
						: (playerContainer_1 != null ? playerContainer_1 : playerContainer_Self);
					break;

				case 2:
					medalImage.sprite = medal_2;
					playerContainerImage.sprite = self
						? playerContainer_Self
						: (playerContainer_2 != null ? playerContainer_2 : playerContainer_Self);
					break;

				case 3:
					medalImage.sprite = medal_3;
					playerContainerImage.sprite = self
						? playerContainer_Self
						: (playerContainer_3 != null ? playerContainer_3 : playerContainer_Self);
					break;

				default:
					// For non-medal positions, optionally hide rank number when rank <= 0
					if (rank > 0)
					{
						nomorText.enabled = true;
						nomorText.text = rank.ToString();
					}
					else
					{
						nomorText.enabled = false; // hide rank number (no flicker with "0")
					}
					medalImage.enabled = false;
					playerContainerImage.sprite = self
						? playerContainer_Self
						: (defaultPlayerContainer != null ? defaultPlayerContainer : playerContainer_Self);
					break;
			}

			// Texts
			nameText.text = LimitByGraphemes(playerName?.Trim() ?? "", MaxNameGraphemes, true);
			this.scoreText.text = $"{scoreText}";

			if (iconImage != null)
			{
				if (!useCountryFlag)
				{
					ApplyAvatarIcon(avatarSprite);
					//ApplyFrame(frameItem);
				}
				else
				{
					ApplyCountryFlag(countryName);
				}
			}
		}

		private void ApplyAvatarIcon(Sprite avatarSprite)
		{
			if (iconImage == null)
				return;

			Sprite spriteToUse = avatarSprite ?? defaultIconSprite;
			if (spriteToUse != null)
			{
				iconImage.sprite = spriteToUse;
				iconImage.enabled = true;
			}
			else
			{
				iconImage.enabled = false;
			}
		}


		private void ApplyCountryFlag(string countryName)
		{
			if (iconImage == null)
				return;

			string iso = CountryIsoUtil.ToIso2OrEmpty(countryName);
			if (string.IsNullOrEmpty(iso)) iso = CountryIsoUtil.ToIso2OrEmpty(CountryService.CountryISO);

			if (countryFlags != null && !string.IsNullOrEmpty(iso))
			{
				string displayName = countryFlags.GetDisplayName(iso);
				if (!string.IsNullOrWhiteSpace(displayName) && nameText != null)
					nameText.text = displayName;
			}

			Sprite flag = (countryFlags != null && !string.IsNullOrEmpty(iso))
				? countryFlags.GetSprite(iso)
				: null;

			Sprite spriteToUse = flag ?? defaultIconSprite;
			if (spriteToUse != null)
			{
				iconImage.sprite = spriteToUse;
				iconImage.enabled = true;
			}
			else
			{
				iconImage.enabled = false;
			}

			if (iconFrameImage != null)
				iconFrameImage.enabled = false;
		}

		private string LimitByGraphemes(string input, int max, bool addEllipsis)
		{
			if (string.IsNullOrEmpty(input) || max <= 0) return "";

			var e = StringInfo.GetTextElementEnumerator(input);
			var sb = new StringBuilder(input.Length);
			int count = 0;

			while (e.MoveNext())
			{
				if (count >= max) break;
				sb.Append(e.GetTextElement());
				count++;
			}

			string result = sb.ToString();
			if (addEllipsis && result.Length < input.Length)
				result += "…";
			return result;
		}
	}
}

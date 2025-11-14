using System;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace BumiMobile.IAPStore
{
    public sealed class TimerRewardsHolder : RewardsHolder
    {
        private const string DEFAULT_BUTTON_TEXT = "FREE";

        [Group("Settings")]
        [SerializeField] string saveID = "uniqueTimerSaveID";

        [Group("Settings"), Space]
        [SerializeField] Button button;

        [Group("Settings")]
        [SerializeField] TMP_Text timerText;
        [Group("Settings")]
        [SerializeField] int timerDurationInMinutes;

        private DateTime timerStartTime;

        private StringBuilder sb;

    private const string PlayerPrefsKeyPrefix = "TimerProduct_";
#if MODULE_SAVE
    private SimpleLongSave save;
#endif

        private void Awake()
        {
            InitializeComponents();

            long storedTicks = 0;
#if MODULE_SAVE
            save = SaveController.GetSaveObject<SimpleLongSave>($"TimerProduct_{saveID}");
            storedTicks = save.Value;
#else
            string playerPrefsKey = PlayerPrefsKeyPrefix + saveID;
            if (PlayerPrefs.HasKey(playerPrefsKey))
            {
                long.TryParse(PlayerPrefs.GetString(playerPrefsKey), out storedTicks);
            }
#endif

            if (storedTicks == 0)
            {
                timerStartTime = DateTime.Now - TimeSpan.FromMinutes(timerDurationInMinutes);
            }
            else
            {
                timerStartTime = DateTime.FromBinary(storedTicks);
            }

            // Check if rewards needs to be disabled
            for (int i = 0; i < rewards.Length; i++)
            {
                if (rewards[i].CheckDisableState())
                {
                    // Disable holder game object
                    gameObject.SetActive(false);

                    return;
                }
            }

            sb = new StringBuilder();

            button.onClick.AddListener(OnButtonClicked);
        }

        private string FormatTimer(TimeSpan timeSpan)
        {
            sb.Clear();

            if(timeSpan.Hours > 0)
            {
                sb.Append(timeSpan.Hours);
                sb.Append(':');
            }

            sb.Append(timeSpan.Minutes.ToString("00"));
            sb.Append(':');

            sb.Append(timeSpan.Seconds.ToString("00"));

            return sb.ToString();
        }

        private void Update()
        {
            TimeSpan elapsed = DateTime.Now - timerStartTime;
            TimeSpan duration = TimeSpan.FromMinutes(timerDurationInMinutes);
            if (elapsed >= duration)
            {
                button.interactable = true;

                timerText.text = DEFAULT_BUTTON_TEXT;
            }
            else
            {
                button.interactable = false;

                timerText.text = FormatTimer(duration - elapsed);

                float prefferedWidth = timerText.preferredWidth;
                if (prefferedWidth < 270) prefferedWidth = 270;

                timerText.rectTransform.sizeDelta = timerText.rectTransform.sizeDelta.SetX(prefferedWidth + 5);
                button.image.rectTransform.sizeDelta = button.image.rectTransform.sizeDelta.SetX(prefferedWidth + 10);
            }
        }

        public bool IsAvailable()
        {
            TimeSpan timer = DateTime.Now - timerStartTime;
            TimeSpan duration = TimeSpan.FromMinutes(timerDurationInMinutes);

            return timer >= duration;
        }

        private void OnButtonClicked()
        {
#if MODULE_HAPTIC
            Haptic.Play(Haptic.HAPTIC_LIGHT);
#endif

#if MODULE_AUDIO
            AudioController.PlaySound(AudioController.AudioClips.buttonSound);
#endif

            timerStartTime = DateTime.Now;

#if MODULE_SAVE
            save.Value = timerStartTime.ToBinary();
            SaveController.MarkAsSaveIsRequired();
#else
            string playerPrefsKey = PlayerPrefsKeyPrefix + saveID;
            PlayerPrefs.SetString(playerPrefsKey, timerStartTime.ToBinary().ToString());
            PlayerPrefs.Save();
#endif

            ApplyRewards();
        }
    }
}
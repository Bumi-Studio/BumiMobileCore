using System;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

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

        private SimpleLongSave save;
        private DateTime timerStartTime;

        private StringBuilder sb;

        private void Awake()
        {
            InitializeComponents();

            save = SaveController.GetSaveObject<SimpleLongSave>($"TimerProduct_{saveID}");

            timerStartTime = DateTime.FromBinary(save.Value);

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
            TimeSpan timer = DateTime.Now - timerStartTime;
            TimeSpan duration = TimeSpan.FromMinutes(timerDurationInMinutes);
            if (timer > duration)
            {
                button.enabled = true;

                timerText.text = DEFAULT_BUTTON_TEXT;
            }
            else
            {
                button.enabled = false;

                timerText.text = FormatTimer(duration - timer);

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

            return timer > duration;
        }

        private void OnButtonClicked()
        {
#if MODULE_HAPTIC
            Haptic.Play(Haptic.HAPTIC_LIGHT);
#endif

            AudioController.PlaySound(AudioController.AudioClips.buttonSound);

            save.Value = DateTime.Now.ToBinary();
            timerStartTime = DateTime.Now;

            ApplyRewards();

            SaveController.MarkAsSaveIsRequired();
        }
    }
}
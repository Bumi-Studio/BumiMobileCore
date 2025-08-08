using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace BumiMobile
{
    public class HapticTestController : MonoBehaviour
    {
        [BoxGroup("Reference", "Settings")]
        [SerializeField]
        private Slider durationSlider;
        [BoxGroup("Reference", "Settings")]
        [SerializeField]
        private Slider intensitySlider;
        [BoxGroup("Reference", "Settings")]
        [SerializeField]
        private Toggle advanceToggle;
        [BoxGroup("Reference", "Settings")]
        [SerializeField]
        private Toggle dynamicToggle;
        [BoxGroup("Reference", "Settings")]
        [SerializeField]
        private Slider minDelaySlider;
        [BoxGroup("Reference", "Settings")]
        [SerializeField]
        private Slider minIntensitySlider;
        [BoxGroup("Reference", "Settings")]
        [SerializeField]
        private Slider maxIntensitySlider;
        [BoxGroup("Reference", "Settings")]
        [SerializeField]
        private Slider intensityStepSlider;
        [BoxGroup("Reference", "Settings")]
        [SerializeField]
        private Slider intensityResetTimerSlider;
        
        [SerializeField] HapticHandler longHapticHandler;
        
        [BoxGroup("Components", "Button")]
        [SerializeField] private Button backButton;
        
        private float lastPlayedTime = float.MinValue;

        private int currentIntensityStep;
        private float lastIntensityStepTime;

        private void Awake()
        {
            backButton.onClick.AddListener(() =>
            {
                SceneManager.LoadScene("Example Scene");
            });
        }

        public void Play()
        {
            if (advanceToggle.isOn)
            {
                if (Time.timeSinceLevelLoad < lastPlayedTime + minDelaySlider.value) return;

                lastPlayedTime = Time.timeSinceLevelLoad;

                if (dynamicToggle.isOn)
                {
                    currentIntensityStep++;

                    if (Time.timeSinceLevelLoad > lastIntensityStepTime)
                        currentIntensityStep = 0;

                    lastIntensityStepTime = Time.timeSinceLevelLoad + intensityResetTimerSlider.value;

                    Haptic.Play(durationSlider.value, 
                        Mathf.Lerp(
                            minIntensitySlider.value, 
                            maxIntensitySlider.value,
                        (float)currentIntensityStep / intensityStepSlider.value));
                }
                else
                {
                    Haptic.Play(durationSlider.value, intensitySlider.value);
                }
            }
            else
            {
                Haptic.Play(durationSlider.value, intensitySlider.value);
            }
        }
    }
}

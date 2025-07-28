using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace BumiMobile
{
    public class HapticTestController : MonoBehaviour
    {
        [SerializeField] HapticHandler longHapticHandler;
        
        [BoxGroup("Components", "Button")]
        [SerializeField] private Button backButton;

        private void Awake()
        {
            backButton.onClick.AddListener(() =>
            {
                SceneManager.LoadScene("Example Scene");
            });
        }

        public void Play()
        {
            Haptic.Play(0.3f);
        }

        public void PlayLong()
        {
            longHapticHandler.Play();
        }

        public void PlayLight()
        {
            Haptic.Play(Haptic.HAPTIC_LIGHT);
        }

        public void PlayMedium()
        {
            Haptic.Play(Haptic.HAPTIC_MEDIUM);
        }

        public void PlayHard()
        {
            Haptic.Play(Haptic.HAPTIC_HARD);
        }

        public void PlayPattern()
        {
            Haptic.Play(Haptic.PATTERN_LIGHT);
        }
    }
}

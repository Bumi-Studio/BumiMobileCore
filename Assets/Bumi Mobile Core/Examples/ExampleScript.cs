using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace BumiMobile
{
    public class ExampleScript : MonoBehaviour
    {
        [BoxGroup("Components", "Button")]
        [SerializeField] private Button tweenButton;
        [BoxGroup("Components", "Button")]
        [SerializeField] private Button localizationButton;
        [BoxGroup("Components", "Button")]
        [SerializeField] private Button hapticButton;
        [BoxGroup("Components", "Button")]
        [SerializeField] private Button adsButton;
        [BoxGroup("Components", "Button")]
        [SerializeField] private Button iapButton;

        private void Awake()
        {
            tweenButton.onClick.AddListener(() => LoadScene("Tween Example Scene"));
            localizationButton.onClick.AddListener(() => LoadScene("Localization Example Scene"));
            hapticButton.onClick.AddListener(() => LoadScene("Haptic Example Scene"));
            adsButton.onClick.AddListener(() => LoadScene("Ads Example Scene"));
            iapButton.onClick.AddListener(() => LoadScene("IAP Example Scene"));
        }

        private void LoadScene(string sceneName)
        {
            SceneManager.LoadScene(sceneName);
        }
    }
}

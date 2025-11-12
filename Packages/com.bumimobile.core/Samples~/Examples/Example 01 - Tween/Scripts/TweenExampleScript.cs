using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace BumiMobile
{
    public class TweenExampleScript : MonoBehaviour
    {
        [BoxGroup("Components", "Panel")]
        [SerializeField] private ItemTweenScript itemTweenScript;
        [BoxGroup("Components", "Button")]
        [SerializeField] private Button openButton;

        [BoxGroup("Components", "Button")] 
        [SerializeField] private Button backButton;

        private void Awake()
        {
            openButton.onClick.AddListener(itemTweenScript.PlayShowAnimation);
            backButton.onClick.AddListener(() =>
            {
                SceneManager.LoadScene("Example Scene");
            });
        }
    }
}

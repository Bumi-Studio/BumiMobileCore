using System;
using UnityEngine;
using UnityEngine.UI;

namespace BumiMobile
{
    public class ItemTweenScript : MonoBehaviour
    {
        [BoxGroup("Components", "Canvas")]
        [SerializeField] private Canvas canvas;
        [BoxGroup("Components", "Button")]
        [SerializeField] private Button closeButton;
        [BoxGroup("Components", "Button")]
        [SerializeField] private Transform panelTransform;
        
        private TweenCase scaleTweenCase;

        private void Awake()
        {
            canvas.enabled = false;
            closeButton.onClick.AddListener(PlayHideAnimation);
        }

        public void PlayShowAnimation()
        {
            canvas.enabled = true;

            scaleTweenCase?.KillActive();
            
            panelTransform.localScale = Vector3.zero;
            scaleTweenCase = panelTransform.DOPushScale(Vector3.one * 1.1f, Vector3.one, 0.5f * 0.64f, 0.5f * 0.36f, Ease.Type.CubicOut, Ease.Type.CubicIn, 0).OnComplete(() =>
            {
               
            });
        }
        
        private void PlayHideAnimation()
        {
            scaleTweenCase?.KillActive();

            panelTransform.localScale = Vector3.one;
            scaleTweenCase = panelTransform.DOPushScale(Vector3.one * 1.1f, Vector3.zero, 0.5f * 0.64f, 0.5f * 0.36f,
                    Ease.Type.CubicOut, Ease.Type.CubicIn, 0)
                .OnComplete(() =>
                {
                    canvas.enabled = false;
                });
        }
    }
}

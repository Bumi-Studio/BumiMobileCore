using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace BumiMobile
{
    [System.Serializable]
    public abstract class UILeaderboardButton
    {
        [SerializeField] Button button;
        public Button Button => button;
        
        [SerializeField]
        private bool selected = true;

        public void Init(UnityAction<bool> buttonAction)
        {
            button.onClick.AddListener(() =>
            {
                selected = !selected;
                buttonAction?.Invoke(selected);
            });
        }

        public abstract void OnSelected(bool isOn);
    }

    [System.Serializable]
    public class UIPlayerButton : UILeaderboardButton
    {
        [BoxGroup("UI")]
        [SerializeField]
        private Image image;
        [SerializeField]
        private Sprite activeSprite;
        [SerializeField]
        private Sprite inactiveSprite;
        [SerializeField]
        private List<GameObject> enabledGameObjects =  new List<GameObject>();
        
        public override void OnSelected(bool isOn)
        {
#if MODULE_HAPTIC
            Haptic.Play(Haptic.HAPTIC_LIGHT);
#endif
            AudioController.PlaySound(AudioController.buttonSound);
            
            image.sprite = isOn ? activeSprite : inactiveSprite;
            foreach (var enabledGameObject in enabledGameObjects)
            {
                enabledGameObject.SetActive(isOn);
            }
        }
    }
    
    [System.Serializable]
    public class UICountryButton : UILeaderboardButton
    {
        [BoxGroup("UI")]
        [SerializeField]
        private Image image;
        [SerializeField]
        private Sprite activeSprite;
        [SerializeField]
        private Sprite inactiveSprite;
        [SerializeField]
        private List<GameObject> disableGameObjects =  new List<GameObject>();
        
        public override void OnSelected(bool isOn)
        {
            image.sprite = isOn ? activeSprite : inactiveSprite;
            foreach (var disableGameObject in disableGameObjects)
            {
                disableGameObject.SetActive(!isOn);
            }
        }
        
    }
    
    [System.Serializable]
    public class UIGlobalButton : UILeaderboardButton
    {
        [BoxGroup("UI")]
        [SerializeField]
        private Image image;
        [BoxGroup("UI")]
        [SerializeField]
        private Sprite activeSprite;
        [BoxGroup("UI")]
        [SerializeField]
        private Sprite inactiveSprite;
        public override void OnSelected(bool isOn)
        {
            image.sprite = isOn ? activeSprite : inactiveSprite;
        }
    }

    [System.Serializable]
    public class UIRegionalButton : UILeaderboardButton
    {
        [BoxGroup("UI")]
        [SerializeField]
        private Image image;
        [BoxGroup("UI")]
        [SerializeField]
        private Sprite activeSprite;
        [BoxGroup("UI")]
        [SerializeField]
        private Sprite inactiveSprite;
        
        public override void OnSelected(bool isOn)
        {
            image.sprite = isOn ? activeSprite : inactiveSprite;
        }
    }
    
    
}
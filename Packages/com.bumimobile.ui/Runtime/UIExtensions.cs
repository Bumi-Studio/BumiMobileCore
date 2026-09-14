using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace BumiMobile
{
    public static class UIExtensions
    {
        public static void ClickButton(this Button button)
        {
            EventSystem.current.SetSelectedGameObject(button.gameObject);

            Tween.NextFrame(() =>
            {
                var eventData = new PointerEventData(EventSystem.current)
                {
                    button = PointerEventData.InputButton.Left
                };

                button.OnPointerClick(eventData);

                Tween.DelayedCall(0.2f, () => EventSystem.current.SetSelectedGameObject(null));
            }, unscaledTime: true);
        }

        public static void AddEvent(this Component behaviour, EventTriggerType triggerType, Action<PointerEventData> call)
        {
            AddEvent(behaviour.gameObject, triggerType, call);
        }

        public static void AddEvent(this GameObject behaviour, EventTriggerType triggerType, Action<PointerEventData> call)
        {
            var trigger = behaviour.GetComponent<EventTrigger>();
            if (trigger == null)
            {
                trigger = behaviour.gameObject.AddComponent<EventTrigger>();
            }

            var entry = new EventTrigger.Entry { eventID = triggerType };
            entry.callback.AddListener(data => call((PointerEventData)data));

            trigger.triggers.Add(entry);
        }
    }
}

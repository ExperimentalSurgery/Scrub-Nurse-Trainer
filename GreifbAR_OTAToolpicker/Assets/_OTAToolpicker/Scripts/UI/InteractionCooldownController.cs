using System.Collections;
using System.Collections.Generic;
using System.Text;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace NMY.OTAToolpicker
{
    public class InteractionCooldownController : MonoBehaviour
    {
        public float cooldownTime;

        private Button[] _buttons;
        private bool _isCoolingDown;
        private float _currentCooldown;

        private void Awake()
        {
            _buttons = FindObjectsOfType<Button>(true);
            _isCoolingDown = false;
            _currentCooldown = 0f;

            foreach (Button button in _buttons)
            {
                EventTrigger trigger = button.transform.AddComponent<EventTrigger>();
                EventTrigger.Entry entry = new EventTrigger.Entry();
                entry.eventID = EventTriggerType.PointerEnter;
                entry.callback.AddListener((eventData) => { this.OnPressButton(button); });
                entry.callback.AddListener((eventData) => { this.CoolDownEvents(); });
                trigger.triggers.Add(entry);
            }
        }

        public void OnPressButton(Button button)
        {
            if (_isCoolingDown || !button.interactable || !button.isActiveAndEnabled)
                return;

            button.onClick.Invoke();
        }

        public void CoolDownEvents()
        {
            _currentCooldown = 0f;
            _isCoolingDown = true;
        }

        private void Update()
        {
            if (_isCoolingDown)
            {
                _currentCooldown += Time.deltaTime;

                if (_currentCooldown >= cooldownTime)
                {
                    _isCoolingDown = false;
                    _currentCooldown = 0f;
                }
            }
        }
    }
}

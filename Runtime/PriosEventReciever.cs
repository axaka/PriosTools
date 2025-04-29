using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PriosTools
{
    public class PriosEventReciever : MonoBehaviour
    {
        public string EventKey = "";
        public ComponentTypes SelectedComponentTypes = ComponentTypes.None;
        public CanvasGroup CanvasGroup { get; private set; }
        public Image Image { get; private set; }
        public PriosUIAnimator UIAnimator { get; private set; }
        public TMP_Text Text { get; private set; }
        public Toggle Toggle { get; private set; }
        public TMP_InputField InputField { get; private set; }

        [Flags]
        public enum ComponentTypes
        {
            None = 0,
            GameObject = 1 << 0,
            UIAnimator = 1 << 1,
            CanvasGroup = 1 << 2,
            Image = 1 << 3,
            Text = 1 << 4,
            InputField = 1 << 5,
            Toggle = 1 << 6,
        }

        void Awake()
        {
            UIAnimator = GetComponent<PriosUIAnimator>();
            CanvasGroup = GetComponent<CanvasGroup>();
            Image = GetComponent<Image>();
            Text = GetComponent<TMP_Text>();
            InputField = GetComponent<TMP_InputField>();
            Toggle = GetComponent<Toggle>();

            PriosEvent.AddListener<object>(EventKey, OnEventTriggered);
        }

        private void OnEventTriggered(object obj)
        {
            // Update GameObject if flag is set
            if ((SelectedComponentTypes & ComponentTypes.GameObject) == ComponentTypes.GameObject && gameObject != null)
            {
                bool? newActiveState = obj switch
                {
                    bool b => b,
                    UnityEngine.Object => !gameObject.activeSelf, // Explicitly toggle if null
                    _ => null // Default to current state for anything else
                };

                if (newActiveState.HasValue) gameObject.SetActive(newActiveState.Value);
            }

            // Update UIAnimator if flag is set
            if ((SelectedComponentTypes & ComponentTypes.UIAnimator) == ComponentTypes.UIAnimator && UIAnimator != null)
            {
                bool? newActiveState = obj switch
                {
                    bool b => b,
                    UnityEngine.Object => !UIAnimator.Showing, // Explicitly toggle if null
                    _ => null // Default to current state for anything else
                };
                if (newActiveState.HasValue) UIAnimator.Run(newActiveState.Value);
            }

            // Update CanvasGroup if flag is set
            if ((SelectedComponentTypes & ComponentTypes.CanvasGroup) == ComponentTypes.CanvasGroup && CanvasGroup != null)
            {
                // Determine the new active state
                bool? newActiveState = obj switch
                {
                    bool b => b,
                    UnityEngine.Object => !CanvasGroup.interactable, // Explicitly toggle if null
                    _ => null // Default to current state for anything else
                };
                if (newActiveState.HasValue)
                {
                    CanvasGroup.alpha = newActiveState.Value ? 1 : 0; // Set alpha based on obj presence
                    CanvasGroup.interactable = newActiveState.Value; // Set interactable based on obj presence
                }
            }

            if ((SelectedComponentTypes & ComponentTypes.Image) == ComponentTypes.Image && Image != null)
            {
                Image.sprite = obj switch
                {
                    Sprite sprite => sprite,
                    UnityEngine.Object => null, // Set to null if obj is UnityEngine.Object
                    _ => Image.sprite // Default to current sprite for anything else
                };
            }

            // Update TMP_Text if flag is set
            if ((SelectedComponentTypes & ComponentTypes.Text) == ComponentTypes.Text && Text != null)
            {
                Text.text = obj != null ? obj.ToString() : "null";
            }

            // Update InputField if flag is set
            if ((SelectedComponentTypes & ComponentTypes.InputField) == ComponentTypes.InputField && InputField != null)
            {
                InputField.text = obj != null ? obj.ToString() : "null";
            }

            // Update Toggle if flag is set
            if ((SelectedComponentTypes & ComponentTypes.Toggle) == ComponentTypes.Toggle && Toggle != null)
            {
                Toggle.isOn = obj switch
                {
                    bool b => b,
                    UnityEngine.Object => !Toggle.isOn, // Toggle if null
                    _ => Toggle.isOn // Default for anything else
                };
            }
        }
    }
}
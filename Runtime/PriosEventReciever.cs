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
		public Slider Slider { get; private set; }
		public TMP_InputField InputField { get; private set; }
		public AudioSource AudioSource { get; private set; }

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
			Slider = 1 << 7,
			AudioSource = 1 << 8,
		}

		void Awake()
		{
			UIAnimator = GetComponent<PriosUIAnimator>();
			CanvasGroup = GetComponent<CanvasGroup>();
			Image = GetComponent<Image>();
			Text = GetComponent<TMP_Text>();
			InputField = GetComponent<TMP_InputField>();
			Toggle = GetComponent<Toggle>();
			Slider = GetComponent<Slider>();
			AudioSource = GetComponent<AudioSource>();

			PriosEvent.AddListener(EventKey, OnEventTriggered);
		}

		private void OnEventTriggered(object obj)
		{
			if ((SelectedComponentTypes & ComponentTypes.GameObject) == ComponentTypes.GameObject && gameObject != null)
			{
				bool? newActiveState = obj switch
				{
					bool b => b,
					UnityEngine.Object => !gameObject.activeSelf,
					_ => null
				};
				if (newActiveState.HasValue) gameObject.SetActive(newActiveState.Value);
			}

			if ((SelectedComponentTypes & ComponentTypes.UIAnimator) == ComponentTypes.UIAnimator && UIAnimator != null)
			{
				bool? newActiveState = obj switch
				{
					bool b => b,
					UnityEngine.Object => !UIAnimator.Showing,
					_ => null
				};
				if (newActiveState.HasValue) UIAnimator.Run(newActiveState.Value);
			}

			if ((SelectedComponentTypes & ComponentTypes.CanvasGroup) == ComponentTypes.CanvasGroup && CanvasGroup != null)
			{
				bool? newActiveState = obj switch
				{
					bool b => b,
					UnityEngine.Object => !CanvasGroup.interactable,
					_ => null
				};
				if (newActiveState.HasValue)
				{
					CanvasGroup.alpha = newActiveState.Value ? 1 : 0;
					CanvasGroup.interactable = newActiveState.Value;
				}
			}

			if ((SelectedComponentTypes & ComponentTypes.Image) == ComponentTypes.Image && Image != null)
			{
				var sprite = obj as Sprite;
				if (sprite != null)
				{
					Image.sprite = sprite;
				}
				else if (obj == null || (obj is UnityEngine.Object unityObj && unityObj == null))
				{
					Image.sprite = null;
				}
			}

			if ((SelectedComponentTypes & ComponentTypes.AudioSource) == ComponentTypes.AudioSource && AudioSource != null)
			{
				if (obj is AudioClip clip)
				{
					AudioSource.clip = clip;
					AudioSource.Play();
				}
				else if (obj == null || (obj is UnityEngine.Object unityObj && unityObj == null))
				{
					AudioSource.Stop();
				}
			}

			if ((SelectedComponentTypes & ComponentTypes.Text) == ComponentTypes.Text && Text != null)
			{
				Text.text = obj != null ? obj.ToString() : "null";
			}

			if ((SelectedComponentTypes & ComponentTypes.InputField) == ComponentTypes.InputField && InputField != null)
			{
				InputField.text = obj != null ? obj.ToString() : "null";
			}

			if ((SelectedComponentTypes & ComponentTypes.Toggle) == ComponentTypes.Toggle && Toggle != null)
			{
				Toggle.isOn = obj switch
				{
					bool b => b,
					UnityEngine.Object => !Toggle.isOn,
					_ => Toggle.isOn
				};
			}

			if ((SelectedComponentTypes & ComponentTypes.Slider) == ComponentTypes.Slider && Slider != null)
			{
				Slider.value = obj switch
				{
					float f => f,
					int i => i,
					_ => Slider.value,
				};
			}
		}
	}
}

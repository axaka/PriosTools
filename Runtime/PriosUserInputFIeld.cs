using TMPro;
using UnityEngine;

namespace PriosTools
{
	[ExecuteAlways]
	[RequireComponent(typeof(TMP_InputField))]
	public class PriosUserInputField : MonoBehaviour
	{
		public PriosUserData userData;
		public string userDataKey = "Name";

		private TMP_InputField inputField;

		private void Awake()
		{
			if (!IsValid()) return;
			EnsureComponent();
			InitializeInput();
			RegisterEvents();
		}

		private void OnEnable()
		{
			if (!IsValid()) return;
			EnsureComponent();
			InitializeInput();
			RegisterEvents();
		}

		private void OnDisable()
		{
			if (inputField != null)
				inputField.onEndEdit.RemoveListener(OnEndEdit);

			if (userData != null && !string.IsNullOrEmpty(userDataKey))
				userData.UnregisterOnChange(userDataKey, OnUserDataChanged);
		}

#if UNITY_EDITOR
		public void OnValidate()
		{
			if (!Application.isPlaying)
			{
				EnsureComponent();
				InitializeInput();
			}
		}
#endif

		private void EnsureComponent()
		{
			if (this == null || inputField != null)
				return;

			inputField = GetComponent<TMP_InputField>();
		}

		private bool IsValid()
		{
			return this;
		}

		private void InitializeInput()
		{
			if (inputField == null || userData == null || string.IsNullOrEmpty(userDataKey))
				return;

			string value = userData.Get(userDataKey);
			if (!string.IsNullOrEmpty(value))
				inputField.SetTextWithoutNotify(value);
		}

		private void RegisterEvents()
		{
			if (inputField != null)
				inputField.onEndEdit.AddListener(OnEndEdit);

			if (userData != null && !string.IsNullOrEmpty(userDataKey))
				userData.RegisterOnChange(userDataKey, OnUserDataChanged);
		}

		private void OnEndEdit(string newValue)
		{
			if (userData != null && !string.IsNullOrEmpty(userDataKey))
				userData.Set(userDataKey, newValue);
		}

		private void OnUserDataChanged(string newValue)
		{
			if (inputField != null && newValue != inputField.text)
				inputField.SetTextWithoutNotify(newValue);
		}
	}
}

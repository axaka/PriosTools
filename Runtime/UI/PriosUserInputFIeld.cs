using TMPro;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace PriosTools
{
	[ExecuteAlways]
	[RequireComponent(typeof(TMP_InputField))]
	public class PriosUserInputField : MonoBehaviour
	{
		public PriosUserData userData;
		[SerializeField] private string _userDataKey = "Name";
		public string userDataKey
		{
			get => _userDataKey;
			set
			{
				_userDataKey = value;
				InitializeInput();
			}
		}


		private TMP_InputField inputField;
		[SerializeField] private TMP_Text labelText;


		private void Awake()
		{
			if (!IsValidInstance()) return;
			EnsureComponent();
			InitializeInput();
			RegisterEvents();
		}

		private void OnEnable()
		{
			if (!IsValidInstance()) return;
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
		private void OnValidate()
		{
			if (!Application.isPlaying)
			{
				EditorApplication.delayCall += () =>
				{
					if (this != null)
					{
						EnsureComponent();
						InitializeInput();
					}
				};
			}
		}
#endif

		private void EnsureComponent()
		{
			if (inputField == null)
				inputField = GetComponent<TMP_InputField>();
		}

		private bool IsValidInstance()
		{
			return this != null && gameObject != null;
		}

		private void InitializeInput()
		{
			if (inputField == null || userData == null || string.IsNullOrEmpty(userDataKey))
				return;

			// Update input field with current value
			string value = userData.Get(userDataKey);
			if (!string.IsNullOrEmpty(value))
				inputField.SetTextWithoutNotify(value);

			// Update label text if assigned
			if (labelText != null)
				labelText.text = $"{userDataKey}";
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

		public void Refresh()
		{
#if UNITY_EDITOR
			if (!Application.isPlaying)
			{
				EnsureComponent();
				InitializeInput();
			}
#endif
		}

	}
}

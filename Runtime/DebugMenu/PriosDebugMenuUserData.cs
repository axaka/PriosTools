using System.Collections.Generic;
using UnityEngine;

namespace PriosTools
{
	public class PriosDebugMenuUserData : MonoBehaviour
	{
		[Header("References")]
		public PriosUserData userData;
		public Transform contentArea;

		[Header("Prefabs")]
		public GameObject inputFieldPrefab; // Should have a PriosUserInputField attached

		private readonly List<GameObject> spawnedFields = new();

		private void Start()
		{
			if (userData == null || contentArea == null || inputFieldPrefab == null)
			{
				Debug.LogWarning("[PriosDebugMenuUserDataEditor] Missing references.");
				return;
			}

			PopulateFields();
		}

		private void PopulateFields()
		{
			foreach (Transform child in contentArea)
				Destroy(child.gameObject);

			foreach (string key in userData.GetDefinedKeys())
			{
				var go = Instantiate(inputFieldPrefab, contentArea);
				var inputField = go.GetComponent<PriosUserInputField>();
				if (inputField != null)
				{
					inputField.userDataKey = key;
					inputField.userData = userData;
#if UNITY_EDITOR
					inputField.Refresh(); // So it updates in Editor too
#endif
				}

				if (go.TryGetComponent<RectTransform>(out var rt))
					rt.sizeDelta = new Vector2(rt.sizeDelta.x, 35);

				go.SetActive(true);
				spawnedFields.Add(go);
			}
		}

		public void RefreshUI()
		{
			foreach (var go in spawnedFields)
			{
				var input = go.GetComponent<PriosUserInputField>();
				input?.Refresh();
			}
		}
	}
}

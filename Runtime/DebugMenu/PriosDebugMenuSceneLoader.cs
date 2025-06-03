using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

namespace PriosTools
{
	public class PriosDebugMenuSceneLoader : MonoBehaviour
	{
		[Serializable]
		public class Prefabs
		{
			public GameObject sceneButton;
		}

		[Serializable]
		public class Transforms
		{
			public Transform contentArea;
		}

		public Prefabs prefabs;
		public Transforms transforms;

		private void Start()
		{
			LoadAllSceneButtons();
		}

		private void LoadAllSceneButtons()
		{
			if (prefabs.sceneButton == null || transforms.contentArea == null)
			{
				Debug.LogWarning("Scene button prefab or content area not assigned.");
				return;
			}

			// Clear previous buttons
			foreach (Transform child in transforms.contentArea)
			{
				Destroy(child.gameObject);
			}

			int sceneCount = SceneManager.sceneCountInBuildSettings;

			for (int i = 0; i < sceneCount; i++)
			{
				string scenePath = SceneUtility.GetScenePathByBuildIndex(i);
				string sceneName = System.IO.Path.GetFileNameWithoutExtension(scenePath);

				GameObject buttonGO = Instantiate(prefabs.sceneButton, transforms.contentArea);

				// Set height explicitly
				if (buttonGO.TryGetComponent<RectTransform>(out var rt))
				{
					rt.sizeDelta = new Vector2(rt.sizeDelta.x, 35);
				}

				TMP_Text label = buttonGO.GetComponentInChildren<TMP_Text>();
				if (label != null)
					label.text = sceneName;

				Button button = buttonGO.GetComponent<Button>();
				if (button != null)
				{
					int index = i;
					button.onClick.AddListener(() => SceneManager.LoadScene(index));
				}
			}
		}

	}
}

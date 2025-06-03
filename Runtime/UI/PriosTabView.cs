using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PriosTabView : MonoBehaviour
{
	[Header("Tab Configuration")]
	public List<Tab> tabs;

	[Serializable]
	public class Tab
	{
		public bool enabled = true; // Enable/disable this tab
		public string name;
		public GameObject content; // Scene object or prefab
		public bool resizeContent = true; // Per-tab setting
	}

	[Header("UI References")]
	public Transforms transforms;

	[Serializable]
	public class Transforms
	{
		public Transform tabButtonArea;
		public Transform tabContentArea;
	}

	[Header("Prefabs")]
	public Prefabs prefabs;

	[Serializable]
	public class Prefabs
	{
		public GameObject tabButtonPrefab;
	}

	private List<Button> tabButtons = new();
	private List<int> visibleTabIndices = new(); // maps UI buttons to real tab indices
	private Dictionary<int, GameObject> contentInstances = new();
	private int activeTabIndex = -1;

	private void Awake()
	{
		SetupTabs();
	}

	private void SetupTabs()
	{
		if (transforms.tabButtonArea == null || prefabs.tabButtonPrefab == null)
		{
			Debug.LogError("Tab button area or prefab not assigned.");
			return;
		}

		for (int i = 0; i < tabs.Count; i++)
		{
			if (!tabs[i].enabled)
				continue;

			int index = i;
			visibleTabIndices.Add(index);

			GameObject tabButtonObj = Instantiate(prefabs.tabButtonPrefab, transforms.tabButtonArea);
			Button button = tabButtonObj.GetComponent<Button>();

			if (button == null)
			{
				Debug.LogError("Tab button prefab must have a Button component.");
				continue;
			}

			tabButtons.Add(button);

			TMP_Text label = tabButtonObj.GetComponentInChildren<TMP_Text>();
			if (label != null)
				label.text = tabs[i].name;
			else
				Debug.LogWarning($"No TMP_Text found in tab button prefab for tab: {tabs[i].name}");

			button.onClick.AddListener(() => ShowTab(index));
		}

		if (visibleTabIndices.Count > 0)
			ShowTab(visibleTabIndices[0]);
	}

	private void ShowTab(int selectedIndex)
	{
		if (selectedIndex == activeTabIndex) return;

		// Disable previous content
		if (activeTabIndex >= 0 && contentInstances.TryGetValue(activeTabIndex, out var prevContent))
		{
			if (prevContent != null)
				prevContent.SetActive(false);
		}

		// Activate or instantiate new content
		if (contentInstances.TryGetValue(selectedIndex, out var existingContent))
		{
			existingContent.SetActive(true);
		}
		else
		{
			GameObject source = tabs[selectedIndex].content;
			GameObject instance = null;

			if (source.scene.IsValid() && source.activeInHierarchy)
			{
				source.transform.SetParent(transforms.tabContentArea, false);
				instance = source;
			}
			else
			{
				instance = Instantiate(source, transforms.tabContentArea);
			}

			if (tabs[selectedIndex].resizeContent && instance.TryGetComponent<RectTransform>(out var rt))
			{
				rt.anchorMin = Vector2.zero;
				rt.anchorMax = Vector2.one;
				rt.offsetMin = Vector2.zero;
				rt.offsetMax = Vector2.zero;
			}

			if (instance != null)
			{
				instance.SetActive(true);
				contentInstances[selectedIndex] = instance;
			}
		}

		// Update tab button states
		for (int i = 0; i < visibleTabIndices.Count; i++)
		{
			int tabIndex = visibleTabIndices[i];
			Button btn = tabButtons[i];
			btn.interactable = (tabIndex != selectedIndex);
		}

		activeTabIndex = selectedIndex;
	}
}

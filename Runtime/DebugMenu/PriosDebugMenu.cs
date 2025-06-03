using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace PriosTools
{
	public class PriosDebugMenu : PriosSingleton<PriosDebugMenu>
	{
		public KeyCode toggleKey = KeyCode.F9;
		public PriosUIAnimator panel;

		private void Update()
		{
			if (Input.GetKeyDown(toggleKey) && panel != null)
			{
				panel.Run(!panel.Showing);
			}
		}

		/// <summary>
		/// Automatically run on game start to spawn the debug menu if needed.
		/// </summary>
		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
		private static void AutoInitialize()
		{
			if (Instance == null)
			{
				var linkData = Resources.Load<PriosLinkData>("PriosLinkData");
				if (linkData != null && linkData.PriosDebugMenu != null)
				{
					Instantiate(linkData.PriosDebugMenu);
				}
				else
				{
					Debug.LogWarning("[PriosTools] Missing PriosLinkData asset or unassigned PriosDebugMenu prefab.");
				}
			}
		}
	}
}

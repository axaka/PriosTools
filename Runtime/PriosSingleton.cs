using UnityEngine;

namespace PriosTools
{
	public abstract class PriosSingleton<T> : MonoBehaviour where T : MonoBehaviour
	{
		private static T _instance;

#if UNITY_EDITOR
		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
		private static void ResetStaticInstance() { _instance = null; }
#endif

		public static T Instance
		{
			get
			{
				if (_instance == null)
				{
					if (!Application.isPlaying)
						return null;

					_instance = FindObjectOfType<T>();

					if (_instance == null)
					{
						// Try prefab from PriosLinkData only if T is PriosDebugMenu
						if (typeof(T) == typeof(PriosDebugMenu))
						{
							var linkData = Resources.Load<PriosLinkData>("PriosLinkData");
							if (linkData != null && linkData.PriosDebugMenu != null)
							{
								var obj = Instantiate(linkData.PriosDebugMenu);
								_instance = obj.GetComponentInChildren<T>(true);
							}
						}

						// Fallback for all other types
						if (_instance == null)
						{
							GameObject singletonObject = new GameObject(typeof(T).Name);
							_instance = singletonObject.AddComponent<T>();
						}
					}
				}

				return _instance;
			}
		}



		protected virtual void Awake()
		{
			if (_instance == null)
			{
				_instance = this as T;

				// Ensure we mark the root GameObject as persistent
				var root = transform.root.gameObject;
				DontDestroyOnLoad(root);
			}
			else if (_instance != this)
			{
				Destroy(gameObject);
			}
		}


		protected virtual void OnApplicationQuit()
		{
			Destroy(gameObject); // Or set instance to null
		}

	}
}
using UnityEngine;

namespace PriosTools
{
	public abstract class PriosSingleton<T> : MonoBehaviour where T : MonoBehaviour
	{
		private static T _instance;

		// Ensure static variables reset properly if domain reload is disabled
		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
		private static void ResetStaticInstance()
		{
			_instance = null;
		}

		public static T Instance
		{
			get
			{
				if (_instance == null)
				{
					// Prevent instance creation when exiting play mode
					if (!Application.isPlaying)
					{
						return null;
					}

					_instance = FindObjectOfType<T>();

					if (_instance == null)
					{
						GameObject singletonObject = new GameObject(typeof(T).Name);
						_instance = singletonObject.AddComponent<T>();
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
				DontDestroyOnLoad(gameObject);
			}
			else if (_instance != this)
			{
				Destroy(gameObject);
			}
		}
	}
}
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace PriosTools
{
	public class PriosSceneManager : MonoBehaviour
	{
		[SerializeField] private Type _type = Type.Name;
		[SerializeField] private string _sceneName = "";

		public enum Type
		{
			Name,
			Restart
		}

		private void Awake()
		{
			GetComponent<Button>().onClick.AddListener(OnLevelButtonClicked);
		}

		private void OnLevelButtonClicked()
		{
			switch (_type)
			{
				case Type.Name: SceneManager.LoadScene(_sceneName); break;
				case Type.Restart: SceneManager.LoadScene(gameObject.scene.name); break;
			}
		}
	}
}

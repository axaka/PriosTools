using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

namespace PriosTools
{
	public static class PriosWebTools
	{
		public static async Task<string> DownloadText(string url)
		{
			using UnityWebRequest request = UnityWebRequest.Get(url);
			var operation = request.SendWebRequest();

			while (!operation.isDone)
				await Task.Yield();

#if UNITY_2020_1_OR_NEWER
			if (request.result != UnityWebRequest.Result.Success)
#else
            if (request.isNetworkError || request.isHttpError)
#endif
			{
				Debug.LogError($"[PriosWebTools] Failed to download: {url}\n{request.error}");
				return null;
			}

			return request.downloadHandler.text;
		}
	}
}

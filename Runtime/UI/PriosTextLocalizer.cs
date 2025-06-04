using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using System.Text.RegularExpressions;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace PriosTools
{
	[ExecuteAlways]
	[RequireComponent(typeof(TMP_Text))]
	public class PriosTextLocalizer : MonoBehaviour
	{
		public string sheet;
		public string key;
		public PriosDataStore dataStore;
		public PriosUserData userData;

		public bool useTypewriterEffect = false;
		public bool supportRichText = true;
		public float typewriterSpeed = 0.05f;
		public AudioClip[] characterSounds;
		public Vector2 pitchRange = new Vector2(0.95f, 1.05f);

		private TMP_Text textComponent;
		private Coroutine typewriterCoroutine;
		private static readonly Regex PlaceholderRegex = new(@"\{([^\{\}]+)\}", RegexOptions.Compiled);
		private readonly List<string> watchedKeys = new();

		private void Awake()
		{
			if (!IsValidInstance()) return;
			EnsureComponent();
			RegisterKeyChangeCallback();
			UpdateText();
		}

		private void OnEnable()
		{
			if (!IsValidInstance()) return;
			EnsureComponent();
			RegisterKeyChangeCallback();
			UpdateText();
		}

		private void OnDisable()
		{
			if (!IsValidInstance()) return;
			UnregisterKeyChangeCallback();
		}

#if UNITY_EDITOR
		private void OnValidate()
		{
			if (!IsValidInstance()) return;
			EnsureComponent();

			// Do NOT call UpdateText here directly
			// Instead: refresh text without effects or audio
			if (!EditorApplication.isPlayingOrWillChangePlaymode)
			{
				EditorApplication.delayCall += () =>
				{
					if (this != null && !Application.isPlaying)
					{
						// Manually refresh text (but don't invoke full UpdateText or StartCoroutine)
						RefreshTextImmediately();
					}
				};
			}
		}
#else
		private void OnValidate()
		{
			if (IsValidInstance()) UpdateText();
		}
#endif

		private void EnsureComponent()
		{
			if (textComponent == null)
				textComponent = GetComponent<TMP_Text>();
		}

		private bool IsValidInstance() => this != null && gameObject != null;

		private void RegisterKeyChangeCallback()
		{
			if (userData == null || dataStore == null || string.IsNullOrEmpty(sheet) || string.IsNullOrEmpty(key))
				return;

			UnregisterKeyChangeCallback();

			string lang = userData.Get("Language");
			string rawText = dataStore.GetFieldValueByKey(sheet, "Key", key, lang);

			if (!string.IsNullOrEmpty(rawText))
			{
				foreach (Match match in PlaceholderRegex.Matches(rawText))
				{
					string watchKey = match.Groups[1].Value;
					if (!string.IsNullOrEmpty(watchKey))
					{
						userData.RegisterOnChange(watchKey, OnWatchedKeyChanged);
						watchedKeys.Add(watchKey);
					}
				}
			}

			userData.RegisterOnChange("Language", OnWatchedKeyChanged);
			watchedKeys.Add("Language");
		}

		private void UnregisterKeyChangeCallback()
		{
			if (userData == null) return;

			foreach (var k in watchedKeys)
				userData.UnregisterOnChange(k, OnWatchedKeyChanged);

			watchedKeys.Clear();
		}

		private void OnWatchedKeyChanged(string _) => UpdateText();

		public void UpdateText()
		{
			if (!IsValidInstance()) return;
			EnsureComponent();

			if (textComponent == null) return;

			if (dataStore == null || userData == null)
			{
				textComponent.text = "[Missing DataStore/UserData]";
				return;
			}

			if (string.IsNullOrEmpty(sheet) || string.IsNullOrEmpty(key))
			{
				textComponent.text = "[Missing Key]";
				return;
			}

			string lang = userData.Get("Language");
			if (string.IsNullOrEmpty(lang))
			{
				textComponent.text = "[Missing Language]";
				return;
			}

			string localizedText = dataStore.GetFieldValueByKey(sheet, "Key", key, lang);
			string finalText = !string.IsNullOrEmpty(localizedText) ? ReplacePlaceholders(localizedText) : $"[{key}]";

			// Fully prevent coroutines if not in play mode
#if UNITY_EDITOR
			if (!Application.isPlaying)
			{
				textComponent.text = finalText;
				return;
			}
#endif

			if (useTypewriterEffect)
			{
				if (typewriterCoroutine != null)
					StopCoroutine(typewriterCoroutine);

				typewriterCoroutine = StartCoroutine(TypeText(finalText));
			}
			else
			{
				textComponent.text = finalText;
			}
		}


		public void FinishTyping()
		{
			if (typewriterCoroutine != null)
			{
				StopCoroutine(typewriterCoroutine);
				typewriterCoroutine = null;

				string lang = userData.Get("Language");
				string rawText = dataStore.GetFieldValueByKey(sheet, "Key", key, lang);
				string fullText = !string.IsNullOrEmpty(rawText) ? ReplacePlaceholders(rawText) : $"[{key}]";

				textComponent.text = fullText;
			}
		}

		private IEnumerator TypeText(string text)
		{
			textComponent.text = "";

			List<string> openTags = new();
			string visibleText = "";
			int i = 0;

			while (i < text.Length)
			{
				if (supportRichText && text[i] == '<')
				{
					int closeIndex = text.IndexOf('>', i);
					if (closeIndex != -1)
					{
						string tag = text.Substring(i, closeIndex - i + 1);

						if (!tag.StartsWith("</") && !tag.EndsWith("/>"))
							openTags.Add(tag);
						else if (tag.StartsWith("</"))
						{
							string tagName = tag.Substring(2, tag.Length - 3);
							for (int t = openTags.Count - 1; t >= 0; t--)
							{
								if (openTags[t].Contains(tagName))
								{
									openTags.RemoveAt(t);
									break;
								}
							}
						}

						visibleText += tag;
						i = closeIndex + 1;
						continue;
					}
				}

				char currentChar = text[i];
				visibleText += currentChar;
				i++;

				// Character sound effect
				if (Application.isPlaying &&
					characterSounds != null &&
					characterSounds.Length > 0 &&
					!char.IsWhiteSpace(currentChar) &&
					!char.IsPunctuation(currentChar))
				{
					AudioClip clip = characterSounds[Random.Range(0, characterSounds.Length)];
					if (clip != null)
					{
						GameObject tempGO = new GameObject("CharSound");
						AudioSource src = tempGO.AddComponent<AudioSource>();
						src.clip = clip;
						src.spatialBlend = 0f; // 2D sound
						src.pitch = Random.Range(pitchRange.x, pitchRange.y);
						src.Play();

						Destroy(tempGO, clip.length / src.pitch); // clean up
					}
				}

				string fullText = visibleText;
				for (int t = openTags.Count - 1; t >= 0; t--)
				{
					string tagName = Regex.Match(openTags[t], @"<(\w+)").Groups[1].Value;
					fullText += $"</{tagName}>";
				}

				textComponent.text = fullText;
				yield return new WaitForSeconds(typewriterSpeed);
			}

			typewriterCoroutine = null; // Clear reference when done
		}

		private string ReplacePlaceholders(string text)
		{
			if (string.IsNullOrEmpty(text) || userData == null)
				return text;

			return PlaceholderRegex.Replace(text, match =>
			{
				string key = match.Groups[1].Value;
				string replacement = userData.Get(key);
				return string.IsNullOrEmpty(replacement) ? match.Value : replacement;
			});
		}

		private void RefreshTextImmediately()
		{
			if (textComponent == null || dataStore == null || userData == null)
				return;

			if (string.IsNullOrEmpty(sheet) || string.IsNullOrEmpty(key))
			{
				textComponent.text = "[Missing Key]";
				return;
			}

			string lang = userData.Get("Language");
			if (string.IsNullOrEmpty(lang))
			{
				textComponent.text = "[Missing Language]";
				return;
			}

			string rawText = dataStore.GetFieldValueByKey(sheet, "Key", key, lang);
			string finalText = !string.IsNullOrEmpty(rawText) ? ReplacePlaceholders(rawText) : $"[{key}]";

			textComponent.text = finalText;
		}

	}
}

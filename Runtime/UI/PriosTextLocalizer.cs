
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

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
		public float typewriterSpeed = 0.05f;
		public float speedUpMultiplier = 10f;
		public bool enablePagination = true;
		public bool autoDetectBounds = true;
		public float maxHeight = 500f;

		public bool scrollOneLineAtATime = true;

		public AudioClip[] characterSounds;
		public Vector2 pitchRange = new Vector2(0.95f, 1.05f);

		private TMP_Text textComponent;
		private AudioSource audioSource;
		private Coroutine typewriterCoroutine;

		private List<string> textLines = new();
		private List<string> visibleLines = new();
		private int maxVisibleLines = 1;
		private int currentLineIndex = 0;
		private bool isSpeedingUp = false;
		private bool isTypingInitialBlock = false;

		private static readonly Regex PlaceholderRegex = new(@"\{([^\{\}]+)\}", RegexOptions.Compiled);

		private bool IsValidInstance() => this != null && gameObject != null && textComponent != null;

		private void Awake()
		{
			EnsureComponent();
			RegisterKeyCallback();
#if UNITY_EDITOR
			if (!Application.isPlaying)
				UpdateText();
#endif
		}

		private void OnEnable()
		{
			EnsureComponent();
			RegisterKeyCallback();
			UpdateText();
		}

		private void OnDisable()
		{
			UnregisterKeyCallback();
		}

		private void Start()
		{
			UpdateText();
			//if (Application.isPlaying)
			//	StartCoroutine(DelayedTextUpdate());
		}

		private IEnumerator DelayedTextUpdate()
		{
			yield return null; // Wait one frame
			UpdateText();
		}


#if UNITY_EDITOR
		private void OnValidate()
		{
			if (!IsValidInstance()) return;
			EnsureComponent();

			if (!EditorApplication.isPlayingOrWillChangePlaymode)
			{
				EditorApplication.delayCall += () =>
				{
					if (this != null && !Application.isPlaying)
						UpdateText();
				};
			}
		}
#else
        private void OnValidate()
        {
            UpdateText();
        }
#endif

		private void EnsureComponent()
		{
			if (textComponent == null)
				textComponent = GetComponent<TMP_Text>();

			textComponent.enableWordWrapping = true;
			textComponent.overflowMode = TextOverflowModes.Overflow;
			textComponent.alignment = TextAlignmentOptions.TopLeft;
			textComponent.enableAutoSizing = false;
			textComponent.lineSpacing = 0f;

			if (textComponent.margin == Vector4.zero)
				textComponent.margin = new Vector4(10, 10, 10, 10);

			// Ensure AudioSource exists
			if (audioSource == null)
			{
				audioSource = GetComponent<AudioSource>();
				if (audioSource == null)
					audioSource = gameObject.AddComponent<AudioSource>();

				audioSource.playOnAwake = false;
				audioSource.spatialBlend = 0f; // 2D sound
			}
		}

		private readonly List<string> watchedKeys = new();

		private void RegisterKeyCallback()
		{
			if (userData == null || dataStore == null || string.IsNullOrEmpty(sheet) || string.IsNullOrEmpty(key))
				return;

			UnregisterKeyCallback();

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

		private void UnregisterKeyCallback()
		{
			if (userData == null) return;

			foreach (var k in watchedKeys)
				userData.UnregisterOnChange(k, OnWatchedKeyChanged);

			watchedKeys.Clear();
		}

		private void OnWatchedKeyChanged(string _) => UpdateText();

		public void SetKeyAndShow(string newKey)
		{
			currentLineIndex = 0;
			isSpeedingUp = false;
			isTypingInitialBlock = false;

			//StopAllCoroutines();
			typewriterCoroutine = null;

			visibleLines.Clear();
			textLines.Clear();
			textComponent.text = "";

			key = newKey;
			UpdateText();
		}

		public void UpdateText()
		{
			StopAllCoroutines();

			if (textComponent == null || dataStore == null || userData == null || string.IsNullOrEmpty(sheet) || string.IsNullOrEmpty(key))
			{
				textComponent.text = "[Missing Configuration]";
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

#if UNITY_EDITOR
			if (!Application.isPlaying)
			{
				TruncateTextInEditor();
				return;
			}
#endif

			// Force layout rebuild before measuring text height
			Canvas.ForceUpdateCanvases();
			LayoutRebuilder.ForceRebuildLayoutImmediate(textComponent.rectTransform);

			if (!enablePagination && !useTypewriterEffect)
			{
				textComponent.text = finalText;
				currentLineIndex = textLines.Count;
			}
			else
			{
				GenerateTextLines(finalText);
				ShowInitialLinesTyped();
			}
		}

		private void GenerateTextLines(string fullText)
		{
			textLines.Clear();
			textComponent.text = fullText;
			textComponent.ForceMeshUpdate();

			var textInfo = textComponent.textInfo;
			var lines = new List<string>();

			bool previousLineWasEmpty = false;

			for (int i = 0; i < textInfo.lineCount; i++)
			{
				var lineInfo = textInfo.lineInfo[i];
				if (lineInfo.characterCount == 0) continue;

				int firstChar = lineInfo.firstCharacterIndex;
				int lastChar = lineInfo.lastCharacterIndex;

				if (firstChar < 0 || lastChar < 0 || lastChar >= textInfo.characterCount)
					continue;

				int start = textInfo.characterInfo[firstChar].index;
				int end = textInfo.characterInfo[lastChar].index + textInfo.characterInfo[lastChar].stringLength;

				start = Mathf.Clamp(start, 0, fullText.Length);
				end = Mathf.Clamp(end, start, fullText.Length);

				string lineText = fullText.Substring(start, end - start).TrimEnd();

				// Skip purely empty or whitespace lines, unless previous was non-empty
				if (string.IsNullOrWhiteSpace(lineText))
				{
					if (previousLineWasEmpty) continue; // collapse multiple empty lines
					previousLineWasEmpty = true;
				}
				else
				{
					previousLineWasEmpty = false;
				}

				lines.Add(lineText);
			}

			textLines = lines;

			// Estimate max visible lines
			float containerHeight = autoDetectBounds ? GetEffectiveTextHeight() : maxHeight;
			float totalHeight = 0f;
			int lineCount = textComponent.textInfo.lineCount;

			for (int i = 0; i < lineCount; i++)
			{
				var line = textComponent.textInfo.lineInfo[i];
				totalHeight += line.lineHeight;
			}

			float averageLineHeight = lineCount > 0 ? totalHeight / lineCount : textComponent.fontSize + textComponent.lineSpacing;
			maxVisibleLines = Mathf.FloorToInt(containerHeight / averageLineHeight);
			maxVisibleLines = Mathf.Max(1, maxVisibleLines); // Always show at least 1 line
		}

		private float GetEffectiveTextHeight()
		{
			Rect rect = textComponent.rectTransform.rect;
			float topMargin = textComponent.margin.y;
			float bottomMargin = textComponent.margin.w;

			// TextMeshPro uses margins in local units, which can mismatch with pixel values,
			// but for practical use this still provides a visually aligned estimate.
			return Mathf.Max(0, rect.height - topMargin - bottomMargin - 1);
		}

		private void ShowInitialLinesTyped()
		{
			visibleLines = new List<string>();
			textComponent.text = "";
			currentLineIndex = 0;

			if (useTypewriterEffect && Application.isPlaying)
			{
				// Pre-fill with empty lines to reserve visual space
				int initialCount = enablePagination
					? Mathf.Min(maxVisibleLines, textLines.Count)
					: textLines.Count;

				for (int i = 0; i < initialCount; i++)
					visibleLines.Add(""); // Reserve lines

				if (typewriterCoroutine != null)
					StopCoroutine(typewriterCoroutine);

				isTypingInitialBlock = true;
				typewriterCoroutine = StartCoroutine(TypeMultipleInitialLines());
			}
			else
			{
				if (!enablePagination && !useTypewriterEffect)
				{
					textComponent.text = string.Join("", textLines); // No forced newlines
					currentLineIndex = textLines.Count;
					return;
				}

				int linesToShow = enablePagination ? Mathf.Min(maxVisibleLines, textLines.Count) : textLines.Count;
				visibleLines.AddRange(textLines.Take(linesToShow));
				textComponent.text = string.Join("\n", visibleLines);
				currentLineIndex = linesToShow;
			}
		}

		private IEnumerator TypeMultipleInitialLines()
		{
			for (int i = 0; i < visibleLines.Count; i++)
			{
				string prefix = string.Join("\n", visibleLines.Take(i));
				if (!string.IsNullOrEmpty(prefix)) prefix += "\n";

				string line = textLines[i];
				yield return typewriterCoroutine = StartCoroutine(TypeLine(line, i, prefix));
			}

			currentLineIndex = visibleLines.Count;
			isTypingInitialBlock = false;
			isSpeedingUp = false;
			typewriterCoroutine = null;
		}

		public void Continue()
		{
			TryContinue();
		}

		public bool TryContinue()
		{
			// 1) If we’re mid-type, speed it up
			if (typewriterCoroutine != null)
			{
				isSpeedingUp = true;
				return true;
			}

			// 2) If we’re still typing the initial block, or out of lines, do nothing
			if (isTypingInitialBlock || currentLineIndex >= textLines.Count)
				return false;

			// 3) Otherwise, advance through lines
			while (currentLineIndex < textLines.Count)
			{
				string nextLine = textLines[currentLineIndex++];

				// Slide window if needed
				if (visibleLines.Count >= maxVisibleLines)
					visibleLines.RemoveAt(0);

				visibleLines.Add(nextLine);

				// If it’s a non-empty line, start typing it and return true
				if (!string.IsNullOrWhiteSpace(nextLine))
				{
					string prefix = string.Join("\n", visibleLines.Take(visibleLines.Count - 1));
					if (!string.IsNullOrEmpty(prefix)) prefix += "\n";

					typewriterCoroutine = StartCoroutine(TypeLine(nextLine, visibleLines.Count - 1, prefix));
					return true;
				}

				// If it was just whitespace, loop to the next line automatically
			}

			// No more lines to show
			return false;
		}

		private IEnumerator TypeLine(string line, int visibleLineIndex, string prefix)
		{
			string typed = prefix;
			List<string> openTags = new();
			int i = 0;

			while (i < line.Length)
			{
				// Handle tag
				if (line[i] == '<')
				{
					int closeIndex = line.IndexOf('>', i);
					if (closeIndex != -1)
					{
						string tag = line.Substring(i, closeIndex - i + 1);
						typed += tag;

						if (!tag.StartsWith("</") && !tag.EndsWith("/>"))
						{
							openTags.Add(tag);
						}
						else if (tag.StartsWith("</"))
						{
							string tagName = tag.Substring(2, tag.Length - 3);
							for (int t = openTags.Count - 1; t >= 0; t--)
							{
								if (Regex.Match(openTags[t], @"<(\w+)").Groups[1].Value == tagName)
								{
									openTags.RemoveAt(t);
									break;
								}
							}
						}

						i = closeIndex + 1;
						continue;
					}
				}

				// Normal character
				char c = line[i++];
				string displayText = typed + c;

				// Add closing tags
				for (int t = openTags.Count - 1; t >= 0; t--)
				{
					string tagName = Regex.Match(openTags[t], @"<(\w+)").Groups[1].Value;
					displayText += $"</{tagName}>";
				}

				textComponent.text = displayText;
				PlayCharacterSound(c);
				yield return new WaitForSeconds(GetDelayForCharacter(c));

				typed += c; // Add after delay to match audio pacing
			}

			// Finalize the line
			if (visibleLineIndex >= 0 && visibleLineIndex < visibleLines.Count)
				visibleLines[visibleLineIndex] = line;

			if (!isTypingInitialBlock)
				isSpeedingUp = false;

			typewriterCoroutine = null;
		}

		// Add this helper to your class:
		private void StopTypewriterEffect()
		{
			if (typewriterCoroutine != null)
			{
				StopCoroutine(typewriterCoroutine);
				typewriterCoroutine = null;
			}
			isTypingInitialBlock = false;
			isSpeedingUp = false;
		}


		private void PlayCharacterSound(char c)
		{
			if (!char.IsWhiteSpace(c) && characterSounds != null && characterSounds.Length > 0)
			{
				var clip = characterSounds[Random.Range(0, characterSounds.Length)];
				if (clip != null)
				{
					audioSource.pitch = Random.Range(pitchRange.x, pitchRange.y);
					audioSource.PlayOneShot(clip);
				}
			}
		}

		private float GetDelayForCharacter(char c)
		{
			float delay = typewriterSpeed;

			if (c == ',')
				delay *= 5f;
			else if (c == '.' || c == '!' || c == '?')
				delay *= 10f;

			if (isSpeedingUp)
				delay /= speedUpMultiplier;

			return delay;
		}

		private string ReplacePlaceholders(string input)
		{
			if (string.IsNullOrEmpty(input))
				return input ?? ""; // return empty string to be safe

			return PlaceholderRegex.Replace(input, match =>
			{
				string k = match.Groups[1].Value;
				return userData.Get(k) ?? match.Value;
			});
		}

		private void TruncateTextInEditor()
		{
			if (textComponent == null
				|| dataStore == null
				|| userData == null
				|| string.IsNullOrEmpty(sheet)
				|| string.IsNullOrEmpty(key))
				return;

			// Resolve full string
			string lang = userData.Get("Language");
			if (string.IsNullOrEmpty(lang)) return;

			string rawText = dataStore.GetFieldValueByKey(sheet, "Key", key, lang);
			string resolvedText = !string.IsNullOrEmpty(rawText)
				? ReplacePlaceholders(rawText)
				: $"[{key}]";

			// 1) Force the UI layout to update so we get correct rect sizes:
			Canvas.ForceUpdateCanvases();
#if UNITY_EDITOR
			// Rebuild this text's own RectTransform...
			LayoutRebuilder.ForceRebuildLayoutImmediate(textComponent.rectTransform);
			// ...and also its parent (if any) so VerticalLayoutGroup settles:
			if (transform.parent is RectTransform parentRect)
				LayoutRebuilder.ForceRebuildLayoutImmediate(parentRect);
#endif

			// 2) If pagination is off, dump full text:
			if (!enablePagination)
			{
				textComponent.text = resolvedText;
				return;
			}

			// 3) Otherwise split into lines same as at runtime and show up to maxVisibleLines:
			GenerateTextLines(resolvedText);
			int linesToShow = Mathf.Min(maxVisibleLines, textLines.Count);
			var truncated = textLines.Take(linesToShow);
			textComponent.text = string.Join("\n", truncated);
		}

		/// <summary>
		/// Returns true if the typewriter is currently animating text.
		/// </summary>
		public bool IsTyping => typewriterCoroutine != null || isTypingInitialBlock;

		/// <summary>
		/// Returns true if there are still lines left to show after the current one.
		/// </summary>
		public bool HasMoreText => currentLineIndex < textLines.Count;

		/// <summary>
		/// Returns true if the entire text block has been fully shown and no animation is running.
		/// </summary>
		public bool IsComplete => !IsTyping && !HasMoreText;

	}
}

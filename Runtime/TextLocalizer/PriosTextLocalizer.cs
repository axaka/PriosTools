// ...using statements remain unchanged...

using System.Collections.Generic;
using System.Text.RegularExpressions;
using TMPro;
using UnityEngine.UI;
using UnityEngine;
using System.Linq;
using System.Collections;

namespace PriosTools
{
	[ExecuteAlways]
	[RequireComponent(typeof(TMP_Text))]
	public class PriosTextLocalizer : MonoBehaviour
	{
		public TextLocalizerData settings;
		public TextLocalizerSettings settingsInstance = new();

		public TextLocalizerSettings ActiveSettings
		{
			get
			{
				if (settings != null) return settings.Settings;
				settingsInstance ??= new TextLocalizerSettings();
				return settingsInstance;
			}
		}

		public string key;

		private TMP_Text textComponent;
		private AudioSource audioSource;
		private Coroutine typewriterCoroutine;

		private List<(int start, int end)> lineSpans = new();
		private int maxVisibleLines = 1;
		private int currentLineIndex = 0;
		private bool isSpeedingUp = false;
		private bool isTypingInitialBlock = false;
		private static readonly Regex PlaceholderRegex = new(@"\{([^\{\}]+)\}", RegexOptions.Compiled);

		private string fullText = "";

		private void Awake()
		{
			EnsureComponent();
			RegisterKeyCallback();
#if UNITY_EDITOR
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
		}

#if UNITY_EDITOR
		private void OnValidate()
		{
			UpdateText();
		}
#else
        private void OnValidate() => UpdateText();
#endif

		private void EnsureComponent()
		{
			if (textComponent == null)
				textComponent = GetComponent<TMP_Text>();

			if (ActiveSettings.enablePagination)
			{
				textComponent.enableAutoSizing = false;
				textComponent.enableWordWrapping = true;
			}

			if (ActiveSettings.useTypewriterEffect)
			{
				//textComponent.overflowMode = TextOverflowModes.Overflow;
				//textComponent.alignment = TextAlignmentOptions.TopLeft;
				//textComponent.lineSpacing = 0f;

				if (audioSource == null && ActiveSettings.characterSounds.Length > 0)
				{
					audioSource = GetComponent<AudioSource>();
					if (audioSource == null)
						audioSource = gameObject.AddComponent<AudioSource>();

					audioSource.playOnAwake = false;
					audioSource.spatialBlend = 0f;
				}
			}



		}

		private readonly List<string> watchedKeys = new();

		private void RegisterKeyCallback()
		{
			if (ActiveSettings == null || ActiveSettings.userData == null || ActiveSettings.dataStore == null || string.IsNullOrEmpty(ActiveSettings.sheet) || string.IsNullOrEmpty(key))
				return;

			UnregisterKeyCallback();

			string lang = ActiveSettings.userData.Get("Language");
			string rawText = ActiveSettings.dataStore.GetFieldValueByKey(ActiveSettings.sheet, "Key", key, lang);

			if (!string.IsNullOrEmpty(rawText))
			{
				foreach (Match match in PlaceholderRegex.Matches(rawText))
				{
					string watchKey = match.Groups[1].Value;
					if (!string.IsNullOrEmpty(watchKey))
					{
						ActiveSettings.userData.RegisterOnChange(watchKey, OnWatchedKeyChanged);
						watchedKeys.Add(watchKey);
					}
				}
			}

			ActiveSettings.userData.RegisterOnChange("Language", OnWatchedKeyChanged);
			watchedKeys.Add("Language");
		}

		private void UnregisterKeyCallback()
		{
			if (ActiveSettings == null || ActiveSettings.userData == null) return;

			foreach (var k in watchedKeys)
				ActiveSettings.userData.UnregisterOnChange(k, OnWatchedKeyChanged);

			watchedKeys.Clear();
		}

		private void OnWatchedKeyChanged(string _) => UpdateText();

		public void SetKeyAndShow(string newKey)
		{
			currentLineIndex = 0;
			isSpeedingUp = false;
			isTypingInitialBlock = false;
			typewriterCoroutine = null;
			textComponent.text = "";
			key = newKey;
			UpdateText();
		}

		public void UpdateText()
		{
			if (!textComponent) return;

			if (ActiveSettings == null ||
				ActiveSettings.dataStore == null ||
				ActiveSettings.userData == null ||
				string.IsNullOrEmpty(ActiveSettings.sheet))
			{
				textComponent.text = "[Missing Configuration]";
				return;
			}

			string lang = ActiveSettings.userData.Get("Language");
			if (string.IsNullOrEmpty(lang))
			{
				textComponent.text = "[Missing Language]";
				return;
			}

#if UNITY_EDITOR
			if (!Application.isPlaying)
			{
				textComponent.text = string.IsNullOrEmpty(key) ? "[Missing Key]" : $"[{key}]";
				return;
			}
#endif

			StopAllCoroutines();
			typewriterCoroutine = null;
			isTypingInitialBlock = false;
			isSpeedingUp = false;

			string rawText = ActiveSettings.dataStore.GetFieldValueByKey(ActiveSettings.sheet, "Key", key, lang);
			fullText = !string.IsNullOrEmpty(rawText) ? ApplyReplacementsAndPlaceholders(rawText) : $"";

			// Force layout rebuild before measuring text height
			Canvas.ForceUpdateCanvases();
			LayoutRebuilder.ForceRebuildLayoutImmediate(textComponent.rectTransform);

			GenerateLineSpans(fullText);
			ShowInitialLinesTyped();
		}

		private void GenerateLineSpans(string text)
		{
			lineSpans.Clear();
			textComponent.text = text;
			textComponent.ForceMeshUpdate();

			var textInfo = textComponent.textInfo;
			int lineCount = textInfo.lineCount;

			for (int i = 0; i < lineCount; i++)
			{
				var lineInfo = textInfo.lineInfo[i];
				if (lineInfo.characterCount == 0) continue;
				int start = textInfo.characterInfo[lineInfo.firstCharacterIndex].index;
				int end = textInfo.characterInfo[lineInfo.lastCharacterIndex].index + textInfo.characterInfo[lineInfo.lastCharacterIndex].stringLength;
				lineSpans.Add((start, end));
			}

			// Pagination calc
			float containerHeight = ActiveSettings.autoDetectBounds ? GetEffectiveTextHeight() : ActiveSettings.maxHeight;
			float totalHeight = 0f;
			for (int i = 0; i < lineCount; i++)
				totalHeight += textComponent.textInfo.lineInfo[i].lineHeight;
			float avgLine = lineCount > 0 ? totalHeight / lineCount : textComponent.fontSize + textComponent.lineSpacing;
			maxVisibleLines = Mathf.FloorToInt(containerHeight / avgLine);
			maxVisibleLines = Mathf.Max(1, maxVisibleLines);
		}

		private float GetEffectiveTextHeight()
		{
			Rect rect = textComponent.rectTransform.rect;
			float topMargin = textComponent.margin.y;
			float bottomMargin = textComponent.margin.w;
			return Mathf.Max(0, rect.height - topMargin - bottomMargin - 1);
		}

		private void ShowInitialLinesTyped()
		{
			isTypingInitialBlock = false;
			typewriterCoroutine = null;

			int firstCount = ActiveSettings.enablePagination
				? Mathf.Min(maxVisibleLines, lineSpans.Count)
				: lineSpans.Count;

			currentLineIndex = firstCount;

			string block = GetLinesWithTagContext(0, firstCount);

			if (ActiveSettings.useTypewriterEffect && Application.isPlaying)
			{
				isTypingInitialBlock = true;
				// For the first block: show nothing as prefix, all visible lines as new content
				typewriterCoroutine = StartCoroutine(RevealRichText("", block, () => isTypingInitialBlock = false));
			}
			else
			{
				textComponent.text = block;
			}
		}

		private List<string> GetOpenTagsUpTo(string text, int endIndex)
		{
			List<string> openTags = new List<string>();
			Stack<string> tempTags = new Stack<string>();
			var tagRegex = new Regex(@"<.*?>", RegexOptions.Compiled);

			foreach (Match m in tagRegex.Matches(text))
			{
				if (m.Index >= endIndex)
					break;
				string tag = m.Value;
				if (tag.StartsWith("</"))
				{
					if (tempTags.Count > 0)
						tempTags.Pop();
				}
				else if (!tag.EndsWith("/>"))
				{
					tempTags.Push(tag);
				}
			}
			if (tempTags.Count > 0)
				openTags.AddRange(tempTags.Reverse());
			return openTags;
		}

		private List<string> GetOpenTagNames(List<string> openTags)
		{
			return openTags
				.Select(t => Regex.Match(t, @"<(\w+)").Groups[1].Value)
				.Where(n => !string.IsNullOrEmpty(n))
				.ToList();
		}

		private string GetLinesWithTagContext(int from, int count)
		{
			if (lineSpans.Count == 0 || from >= lineSpans.Count) return "";
			int lastLine = Mathf.Min(from + count, lineSpans.Count) - 1;
			int start = lineSpans[from].start;
			int end = lineSpans[lastLine].end;

			List<string> openTags = GetOpenTagsUpTo(fullText, start);
			List<string> openNames = GetOpenTagNames(openTags);

			string block = fullText.Substring(start, end - start);

			var sb = new System.Text.StringBuilder();
			if (openTags.Count > 0)
				sb.Append(string.Concat(openTags));
			sb.Append(block);
			if (openNames.Count > 0)
				sb.Append(string.Concat(openNames.AsEnumerable().Reverse().Select(t => $"</{t}>")));
			return sb.ToString();
		}

		public void Continue() => TryContinue();

		public bool TryContinue()
		{
			if (typewriterCoroutine != null)
			{
				isSpeedingUp = true;
				return true;
			}

			bool pag = ActiveSettings.enablePagination;
			bool tw = ActiveSettings.useTypewriterEffect;
			bool scroll1 = ActiveSettings.scrollOneLineAtATime;

			// TYPEWRITER + PAGINATION + ONE-LINE-SCROLL
			if (tw && pag && scroll1)
			{
				while (currentLineIndex < lineSpans.Count)
				{
					int from = currentLineIndex - maxVisibleLines + 1;
					if (from < 0) from = 0;
					int prevFrom = from + 1;

					string prefix = prevFrom < currentLineIndex
						? GetLinesWithTagContext(from, maxVisibleLines - 1)
						: "";

					string newLine = GetLinesWithTagContext(currentLineIndex, 1);

					string textNoTags = Regex.Replace(newLine, "<.*?>", "").Trim();
					if (string.IsNullOrEmpty(textNoTags))
					{
						textComponent.text = prefix + newLine;
						currentLineIndex++;
						continue;
					}

					currentLineIndex++;
					typewriterCoroutine = StartCoroutine(RevealRichText(prefix, newLine, null));
					return true;
				}
				return false;
			}

			// TYPEWRITER + PAGINATION + PAGE-SCROLL
			if (tw && pag && !scroll1)
			{
				while (currentLineIndex < lineSpans.Count)
				{
					int from = currentLineIndex;
					int pageCount = Mathf.Min(maxVisibleLines, lineSpans.Count - from);

					// Check if *all* lines in this page are empty; if so, show instantly and advance
					bool allEmpty = true;
					for (int i = 0; i < pageCount; i++)
					{
						string candidate = GetLinesWithTagContext(from + i, 1);
						if (!string.IsNullOrWhiteSpace(Regex.Replace(candidate, "<.*?>", "")))
						{
							allEmpty = false;
							break;
						}
					}

					string newPage = GetLinesWithTagContext(from, pageCount);

					if (allEmpty)
					{
						textComponent.text = newPage;
						currentLineIndex += pageCount;
						continue;
					}

					typewriterCoroutine = StartCoroutine(RevealRichText("", newPage, null));
					currentLineIndex += pageCount;
					return true;
				}
				return false;
			}

			// TYPEWRITER ONLY (no pagination)
			if (tw && !pag)
			{
				while (currentLineIndex < lineSpans.Count)
				{
					string prefix = GetLinesWithTagContext(0, currentLineIndex);
					string newContent = GetLinesWithTagContext(currentLineIndex, 1);

					string textNoTags = Regex.Replace(newContent, "<.*?>", "").Trim();
					if (string.IsNullOrEmpty(textNoTags))
					{
						textComponent.text = prefix + newContent;
						currentLineIndex++;
						continue;
					}

					typewriterCoroutine = StartCoroutine(RevealRichText(prefix, newContent, null));
					currentLineIndex++;
					return true;
				}
				return false;
			}

			// PAGINATION ONLY (no typewriter), scroll one line
			if (!tw && pag && scroll1)
			{
				while (currentLineIndex < lineSpans.Count)
				{
					int from = currentLineIndex - maxVisibleLines + 1;
					if (from < 0) from = 0;
					string nextLineBlock = GetLinesWithTagContext(from, Mathf.Min(maxVisibleLines, lineSpans.Count - from));

					// Only check last line in window for being empty
					string lastLine = GetLinesWithTagContext(currentLineIndex, 1);
					string textNoTags = Regex.Replace(lastLine, "<.*?>", "").Trim();

					textComponent.text = nextLineBlock;
					currentLineIndex++;

					// If lastLine is empty, keep going without returning true
					if (string.IsNullOrEmpty(textNoTags))
						continue;
					return true;
				}
				return false;
			}
			// PAGINATION ONLY (no typewriter), page scroll
			if (!tw && pag && !scroll1)
			{
				while (currentLineIndex < lineSpans.Count)
				{
					int from = currentLineIndex;
					int pageCount = Mathf.Min(maxVisibleLines, lineSpans.Count - from);

					// Check if all lines in page are empty
					bool allEmpty = true;
					for (int i = 0; i < pageCount; i++)
					{
						string candidate = GetLinesWithTagContext(from + i, 1);
						if (!string.IsNullOrWhiteSpace(Regex.Replace(candidate, "<.*?>", "")))
						{
							allEmpty = false;
							break;
						}
					}

					string nextPage = GetLinesWithTagContext(from, pageCount);
					textComponent.text = nextPage;
					currentLineIndex += pageCount;

					if (allEmpty)
						continue;
					return true;
				}
				return false;
			}
			return false;
		}

		/// <summary>
		/// Typewriter coroutine that supports TMP rich text tags.
		/// Tags appear instantly; content within tags is typed out with delay.
		/// </summary>
		private IEnumerator RevealRichText(string prefix, string newContent, System.Action onFinish = null)
		{
			int i = 0;
			List<string> openTags = new List<string>();
			string current = prefix ?? "";

			textComponent.text = prefix ?? "";

			var tagRegex = new Regex(@"<.*?>", RegexOptions.Compiled);
			foreach (Match m in tagRegex.Matches(prefix ?? ""))
			{
				string tag = m.Value;
				if (tag.StartsWith("</"))
				{
					if (openTags.Count > 0)
						openTags.RemoveAt(openTags.Count - 1);
				}
				else if (!tag.EndsWith("/>"))
				{
					var match = Regex.Match(tag, @"<(\w+)");
					if (match.Success) openTags.Add(match.Groups[1].Value);
				}
			}

			string currentTyped = prefix ?? "";

			while (i < newContent.Length)
			{
				if (newContent[i] == '<')
				{
					int closeIdx = newContent.IndexOf('>', i);
					if (closeIdx >= 0)
					{
						string tag = newContent.Substring(i, closeIdx - i + 1);
						currentTyped += tag;

						if (!tag.StartsWith("</") && !tag.EndsWith("/>"))
						{
							var m = Regex.Match(tag, @"<(\w+)");
							if (m.Success) openTags.Add(m.Groups[1].Value);
						}
						else if (tag.StartsWith("</"))
						{
							var m = Regex.Match(tag, @"</(\w+)");
							if (m.Success && openTags.Count > 0 && openTags.Last() == m.Groups[1].Value)
								openTags.RemoveAt(openTags.Count - 1);
						}

						i = closeIdx + 1;
						textComponent.text = currentTyped + string.Concat(openTags.AsEnumerable().Reverse().Select(t => $"</{t}>"));
						yield return null;
						continue;
					}
				}

				string display = currentTyped + newContent[i];
				display += string.Concat(openTags.AsEnumerable().Reverse().Select(t => $"</{t}>"));
				textComponent.text = display;

				PlayCharacterSound(newContent[i]);
				float delay = GetDelayForCharacter(newContent[i]);
				if (isSpeedingUp) delay /= 10f;
				yield return new WaitForSeconds(delay);

				currentTyped += newContent[i];
				i++;
			}

			textComponent.text = (prefix ?? "") + newContent;
			typewriterCoroutine = null;
			isTypingInitialBlock = false;
			isSpeedingUp = false;
			onFinish?.Invoke();
		}

		private void PlayCharacterSound(char c)
		{
			if (!char.IsWhiteSpace(c) && ActiveSettings.characterSounds != null && ActiveSettings.characterSounds.Length > 0)
			{
				var clip = ActiveSettings.characterSounds[Random.Range(0, ActiveSettings.characterSounds.Length)];
				if (clip != null)
				{
					audioSource.pitch = Random.Range(1 - ActiveSettings.pitchVariation, 1 + ActiveSettings.pitchVariation);
					audioSource.PlayOneShot(clip);
				}
			}
		}

		private float GetDelayForCharacter(char c)
		{
			float delay = ActiveSettings.typewriterSpeed;

			if (c == ',')
				delay *= 5f;
			else if (c == '.' || c == '!' || c == '?')
				delay *= 10f;

			return delay;
		}

		private string ApplyReplacementsAndPlaceholders(string input)
		{
			if (string.IsNullOrEmpty(input))
				return input ?? "";
			foreach (var rep in ActiveSettings.textReplacements)
			{
				if (!string.IsNullOrEmpty(rep.from))
					input = input.Replace(rep.from, rep.to ?? "");
			}
			return PlaceholderRegex.Replace(input, match =>
			{
				string key = match.Groups[1].Value;
				return ActiveSettings.userData.Get(key) ?? match.Value;
			});
		}

		public bool IsTyping => typewriterCoroutine != null || isTypingInitialBlock;
		public bool HasMoreText => currentLineIndex < lineSpans.Count;
		public bool IsComplete => !IsTyping && !HasMoreText;
	}
}

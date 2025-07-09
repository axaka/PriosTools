using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PriosTools
{
	[System.Serializable]
	public class TextLocalizerSettings
	{
		//[Header("Data Configuration")]
		public PriosUserData userData;
		public PriosDataStore dataStore;
		public string sheet;

		//[Header("Typewriter Effect")]
		public bool useTypewriterEffect = false;
		public float typewriterSpeed = 0.05f;
		public float speedUpMultiplier = 10f;

		//[Header("Pagination")]
		public bool enablePagination = true;
		public bool autoDetectBounds = true;
		public float maxHeight = 500f;
		public bool scrollOneLineAtATime = true;

		//[Header("Audio")]
		public AudioClip[] characterSounds;
		public float pitchVariation = 0.05f;

		//[Header("Text Replacement")]
		public Replace[] textReplacements = new Replace[0];

		//[Header("Advanced")]
		public string keyField = "Key";
		public string languageDisplayKey = "Language";

		[System.Serializable]
		public class Replace
		{
			public string from;
			public string to;
		}
	}

	[CreateAssetMenu(fileName = "TextLocalizerSettings", menuName = "Prios Tools/Text Localizer Settings")]
	public class TextLocalizerData : ScriptableObject
	{
		public TextLocalizerSettings Settings;
	}
}

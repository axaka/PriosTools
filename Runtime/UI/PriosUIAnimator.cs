using System.Collections;
using UnityEngine;

namespace PriosTools
{
	[RequireComponent(typeof(CanvasGroup))]
	[RequireComponent(typeof(RectTransform))]
	public class PriosUIAnimator : MonoBehaviour
	{
		[Header("Settings")]
		[SerializeField] private bool _startShowing = false;
		[SerializeField] private bool _startAnimating = false;

		[Header("Animation Settings")]
		[SerializeField] private PriosUIAnimatorData.AnimationType _animationType = PriosUIAnimatorData.AnimationType.Fade;
		[SerializeField] private PriosUIAnimatorData.SlideDirection _slideDirection = PriosUIAnimatorData.SlideDirection.Down;
		[SerializeField] private bool _preventIfRunning = false;
		[SerializeField] private bool _preventIfAlreadyCorrect = true;

		[Header("Animation Data")]
		[SerializeField] private PriosUIAnimatorData _animationData = null;

		Coroutine _animationCoroutine = null;

		private CanvasGroup _canvasGroup = null;
		private CanvasGroup CanvasGroup => _canvasGroup ??= GetComponent<CanvasGroup>();

		//private RectTransform _rectTransform = null;
		private Vector2? _slideDestination = null;

		public bool Showing => CanvasGroup.interactable;
		public bool Running => _animationCoroutine != null;

		private void Start()
		{
			SetSlideDestination();
			bool preventIfRunning = _preventIfRunning;
			bool preventIfAlreadyCorrect = _preventIfAlreadyCorrect;

			_preventIfRunning = false;
			_preventIfAlreadyCorrect = false;

			Run(_startShowing, _startAnimating ? _animationData.AnimationDuration : 0f);

			_preventIfRunning = preventIfRunning;
			_preventIfAlreadyCorrect = preventIfAlreadyCorrect;
		}

		public void SetSlideDestination()
		{
			SetSlideDestination(transform.localPosition);
		}
		public void SetSlideDestination(Vector2? position)
		{
			if (!this) return;

			_slideDestination = position;
		}

		public float Run(bool show)
		{
			return Run(show, _animationData.AnimationDuration);
		}
		public float Run(bool show, float duration)
		{
			return Run(show, duration, _animationType, _slideDirection);
		}

		public float Run(bool show,
		float duration,
		PriosUIAnimatorData.AnimationType animationType,
		PriosUIAnimatorData.SlideDirection slideDirection)
		{
			if (!this) return 0.0f;

			if (_preventIfRunning && Running)
			{
				return 0.0f;
			}

			if (_preventIfAlreadyCorrect && Showing == show)
			{
				return 0.0f;
			}

			CanvasGroup.interactable = show;
			CanvasGroup.blocksRaycasts = show;

			switch (animationType)
			{
				case PriosUIAnimatorData.AnimationType.Fade:
					_animationCoroutine = StartCoroutine(AnimateFade(show, duration));
					break;
				case PriosUIAnimatorData.AnimationType.Slide:
					_animationCoroutine = StartCoroutine(AnimateSlide(show, duration, slideDirection));
					break;
			}

			return duration;
		}

		IEnumerator AnimateSlide(bool show,
			float duration,
			PriosUIAnimatorData.SlideDirection slideDirection)
		{
			if (_slideDestination.HasValue == false) SetSlideDestination();

			Vector2 startPos = !show ? _slideDestination.Value : _slideDestination.Value + GetScreenOffsetVector(slideDirection);
			Vector2 endPos = show ? _slideDestination.Value : _slideDestination.Value + GetScreenOffsetVector(slideDirection);

			if (duration <= 0)
			{
				transform.localPosition = endPos;
				_animationCoroutine = null; // Reset coroutine reference
				yield break;
			}

			float timeElapsed = 0f;
			while (timeElapsed < duration) // Use duration directly
			{
				timeElapsed += Time.deltaTime;
				float t = Mathf.Clamp01(timeElapsed / duration); // Normalize time between 0 and 1
				transform.localPosition = Vector2.LerpUnclamped(startPos, endPos, _animationData.AnimationCurve.Evaluate(t));
				yield return null;
			}
			_animationCoroutine = null; // Reset coroutine reference
		}
		IEnumerator AnimateFade(bool show, float duration)
		{
			if (_slideDestination.HasValue == false) SetSlideDestination();

			if (duration <= 0)
			{
				CanvasGroup.alpha = show ? 1 : 0;
				_animationCoroutine = null; // Reset coroutine reference
				yield break;
			}

			float timeElapsed = 0f;
			while (timeElapsed < duration)
			{
				timeElapsed += Time.deltaTime;
				float t = Mathf.Clamp01(timeElapsed / duration);
				CanvasGroup.alpha = Mathf.Lerp(show ? 0 : 1,
					show ? 1 : 0, t);
				yield return null;
			}
			_animationCoroutine = null; // Reset coroutine reference
		}

		Vector2 GetScreenOffsetVector(PriosUIAnimatorData.SlideDirection slideDirection)
		{
			return PriosUIAnimatorData.GetDirectionVector(slideDirection) * new Vector2(Screen.width, Screen.height);
		}
	}
}
using UnityEngine;

namespace PriosTools
{
	[CreateAssetMenu(fileName = "PriosUIAnimatorData", menuName = "Data/PriosUIAnimatorData", order = 1)]
	public class PriosUIAnimatorData : ScriptableObject
	{
		[SerializeField] private float _animationDuration = 0.75f;
		[SerializeField] private AnimationCurve _animationCurve = AnimationCurve.Linear(0, 0, 1, 1);

		public float AnimationDuration => _animationDuration;
		public AnimationCurve AnimationCurve => _animationCurve;

		public enum AnimationType
		{
			Fade,
			Slide
		}
		public enum SlideDirection
		{
			Left,
			Right,
			Up,
			Down
		}
		public static Vector2 GetDirectionVector(SlideDirection direction)
		{
			return direction switch
			{
				SlideDirection.Left => Vector2.left,     // (-1, 0)
				SlideDirection.Right => Vector2.right,  // (1, 0)
				SlideDirection.Up => Vector2.up,        // (0, 1)
				SlideDirection.Down => Vector2.down,    // (0, -1)
				_ => Vector2.zero                       // Fallback for invalid direction
			};
		}
	}
}
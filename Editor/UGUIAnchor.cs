using UnityEditor;
using UnityEngine;

public class UGUIAnchor : MonoBehaviour
{
	[MenuItem("UI/Anchor Around Object %#q")]
	static void uGUIAnchorAroundObject()
	{
		var activeObject = Selection.activeGameObject;
		var rectTransform = activeObject.transform.parent.GetComponent<RectTransform>();

		if (activeObject != null && rectTransform != null)
		{
			Undo.RegisterCompleteObjectUndo(activeObject, "Set anchor points automatically " + activeObject.gameObject.name);
			var r = activeObject.GetComponent<RectTransform>();

			var offsetMin = r.offsetMin;
			var offsetMax = r.offsetMax;
			var _anchorMin = r.anchorMin;
			var _anchorMax = r.anchorMax;

			var parent_width = rectTransform.rect.width;
			var parent_height = rectTransform.rect.height;

			var anchorMin = new Vector2(_anchorMin.x + (offsetMin.x / parent_width),
										_anchorMin.y + (offsetMin.y / parent_height));
			var anchorMax = new Vector2(_anchorMax.x + (offsetMax.x / parent_width),
										_anchorMax.y + (offsetMax.y / parent_height));

			r.anchorMin = anchorMin;
			r.anchorMax = anchorMax;

			r.offsetMin = new Vector2(0, 0);
			r.offsetMax = new Vector2(0, 0);
			r.pivot = new Vector2(0.5f, 0.5f);

			EditorUtility.SetDirty(activeObject);
		}
	}
}
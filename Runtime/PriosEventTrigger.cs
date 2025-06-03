using UnityEngine;
using UnityEngine.UI;

namespace PriosTools
{
	public class PriosEventTrigger : MonoBehaviour
	{
		public string EventKey;
		public PriosEvent.EventType SelectedType;
		public bool boolData;
		public int intData;
		public float floatData;
		public string stringData;
		public Object objectData;

		private void Awake()
		{
			GetComponent<Button>().onClick.AddListener(RunEvent);
		}

		public void RunEvent()
		{
			object eventData = SelectedType switch
			{
				PriosEvent.EventType.Object => objectData,
				PriosEvent.EventType.Bool => boolData,
				PriosEvent.EventType.Int => intData,
				PriosEvent.EventType.Float => floatData,
				PriosEvent.EventType.String => stringData,
				_ => null
			};
			PriosEvent.TriggerEvent(EventKey, eventData);
		}
	}
}
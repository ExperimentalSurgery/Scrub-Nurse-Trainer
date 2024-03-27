using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace NMY.TouchInteraction3D {

	/// <summary>
	/// A component to deactivate the TouchManager when an uGUI component is touched. This can be used
	/// to avoid unwanted interaction with TouchManager enabled colliders when an uGUI component
	/// is touched. 
	/// Simply place this component on any GameObject using a uGUI component to deactivate TouchManager
	/// functionality. The TouchManager is deactivated on pointer down and reactivated on pointer up.
	/// </summary>
	public class TouchManagerUIDeactivator : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
	{
		static public BBIPhoneTouchManager TouchManager {
			get {
				if (touchManager==null)
					touchManager = FindObjectOfType<BBIPhoneTouchManager>();
				return touchManager;
			}
		}
		static BBIPhoneTouchManager touchManager;

		public void OnPointerDown(PointerEventData eventData)
		{
			if (TouchManager)
				TouchManager.enabled = false;
		}

		public void OnPointerUp(PointerEventData eventData)
		{
			if (TouchManager)
				TouchManager.enabled = true;
		}
	}

} // namespace

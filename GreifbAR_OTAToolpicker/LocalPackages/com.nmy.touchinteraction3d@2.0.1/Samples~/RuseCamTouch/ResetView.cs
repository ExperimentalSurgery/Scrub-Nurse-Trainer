using UnityEngine;
using System.Collections; 
using NMY;

namespace NMY.Example {

	public class ResetView : StartupBehaviour {
 
		private Vector3 localPosition, localScale; 
		private Quaternion localRotation;
				
		override protected void StartupEnter()
		{
			// store transformation
			localPosition=Camera.main.transform.localPosition;
			localRotation=Camera.main.transform.localRotation;
			localScale=Camera.main.transform.localScale;
		}

		public void ResetCameraTransform() 
		{
			Camera cam = Camera.main;
			// reset transformation
			cam.transform.localPosition = localPosition;
			cam.transform.localRotation = localRotation;
			cam.transform.localScale = localScale;
		}
	}

} // namespace

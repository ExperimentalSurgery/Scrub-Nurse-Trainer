using UnityEngine;
using System.Collections;

namespace NMY {

/// <summary>
/// The most basic ActivatableStartupBehaviour possible: (De)Activate is mapped to (De)ActivateImmediately,
/// objects will be set to active=false, and nothing else happens.
/// </summary>
/// <remarks>
/// This is essentially the same as go.SetActive(bool), but provides the IActivatable interface for 
/// convenience (and events feedback).
/// </remarks>
public class SimpleOnOffActivatable : ActivatableStartupBehaviour {
		
	override protected void StartupEnter(){
	}

	#region IActivatable
	override protected void ActivateEnter() {
		ActivateImmediatelyEnter();
	}
	
	override protected void DeactivateEnter() {
		DeactivateImmediatelyEnter();
	}

	override protected void ActivateImmediatelyEnter() {
		gameObject.SetActive(true);
	}

	override protected void DeactivateImmediatelyEnter() {
		gameObject.SetActive(false);
	}
	#endregion	
}

}
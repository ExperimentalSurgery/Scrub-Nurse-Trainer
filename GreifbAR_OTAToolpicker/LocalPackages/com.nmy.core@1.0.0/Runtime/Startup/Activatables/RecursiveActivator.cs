using UnityEngine;
using System.Collections;

namespace NMY {

/// <summary>
/// Recursively control (de)activation of all ASBs in this subtree.
/// </summary>
/// <remarks>
/// NOTE: This is a convenience class which can be derived to use it for specific types of ASBs.
/// 
/// It does *NOT* automatically startup its children.
/// Starting up *all* children automatically is usually not what one wants, and could be done by simply
/// having them auto-started up anyway.
/// </remarks>
public class RecursiveActivator : ActivatableStartupBehaviour {

	public bool startupChildren=false; // false: backwards compatibilty
	protected ActivatableStartupBehaviour[] activatables;

	override protected void StartupEnter(){
		activatables=GetComponentsInChildren<ActivatableStartupBehaviour>();

		// NOTE: cannot unconditionally startup any children here, since that might not be what we want
		//       for derived classes (e.g., only starting up certain objects)
		if(startupChildren)
		   foreach(ActivatableStartupBehaviour asb in activatables)
				if(asb!=this)
					asb.Startup();
	}

	#region IActivatable
	override protected void ActivateEnter() {
		foreach(ActivatableStartupBehaviour asb in activatables)
			if(asb.enabled && asb!=this)
				asb.Activate();
	}

	override protected void DeactivateEnter() {
		foreach(ActivatableStartupBehaviour asb in activatables)
			if(asb.enabled && asb!=this)
				asb.Deactivate();
	}

	override protected void ActivateImmediatelyEnter() {
		foreach(ActivatableStartupBehaviour asb in activatables)
			if(asb.enabled && asb!=this)
				asb.ActivateImmediately();
	}

	override protected void DeactivateImmediatelyEnter() {
		foreach(ActivatableStartupBehaviour asb in activatables)
			if(asb.enabled && asb!=this)
				asb.DeactivateImmediately();
	}
	#endregion
}

}
using UnityEngine;
using System.Collections;
using NMY;

namespace NMY {

/// <summary>
/// Small test script to manually trigger Activate/Deactivate/DeactivateImmediately in the Inspector.
/// </summary>
/// <remarks>
/// when we initialize, force state to the one defined in "activator".
/// otherwise correctly update the inspector in case the ASB's state changed externally
/// </remarks>
public class ActivatableStartupBehaviour_TriggerManually : MonoBehaviour {
	
	public ActivatableStartupBehaviour activatableStartupBehaviour; ///< if null, find in current GO
	public bool activator=false;
	public bool deactivateNow=false;
	public bool activateNow=false;
	protected bool previousState;
	
	virtual protected void Awake(){
		if(!activatableStartupBehaviour)
			activatableStartupBehaviour=GetComponent<ActivatableStartupBehaviour>();
		if(!activatableStartupBehaviour){
			Debug.LogError("Need to assign an ASB, or attach this script to a GO that has one.");
			enabled=false;
		}else if(activatableStartupBehaviour.startupInAwake)
			activatableStartupBehaviour.Startup();
	}
	
	virtual protected void Start(){
		if(activatableStartupBehaviour && !activatableStartupBehaviour.startupInAwake)
			activatableStartupBehaviour.Startup();
		
		// ensure initialization to our state "activator" when we're first activated
		previousState=activatableStartupBehaviour.isActivated;
	}
	
	virtual protected void Update(){
		// update state if changed externally
		if(previousState!=activatableStartupBehaviour.isActivated){
			activator=activatableStartupBehaviour.isActivated;
			previousState=activator;
		}
		
		if(deactivateNow){
			activatableStartupBehaviour.DeactivateImmediately();
			deactivateNow=false;
			activator=false;
			previousState=false;
		}
		if(activateNow){
			activatableStartupBehaviour.ActivateImmediately();
			activateNow=false;
			activator=true;
			previousState=true;
		}
		if(activator!=previousState){
			if(activator)
				activatableStartupBehaviour.Activate();
			else
				activatableStartupBehaviour.Deactivate();
			previousState=activator;
		}
	}
}

} // namespace
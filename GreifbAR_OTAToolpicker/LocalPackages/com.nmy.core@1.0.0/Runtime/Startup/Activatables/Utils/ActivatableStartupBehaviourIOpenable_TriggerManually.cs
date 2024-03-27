using UnityEngine;
using System.Collections;

namespace NMY {

/// <summary>
/// Small test script to manually trigger Activate/Deactivate/DeactivateImmediately in the Inspector.
/// </summary>
public class ActivatableStartupBehaviourIOpenable_TriggerManually : ActivatableStartupBehaviour_TriggerManually {
	
	public bool opener=false;
	public bool closeNow=false;
	public bool openNow=false;
	private bool previousOpenState;
	
	override protected void Awake(){
		base.Awake ();
		
		if((activatableStartupBehaviour as IOpenable)==null){
			Debug.LogError("Need to assign an ASB which implements IOpenable, or attach this script to a GO that has one.");
			enabled=false;
		}
	}
	
	override protected void Start(){
		base.Start();
		
		// ensure initialization to our state "opener" when we're first activated
		previousOpenState=(activatableStartupBehaviour as IOpenable).openableState==OpenableState.Opened;
	}
	
	override protected void Update(){
		base.Update();
		
		// update state if changed externally
		if(previousOpenState!=((activatableStartupBehaviour as IOpenable).openableState==OpenableState.Opened)){
			opener=(activatableStartupBehaviour as IOpenable).openableState==OpenableState.Opened;
			previousOpenState=opener;
		}
		
		if(closeNow){
			(activatableStartupBehaviour as IOpenable).CloseImmediately();
			closeNow=false;
			opener=(activatableStartupBehaviour as IOpenable).openableState==OpenableState.Opened; // can differ if !canOpen
			previousOpenState=opener;
		}
		if(openNow){
			(activatableStartupBehaviour as IOpenable).OpenImmediately();
			openNow=false;
			opener=(activatableStartupBehaviour as IOpenable).openableState==OpenableState.Opened; // can differ if !canOpen
			previousOpenState=opener;
		}
		if(opener!=previousOpenState){
			if(opener)
				(activatableStartupBehaviour as IOpenable).Open();
			else
				(activatableStartupBehaviour as IOpenable).Close();
			opener=(activatableStartupBehaviour as IOpenable).openableState==OpenableState.Opened; // can differ if !canOpen
			previousOpenState=opener;
		}
	}
}

} // namespace
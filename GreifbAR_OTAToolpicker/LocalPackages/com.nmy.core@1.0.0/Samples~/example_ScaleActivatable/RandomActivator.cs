using UnityEngine;
using System.Collections;
using NMY;

/// <summary>
/// Randomly toggle activation of any ActivatableStartupBehaviour in this subtree.
/// </summary>
public class RandomActivator : StartupBehaviour {

	public float frequency=2;
	protected ActivatableStartupBehaviour[] activatables;
	protected float triggerTime=0; // internal counter keeping track of when to trigger the next event
	protected ActivatableStartupBehaviour previous;

	override protected void StartupEnter(){
		activatables=GetComponentsInChildren<ActivatableStartupBehaviour>(true);
	}

	void Update(){
		if(!hasStartedUp)
			return;
		
		triggerTime+=Time.deltaTime;
		if(triggerTime>1f/frequency){
			ActivatableStartupBehaviour asb;
			do{
				asb=activatables[Random.Range(0,activatables.Length)];
			}while(asb==previous&&activatables.Length>1); // not the same object twice in a row
			previous=asb;
			if(asb.isActivated)
				asb.Deactivate();
			else
				asb.Activate();
			triggerTime-=1f/frequency;
		}
	}
}

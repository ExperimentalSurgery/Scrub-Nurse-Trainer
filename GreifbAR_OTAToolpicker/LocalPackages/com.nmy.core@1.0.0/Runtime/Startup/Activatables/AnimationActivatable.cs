using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using NMY;
using NMY.DelegateCoroutines;

namespace NMY {

/// <summary>
/// An ActivatableStartupBehaviour that triggers a Unity animation instead of alpha for a single group of objects.
/// </summary>
/// <remarks>
/// NOTE: cleanly sends all {De}Activate{d}-Events
/// </remarks>
public class AnimationActivatable : ActivatableStartupBehaviour {

	[Header("Animation parameters")]
	public Animation sourceAnimation;
	public float speed=1;
	//public bool invert=false; // NYI
	protected string animName;
	protected float totalLength; // moved to base class, might be useful. NOTE: for now, only initialized on Startup
	public float length {
		get { 
			if(!_hasStartedUp)
				Debug.LogError ("Initialization fail accessing length!");
			return totalLength;
		}
	}
	
	public bool deactivateObjects=false; // defaults to false for possible back compatibility conflicts
	
	override protected void StartupEnter(){
		if(sourceAnimation==null)
			sourceAnimation=GetComponent<Animation>();
		if(sourceAnimation){
			animName=sourceAnimation.clip.name;
			totalLength=sourceAnimation[animName].clip.length/speed;
		}
	}

	/// <summary>
	/// If any parameters changed, call this to update the totalLength.
	/// </summary>
	virtual public void UpdateTotalLength(){
		if(!_hasStartedUp)
			return; // ignore if not initialized
		if(sourceAnimation)
			totalLength=sourceAnimation[animName].clip.length/speed;
		else
			totalLength=0;
	}

	public void OverrideTotalLength(float newLength, bool ignoreIfShorter=true){
		if(!_hasStartedUp)
			Startup();
		if(!ignoreIfShorter || newLength>totalLength)
			totalLength=newLength;
	}

	#region IActivatable
	override protected void ActivateEnter() {
		float curPos=sourceAnimation[animName].time; // in case we're currently deactivating this is !=0
		StopDeactivate();
		if(deactivateObjects){
			gameObject.SetActive(true);
			//StaticUtils.ActivateRecursivelyHACK(gameObject,true); // take care of kids // unresolved Reinhausen HACK
			StaticUtils.ActivateRecursively(gameObject,true); // take care of kids
		}

		// sourceAnimation[animName].time will be correct; no need to modify
		sourceAnimation[animName].speed=speed;
		sourceAnimation.Play(animName);

		this.WaitAndExecute(totalLength-curPos/speed, () => {
			OnActivateFinished();
		});
	}

	override protected void ActivateExit() {}
	
	virtual protected void OnActivateFinished(object o=null, System.EventArgs args=null){
		if(_isActivated)
			SendActivatedEvent();
	}
	
	override protected void DeactivateEnter() {
		StopActivate();
		if(deactivateObjects)
			StaticUtils.ActivateRecursively(gameObject,false, deactivateASBonly:true); // take care of kids; dont deac. non-ASBs; we'll do that with timing
		
		if(!sourceAnimation.isPlaying)
			sourceAnimation[animName].time = sourceAnimation[animName].clip.length; // do not reset unconditionally - continue from where we were
		float curPos=sourceAnimation[animName].time; // in case we're currently activating this is !=length
		sourceAnimation[animName].speed=-speed;
		sourceAnimation.Play(animName);
		
		this.WaitAndExecute(curPos/speed, () => {
			OnDeactivateFinished();
		});
	}

	// override this - we're sending the DeactivatedEvent ourselves
	override protected void DeactivateExit() {}

	virtual protected void OnDeactivateFinished(object o=null, System.EventArgs args=null) {
		// bailout?
		if(isActivated)
			return;
		
		if(deactivateObjects){
			StaticUtils.ActivateRecursively(gameObject,false); // now take care of the rest of the bunch
			gameObject.SetActive(false);
		}
		SendDeactivatedEvent();
	}
	
	override protected void ActivateImmediatelyEnter() {
		StopDeactivateImmediately();

		if(deactivateObjects){
			gameObject.SetActive(true);
			//StaticUtils.ActivateRecursivelyHACK(gameObject,true,true); // take care of kids // unresolved Reinhausen HACK
			StaticUtils.ActivateRecursively(gameObject,true,true); // take care of kids
		}
		sourceAnimation[animName].clip.SampleAnimation(sourceAnimation.gameObject,sourceAnimation[animName].clip.length);
	}

	override protected void DeactivateImmediatelyEnter() {
		StopActivateImmediately();
		
		sourceAnimation[animName].clip.SampleAnimation(sourceAnimation.gameObject,0);
		if(deactivateObjects){
			StaticUtils.ActivateRecursively(gameObject,false,true); // take care of kids
			gameObject.SetActive(false);
		}
	}

	override protected void StopActivate() {
//		sourceAnimation.Stop(); // no need to stop, continue from where we are
		DelegateCoroutine.StopAll(this);
	}

	override protected void StopDeactivate() {
//		sourceAnimation.Stop(); // no need to stop, continue from where we are
		DelegateCoroutine.StopAll(this);
	}

	// different behaviour when coming from DI
	virtual protected void StopActivateImmediately() {
		sourceAnimation.Stop(); // need to stop playing
		DelegateCoroutine.StopAll(this);
	}
	
	// different behaviour when coming from AI
	virtual protected void StopDeactivateImmediately() {
		sourceAnimation.Stop(); // need to stop playing
		DelegateCoroutine.StopAll(this);
	}
	#endregion	
}

} // namespace

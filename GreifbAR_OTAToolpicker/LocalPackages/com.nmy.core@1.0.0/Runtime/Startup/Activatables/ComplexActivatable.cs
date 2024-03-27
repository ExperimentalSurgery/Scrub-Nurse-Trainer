using UnityEngine;
using System.Collections;
using NMY.DelegateCoroutines;

#if NMY_HASDEPENDENCY
// dependency to StandardShaderFader

namespace NMY {
/// <summary>
/// An ActivatableStartupBehaviour that optionally triggers
/// - a Unity animation
/// - a fade
/// - an axis-selective translation
/// </summary>
/// <remarks>
/// NOTE: cleanly sends all {De}Activate{d}-Events
/// TODO: finding correct starting points and timing when (re-)starting
/// in-between is rather complicated, so we live with that for now
/// ==> interrupted behaviour is incorrect for now! but the end states are always correct.
/// </remarks>
public class ComplexActivatable : AnimationActivatable {

	public enum Axis {
		MINUS_X,
		MINUS_Y,
		MINUS_Z,
		PLUS_X,
		PLUS_Y,
		PLUS_Z,
		REF_TARGET, // point defined in axisVector
		CUSTOM_AXIS // axis/direction defined in axisVector
	}

	// NOTE: indices match enum Axis; REF_TARGET and CUSTOM_AXIS are never accessed
	protected Vector3[] axisValues=new Vector3[]{
		-Vector3.right,
		-Vector3.up,
		-Vector3.forward,
		Vector3.right,
		Vector3.up,
		Vector3.forward 
	};

	[Header("Active sections")]
	public bool useAnimation=false;
	public bool useFade=true;
	public bool useAxisAnimation=false;

	[Header("Fade parameters")]
	public float fadeDuration=1;
	public float fadeDelay=0;
	public float alphaFrom=0;
	public float alphaTo=1;
	public iTween.EaseType fadeEaseType=iTween.EaseType.linear;
	public bool fadeChildren=false; // if useFade, also fade children in ComplexActivatables (default is off, as they usually do this themselves)
	protected StandardShaderFader shaderFader;

	[Header("Axis animation parameters")]
	public float axisDuration=1;
	public float axisDelay=0;
	[Tooltip("along which axis (points toward the starting point of the animation!)")]
	public Axis axis;
	[Tooltip("start of animation at this distance from initialPosition")]
	public float axisAmount=1;
	[Tooltip("REF_TARGET: this is the reference point; axis is REF_TARGET->initialPosition. CUSTOM_AXIS: this is the custom axis")]
	public Vector3 axisVector; // in play mode this will represent the actual axis vector
	public iTween.EaseType axisEaseType=iTween.EaseType.linear;
	protected Vector3 initialPosition;
	protected ComplexActivatable[] childrenDependencies;


	override protected void StartupEnter(){
//		initialPosition=transform.position;
		initialPosition=transform.localPosition;

		if(useAnimation)
			base.StartupEnter();
		if(!sourceAnimation){
			useAnimation=false;
			totalLength=0;
		}

		if(useFade){
			shaderFader=GetComponent<StandardShaderFader>();
			if(!shaderFader){
//MaterialRenderQueueModifier blub=gameObject.AddComponent<MaterialRenderQueueModifier>();
//blub.InitToDifferentQueueOffset(Random.Range(0,10)-5);
//blub.Startup();
				shaderFader=gameObject.AddComponent<StandardShaderFader>();
				shaderFader.zWriteWhileFading=true;
				shaderFader.ignoreASBchildren=!fadeChildren;
			}
			shaderFader.Startup();
		}

		if(useAxisAnimation){
			// initialize dynamic values
			if(axis==Axis.REF_TARGET)
				axisVector=(initialPosition-axisVector).normalized;
			else if(axis==Axis.CUSTOM_AXIS)
				axisVector=axisVector.normalized;
			else
				axisVector=axisValues[(int)axis];
		}

		// we need to initialize all children shaderFaders before we get to StartupExit!
		childrenDependencies=GetComponentsInChildren<ComplexActivatable>(); // we don't want inactive ones
		foreach(ComplexActivatable ca in childrenDependencies)
			ca.Startup();

		// TODO: is it wise to start ALL ASBs instead?
//		ActivatableStartupBehaviour[] childrenDependencies=GetComponentsInChildren<ActivatableStartupBehaviour>(); // we don't want inactive ones
//		foreach(ActivatableStartupBehaviour asb in childrenDependencies)
//			asb.Startup();

		if(useFade)
			totalLength=Mathf.Max(totalLength,(fadeDuration+fadeDelay)/speed);
		if(useAxisAnimation)
			totalLength=Mathf.Max(totalLength,(axisDuration+axisDelay)/speed);
	}

	/// <summary>
	/// If any parameters changed, call this to update the totalLength.
	/// </summary>
	override public void UpdateTotalLength(){
		if(!_hasStartedUp)
			return; // ignore if not initialized

		base.UpdateTotalLength();
		if(useFade)
			totalLength=Mathf.Max(totalLength,(fadeDuration+fadeDelay)/speed);
		if(useAxisAnimation)
			totalLength=Mathf.Max(totalLength,(axisDuration+axisDelay)/speed);
	}

	virtual public void FadeTo(float alpha, bool immediately=false, bool withDelay=false){
		if(!isActivated && alpha!=0)
			return;
		//Debug.Log ("FI "+name+" "+alpha+" "+immediately);
		//print(Time.time+" "+name+" "+alpha+" "+immediately+" "+(shaderFader!=null));
		if(shaderFader){
			iTween.Stop(gameObject);
			if(immediately)
				shaderFader.SetAlpha(alpha);
			else
				StaticUtils.DoValueITween(gameObject,fadeDuration/speed,withDelay?fadeDelay/speed:0,shaderFader.alpha,alpha,
				    (System.Action<object>) (newVal => {
						shaderFader.SetAlpha((float)newVal);
					}),
					fadeEaseType
				);
		}
	}
	
	/// <summary>
	/// fade back to original alphaTo, in case somebody used FadeTo(alpha)
	/// </summary>
	virtual public void FadeToOrig(bool immediately=false, bool withDelay=false){
		if(!isActivated)
			return;
		//Debug.Log ("F1 "+name+" "+immediately);
		if(shaderFader && shaderFader.alpha!=alphaTo){
			iTween.Stop(gameObject);
			if(immediately)
				shaderFader.SetAlpha(alphaTo);
			else
				StaticUtils.DoValueITween(gameObject,fadeDuration/speed,withDelay?fadeDelay/speed:0,shaderFader.alpha,alphaTo,
				    (System.Action<object>) (newVal => {
						shaderFader.SetAlpha((float)newVal);
					}),
					fadeEaseType
				);
		}
	}
	
	#region IActivatable
	override protected void ActivateEnter() {
		//Debug.Log ("CA "+name);
		StopDeactivate();
		if(deactivateObjects){
			gameObject.SetActive(true);
			//StaticUtils.ActivateRecursivelyHACK(gameObject,true); // take care of kids // unresolved Reinhausen HACK
			StaticUtils.ActivateRecursively(gameObject,true); // take care of kids
		}
//		ActivatableStartupBehaviour[] asbs=gameObject.GetComponents<ActivatableStartupBehaviour>();
//		foreach(ActivatableStartupBehaviour asb in asbs)
//			if(asb!=this)
//				asb.Activate();
//		for(int i=0;i<transform.childCount;i++)
//			StaticUtils.ActivateRecursively(transform.GetChild(i),true); // take care of kids - exclude root! that's us. it would stop ActivateRecursively immediately
		
		if(useAnimation){
			// sourceAnimation[animName].time will be correct; no need to modify
			sourceAnimation[animName].speed=speed;
			sourceAnimation.Play(animName);
		}

		if(useFade)
			FadeToOrig(withDelay:true);

		if(useAxisAnimation)
			StaticUtils.DoValueITween(gameObject,axisDuration/speed,axisDelay/speed,0,1, // "0" should be current pos (=when we were interrupted)
				(System.Action<object>) (newVal => {
					transform.localPosition=initialPosition+axisVector*((1-(float)newVal)*axisAmount);
				}),
				axisEaseType
			);

		// TODO: totalLength is too long when interrupting a Deactivate.
		// need a case subdivition as in Deactivate(). problem: do we always know where we are?
		// anim: if anim is longest: see base class
		// fade: grab shaderFader.alpha -> not if delay, not if fade!=totalLength
		// axisAnimation: ???
		this.WaitAndExecute(totalLength, () => {
			OnActivateFinished();
		});
	}

	override protected void DeactivateEnter() {
		//Debug.Log ("CD "+name);
		StopActivate();
		StaticUtils.ActivateRecursively(gameObject,false, deactivateASBonly:true); // take care of kids; dont deac. non-ASBs; we'll do that with timing
//		ActivatableStartupBehaviour[] asbs=gameObject.GetComponents<ActivatableStartupBehaviour>();
//		foreach(ActivatableStartupBehaviour asb in asbs)
//			if(asb!=this)
//				asb.Deactivate();
//		for(int i=0;i<transform.childCount;i++)
//			StaticUtils.ActivateRecursively(transform.GetChild(i),false, deactivateASBonly:true); // take care of kids; dont deac. non-ASBs; we'll do that with timing
		
		if(useAnimation)
			this.WaitAndExecute(totalLength-sourceAnimation[animName].clip.length/speed, () => {
				//if(_isActivated)
				//	return;
				if(!sourceAnimation.isPlaying)
					sourceAnimation[animName].time = sourceAnimation[animName].clip.length; // do not reset unconditionally - continue from where we were
				sourceAnimation[animName].speed=-speed;
				sourceAnimation.Play(animName);
			});

		if(useFade)
			StaticUtils.DoValueITween(gameObject,fadeDuration/speed,totalLength-(fadeDuration+fadeDelay)/speed,shaderFader.alpha,alphaFrom,
				(System.Action<object>) (newVal => {
					shaderFader.SetAlpha((float)newVal);
				}),
				StaticUtils.GetInverseITweenEaseType(fadeEaseType)
			);

		if(useAxisAnimation)
			StaticUtils.DoValueITween(gameObject,axisDuration/speed,totalLength-(axisDuration+axisDelay)/speed,1,0, // "1" should be current pos (=when we were interrupted)
			    (System.Action<object>) (newVal => {
					transform.localPosition=initialPosition+axisVector*((1-(float)newVal)*axisAmount);
				}),
				StaticUtils.GetInverseITweenEaseType(axisEaseType)
			 );
		
		// TODO: totalLength is too long when interrupting an Activate. => objects will be deactivated too late!
		// problem: do we always know where we are?
		// anim: if anim is longest: see base class
		// fade: grab shaderFader.alpha -> not if delay, not if fade!=totalLength
		// axisAnimation: ???
		this.WaitAndExecute(totalLength, () => {
			OnDeactivateFinished();
		});
	}

	override protected void ActivateImmediatelyEnter() {
		//Debug.Log ("CAI "+name);
		if(useAnimation)
			base.ActivateImmediatelyEnter();
		else{
			StopDeactivateImmediately();
			if(deactivateObjects){
				gameObject.SetActive(true);
				//StaticUtils.ActivateRecursivelyHACK(gameObject,true,true); // take care of kids // unresolved Reinhausen HACK
				StaticUtils.ActivateRecursively(gameObject,true,true); // take care of kids
			}
//			ActivatableStartupBehaviour[] asbs=gameObject.GetComponents<ActivatableStartupBehaviour>();
//			foreach(ActivatableStartupBehaviour asb in asbs)
//				if(asb!=this)
//					asb.ActivateImmediately();
//			for(int i=0;i<transform.childCount;i++)
//				StaticUtils.ActivateRecursively(transform.GetChild(i),true,true); // take care of kids - exclude root! that's us. it would stop ActivateRecursively immediately
		}
		if(shaderFader)
			FadeToOrig(true);
		if(useAxisAnimation)
			transform.localPosition=initialPosition;
	}

	override protected void DeactivateImmediatelyEnter() {
		//Debug.Log ("CDI "+name);
		if(shaderFader)
			FadeTo(alphaFrom,true);
		if(useAxisAnimation)
			transform.localPosition=initialPosition+axisVector*axisAmount;
		if(useAnimation)
			base.DeactivateImmediatelyEnter();
		else{
			StopActivateImmediately();
			if(deactivateObjects){
				StaticUtils.ActivateRecursively(gameObject,false,true); // take care of kids
				gameObject.SetActive(false);
			}
//			ActivatableStartupBehaviour[] asbs=gameObject.GetComponents<ActivatableStartupBehaviour>();
//			foreach(ActivatableStartupBehaviour asb in asbs)
//				if(asb!=this)
//					asb.DeactivateImmediately();
//			// don't deactivateObjects  means we still need to deactivate ASBs!
//			for(int i=0;i<transform.childCount;i++)
//				StaticUtils.ActivateRecursively(transform.GetChild(i),false,true, !deactivateObjects);
		}
	}

	override protected void StopActivate() {
		if(useAnimation)
			base.StopActivate(); // already includes a DelegateCoroutine.StopAll
		else
			DelegateCoroutine.StopAll(this);

		iTween.Stop(gameObject);
	}

	override protected void StopDeactivate() {
		if(useAnimation)
			base.StopDeactivate(); // already includes a DelegateCoroutine.StopAll
		else
			DelegateCoroutine.StopAll(this);

		iTween.Stop(gameObject);
	}
	
	// different behaviour when coming from DI
	override protected void StopActivateImmediately() {
		if(useAnimation)
			base.StopActivateImmediately(); // already includes a DelegateCoroutine.StopAll
		else
			DelegateCoroutine.StopAll(this);

		iTween.Stop(gameObject);
	}
	
	// different behaviour when coming from AI
	override protected void StopDeactivateImmediately() {
		if(useAnimation)
			base.StopDeactivateImmediately(); // already includes a DelegateCoroutine.StopAll
		else
			DelegateCoroutine.StopAll(this);

		iTween.Stop(gameObject);
	}
	#endregion	
}

}

#endif
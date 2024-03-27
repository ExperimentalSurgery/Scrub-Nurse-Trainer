using UnityEngine;
using System.Collections;

namespace NMY {

/// <summary>
/// An ActivatableStartupBehaviour that triggers a Unity animation instead of alpha for a single group of objects.
/// </summary>
/// <remarks>
/// NOTE: cleanly sends all {De}Activate{d}-Events
/// </remarks>
public class FadeableAnimationActivatable : AnimationActivatable {

	public float fadeDuration=1;
	public float fadeDelay=1;
	public float alphaFrom=0;
	public float alphaTo=1;
	
	#region IActivatable
	override protected void ActivateEnter() {
		StopDeactivate();

//		sourceAnimation.Rewind(animName); // do not reset - continue from where we were
		sourceAnimation[animName].speed=speed;
		sourceAnimation.Play(animName);

		Hashtable args = new Hashtable();
		args.Add("time", fadeDuration/speed);
		args.Add("delay", fadeDelay/speed);
		args.Add("from", alphaFrom);
		args.Add("to", alphaTo);
		args.Add("onupdate", (System.Action<object>) (newVal => {
			StaticUtils.SetAlphaRecursively(gameObject,(float)newVal);
		}));
		iTween.ValueTo(gameObject, args);

		this.WaitAndExecute(Mathf.Max(sourceAnimation[animName].clip.length,fadeDuration+fadeDelay)/speed, () => {
			OnActivateFinished();
		});
	}

	override protected void DeactivateEnter() {
//		StopActivate(); // no need to stop, continue from where we are

		if(sourceAnimation[animName].clip.length>fadeDuration+fadeDelay){
			// delay tween...
			if(!sourceAnimation.isPlaying)
				sourceAnimation[animName].time = sourceAnimation[animName].clip.length; // do not reset unconditionally - continue from where we were
			sourceAnimation[animName].speed=-speed;
			sourceAnimation.Play(animName);
			
			Hashtable args = new Hashtable();
			args.Add("time", fadeDuration/speed);
			args.Add("delay", (sourceAnimation[animName].clip.length-fadeDuration-fadeDelay)/speed);
			args.Add("from", alphaTo);
			args.Add("to", alphaFrom);
			args.Add("onupdate", (System.Action<object>) (newVal => {
				StaticUtils.SetAlphaRecursively(gameObject,(float)newVal);
			}));
			iTween.ValueTo(gameObject, args);
		}else{
			// delay animation...
			Hashtable args = new Hashtable();
			args.Add("time", fadeDuration/speed);
			args.Add("from", alphaTo);
			args.Add("to", alphaFrom);
			args.Add("onupdate", (System.Action<object>) (newVal => {
				StaticUtils.SetAlphaRecursively(gameObject,(float)newVal);
			}));
			iTween.ValueTo(gameObject, args);

			this.WaitAndExecute((fadeDuration+fadeDelay-sourceAnimation[animName].clip.length)/speed, () => {
				if(!isDeactivating)
					return;
				if(!sourceAnimation.isPlaying)
					sourceAnimation[animName].time = sourceAnimation[animName].clip.length; // do not reset unconditionally - continue from where we were
				sourceAnimation[animName].speed=-speed;
				sourceAnimation.Play(animName);
			});
		}
		
		this.WaitAndExecute(Mathf.Max(sourceAnimation[animName].clip.length,fadeDuration+fadeDelay)/speed, () => {
			OnDeactivateFinished();
		});
	}

	override protected void ActivateImmediatelyEnter() {
		base.ActivateImmediatelyEnter();
		StaticUtils.SetAlphaRecursively(gameObject,alphaTo);
	}

	override protected void DeactivateImmediatelyEnter() {
		base.DeactivateImmediatelyEnter();
		StaticUtils.SetAlphaRecursively(gameObject,alphaFrom);
	}

	override protected void StopActivate() {
		base.StopActivate();
		iTween.Stop(gameObject);
		// TODO: need to stop the WaitAndExecute's!
	}

	override protected void StopDeactivate() {
		base.StopDeactivate();
		iTween.Stop(gameObject);
		// TODO: need to stop the WaitAndExecute's!
	}
	#endregion	
}

}
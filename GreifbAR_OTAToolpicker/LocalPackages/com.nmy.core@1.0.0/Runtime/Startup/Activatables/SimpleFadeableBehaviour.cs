using UnityEngine;
using System.Collections;

namespace NMY {

/// <summary>
/// An ActivatableStartupBehaviour with simple fadeIn/fadeOut functionality.
/// </summary>
/// <remarks>
/// If this GO has a CanvasGroup, it modifies its alpha. Otherwise:
/// As always, this relies on all objects envolved to already have a fadeable (=Transparent/...) shader.
/// NOTE: cleanly sends all {De}Activate{d}-Events
/// NOTE: currently, only the Renderer on THIS GO is queried, but fading occurs recursively (with that origAlpha)
/// </remarks>
public class SimpleFadeableBehaviour : ActivatableStartupBehaviour {

	public float fadeDuration=1; // 0: immediately
	public bool deactivateObjects=true;
	protected float origAlpha; // NOTE: currently, only the Renderer on THIS GO is queried, but fading occurs recursively (with that origAlpha)
	protected CanvasGroup canvasGroup;

	override protected void StartupEnter(){
		canvasGroup=GetComponent<CanvasGroup>();
		if(canvasGroup)
			origAlpha=canvasGroup.alpha;
		else if(GetComponent<Renderer>())
			origAlpha=StaticUtils.GetMaterialAlpha(GetComponent<Renderer>().sharedMaterial);
		else
			origAlpha=1;
	}
	
	// unused?
	//void FadeTo(float alpha){
	//	if(!isActivated)
	//		return;
		
	//	iTween.Stop(gameObject);
	//	Hashtable args = new Hashtable();
	//	args.Add("time", fadeDuration);
	//	args.Add("alpha", alpha);
	//	iTween.FadeTo(gameObject, args);
	//}
	
	#region IActivatable
	override protected void ActivateEnter() {
		if(fadeDuration==0){
			ActivateImmediately();
			return;
		}

		StopDeactivate();

		if(deactivateObjects)
			gameObject.SetActive(true);
		if(canvasGroup){
			StaticUtils.DoValueITween(gameObject,fadeDuration,0,canvasGroup.alpha,origAlpha, (val => {
					canvasGroup.alpha=(float)val;
				}),
				iTween.EaseType.easeOutCubic,
				OnActivateFinished
			);
		}else{
			Hashtable args = new Hashtable();
			args.Add("time", fadeDuration);
			args.Add("alpha", origAlpha);
			args.Add("oncomplete", "OnActivateFinished");
			iTween.FadeTo(gameObject, args);
		}
	}
	
	override protected void ActivateExit() {}
	
	// SendMessage (via iTween) apparently can't handle optional parameters (tested 2017.4-2019.2), so use explicit wrapper...
	protected void OnActivateFinished(){
		OnActivateFinished(null,null);
	}

	virtual protected void OnActivateFinished(object sender, object args){
		SendActivatedEvent();
	}
	
	override protected void DeactivateEnter() {
		if(fadeDuration==0){
			DeactivateImmediately();
			return;
		}

		StopActivate();

		if(canvasGroup){
			StaticUtils.DoValueITween(gameObject,fadeDuration,0,canvasGroup.alpha,0f, (val => {
					canvasGroup.alpha=(float)val;
				}),
				iTween.EaseType.easeOutCubic,
				OnDeactivateFinished
			);
		}else{
			Hashtable args = new Hashtable();
			args.Add("time", fadeDuration);
			args.Add("alpha", 0f);
			args.Add("oncomplete", "OnDeactivateFinished");
			iTween.FadeTo(gameObject, args);
		}
	}
	
	override protected void DeactivateExit() {}
	
	// SendMessage (via iTween) apparently can't handle optional parameters (tested 2017.4-2019.2), so use explicit wrapper...
	protected void OnDeactivateFinished(){
		OnDeactivateFinished(null,null);
	}

	virtual protected void OnDeactivateFinished(object sender, object args){
		SendDeactivatedEvent();
		if(deactivateObjects)
			gameObject.SetActive(false);
	}
	
	override protected void ActivateImmediatelyEnter() {
		StopDeactivate();

		if(deactivateObjects)
			gameObject.SetActive(true);
		if(canvasGroup)
			canvasGroup.alpha=origAlpha;
		else
			NMY.StaticUtils.SetAlphaRecursively(gameObject,origAlpha);
	}
	
	override protected void DeactivateImmediatelyEnter() {
		StopActivate();

		if(canvasGroup)
			canvasGroup.alpha=0f;
		else
			NMY.StaticUtils.SetAlphaRecursively(gameObject,0f);
		if(deactivateObjects)
			gameObject.SetActive(false);
	}

	override protected void StopActivate() {
		iTween.Stop(gameObject);
	}

	override protected void StopDeactivate() {
		iTween.Stop(gameObject);
	}
	#endregion	
}

}
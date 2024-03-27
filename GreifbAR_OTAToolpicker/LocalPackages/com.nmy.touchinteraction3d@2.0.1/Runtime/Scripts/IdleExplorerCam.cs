using UnityEngine;
using System.Collections;

namespace NMY.TouchInteraction3D {

/// <summary>
/// Idle mode for ExplorerCamTouch.
/// </summary>
/// <remarks>
/// Smoothly starts an idle animation after a certain grace period.
/// Animation consists of:
/// - continuous Y-axis rotation
/// - adjust X-axis rotation until a given camera height is achieved
/// </remarks>
public class IdleExplorerCam : NMY.StartupBehaviour {
	
	public bool idleAnimOnStartup=true;
	public float idleGracePeriod=5;
	public float idleSpeed=15;
	public float targetHeight=1.55f;
	
	private BBInputDelegate touchManager;
	private ExplorerCamTouch ect;
	private Transform cam;
	private float lastTouchTime;
	private float currentIdleSpeedY=0;
	private float currentSmoothToSpeedX=0;
	private float smoothTime=2;
	
	protected override void StartupEnter(){
		ect=GetComponent<ExplorerCamTouch>();
		if(!ect){
			Debug.LogError("IdleExplorerCam needs to be attached to an ExplorerCamTouch object! Disabling script.");
			enabled=false;
		}
		
		if(enabled){
			touchManager=BBInputDelegate.instance;
			ect.Startup();
			cam=ect.renderingCamera.transform;

			if(idleAnimOnStartup)
				lastTouchTime=-idleGracePeriod; // force immediate idle animation 
		}
	}
	
	void Update(){
		if(touchManager.activeEvents.Count>0){
			// stop idling
			lastTouchTime=Time.time;
			currentIdleSpeedY=0;
			currentSmoothToSpeedX=0;
		}
		
		if(Time.time-lastTouchTime>=idleGracePeriod){
			// smooth into animation after exceeding grace period
			float factorTime=(Time.time-lastTouchTime-idleGracePeriod)/smoothTime;
			currentIdleSpeedY=Mathf.Clamp(idleSpeed*factorTime,0,idleSpeed);
			
			// smooth height towards target height
			float diffHeight=cam.localPosition.y-targetHeight;
			currentSmoothToSpeedX=diffHeight*factorTime;
			if(diffHeight>0)
				currentSmoothToSpeedX=Mathf.Clamp(currentSmoothToSpeedX,0,diffHeight);
			else
				currentSmoothToSpeedX=Mathf.Clamp(currentSmoothToSpeedX,diffHeight,0);
			
			ect.rotVelY=-currentIdleSpeedY;
			ect.rotVelX=-currentSmoothToSpeedX*5; // arbitrary speedup factor WC->angle
		}
	}
}

} // namespace

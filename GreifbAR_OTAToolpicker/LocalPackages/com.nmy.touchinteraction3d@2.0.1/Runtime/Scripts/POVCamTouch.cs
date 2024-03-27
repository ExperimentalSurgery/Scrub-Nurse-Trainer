using UnityEngine;
using System.Collections;

namespace NMY.TouchInteraction3D {

public class POVCamTouch : BBTouchableObject {

	public float rotationSpeed = 10.0f;
	public float zoomSpeed = 10.0f;
	
	public float rotationDamping = 0.92f;
	public float zoomDamping = 0.9f;
	
	public bool reverseRotation=false; // set to true if we want to simulate a PanCamTouch

	// Minimum view angle of camera, and rotational limits
	// no limit if min==max
	// NOTE: at the moment, vertical limits cannot be <-90 or >90, due to internal Unity angle computations
	// NOTE: Unity uses left-handed rotation, so minVertical is the maximum up pitch, and maxVertical is the minimum down pitch
	public float minAngle = 1.0f;	
	public float maxAngle = 90f;
	public float minHorizontal = 0f;
	public float maxHorizontal = 0f;
	public float minVertical = 2.0f;
	public float maxVertical = 75.0f;
	
	// if true, force limits on camera at startup, although we didn't touch anything yet
	public bool initializeCameraToLimits=true;

	// if true, we include the view frustum in the limit checks (the limits adapt to the current fieldOfView)
	// (e.g., to prevent seeing black areas outside the images in the display panel)
	// if false, we use normal limits, simply checking the cam.forward direction
	// (e.g., for the EOIR cams)
	public bool viewFrustumLimits=true;
	//private float initialFovX, initialFovY;

	private bool limitX, limitY;

	/* The maximum velocity of the camera. */
	public float maxVelocity = 5000.0f;
	public float minAngularVelocity = 0.01f;
	public float maxAngularVelocity = 10.0f;
	
	private float zoomFactor;
	private float rotVelX,rotVelY;

	private Vector3 lastScreenPosition0;
	private Vector3 lastScreenPosition1;
	private long eventID0, eventID1;
	
	private Transform camTrans;  // the camera transform (local variable for speed reasons)
	
	override protected void StartupEnter() {
		base.StartupEnter();
		camTrans = renderingCamera.transform;

		Init();
	}

	public void Init(){
		if(!hasStartedUp){
			Startup(); // will call Init() itself...
			return;  // ...so bailout
		}

		// We cannot simply use Mathf.Clamp(), because of the usual wraparound problems for angles, so we use the
		// rather complex but highly configurable version we used in Touchlab 1.
		//
		// during initialization, we normalize angles so we can clamp to arbitrary limits
		// - max to [0..360)
		// - max-min to [0..360)
		while(maxHorizontal>=360) maxHorizontal-=360;
		while(maxHorizontal<0) maxHorizontal+=360;
		while(maxHorizontal-minHorizontal>=360) minHorizontal+=360;
		while(minHorizontal>maxHorizontal) minHorizontal-=360;

		// NOTE: for the vertical axis, we need to limit the possible range to -90..90,
		// otherwise we can't use the localEulerAngles routines and would have to compute 
		// the precise angle on our own
		while(maxVertical>=360) maxVertical-=360;
		while(maxVertical<0) maxVertical+=360;
		if(maxVertical>90&&maxVertical<270){
			print("WARNING! Cannot set vertical limit outside -90..90! Clamping.");
			if(maxVertical>180)
				maxVertical=270;
			else
				maxVertical=90;
		}
		while(maxVertical-minVertical>=360) minVertical+=360;
		while(minVertical>maxVertical) minVertical-=360;
		if(maxVertical<=90&&minVertical<-90 || maxVertical>=270&&minVertical<270){
			print("WARNING! Cannot set vertical limit outside -90..90! Clamping.");
			if(minVertical<-180)
				minVertical=maxVertical;
			else if(minVertical<-90)
				minVertical=-90;
			else if(minVertical>180)
				minVertical=270;
			else
				minVertical=maxVertical;
		}

		// do we have limits set?
		limitX=minVertical!=maxVertical;
		limitY=minHorizontal!=maxHorizontal;

		if(reverseRotation){
			float oldMax=maxVertical;
			maxVertical=-minVertical;
			minVertical=-oldMax;
			oldMax=maxHorizontal;
			maxHorizontal=-minHorizontal;
			minHorizontal=-oldMax;
			// TODO: this shouldn't be necessary, that's what the limit check takes care of
			while(maxHorizontal<0){
				maxHorizontal+=360;
				minHorizontal+=360;
			}
		}

		/*
		 * variant: this will keep our initial center always in view, independent of cam.fieldOfView
		 */
		/*
		if(!viewFrustumLimits){
			// compute initial fov's
			initialFovY=renderingCamera.fieldOfView/2;
			initialFovX=Mathf.Atan(Mathf.Tan(initialFovY*Mathf.Deg2Rad)*renderingCamera.aspect)*Mathf.Rad2Deg;
		}
		*/

		if(initializeCameraToLimits){
			if(limitY){
				if(viewFrustumLimits){
					// auto-adjust maxAngle to accommodate the given rotational limits
					float limitAngle=maxHorizontal-minHorizontal;
					if(limitAngle<180){ // no need to adjust if >=180
						// compute vertical fieldOfView equivalent
						float fovX=limitAngle/2;
						float fovY=Mathf.Atan(Mathf.Tan(fovX*Mathf.Deg2Rad)/renderingCamera.aspect)*Mathf.Rad2Deg;
						if(maxAngle>fovY*2)
							maxAngle=fovY*2;
					}
				}

				/*
		 		 * old code: restrict movement, but do not clamp
				 */
				/*
				float currentY=camTrans.localEulerAngles.y;
				while(maxHorizontal-currentY>=360) currentY+=360;
				while(currentY>maxHorizontal) currentY-=360;
				if(minHorizontal>currentY||maxHorizontal<currentY){
					// current angle is not in allowed interval; initialize to nearest valid boundary
					// (we might want to interpolate to that value, though)
					float maxLimit=Mathf.Min(Mathf.Abs(maxHorizontal-currentY),360-Mathf.Abs(maxHorizontal-currentY));
					float minLimit=Mathf.Min(Mathf.Abs(minHorizontal-currentY),360-Mathf.Abs(minHorizontal-currentY));
					Vector3 rot=camTrans.localEulerAngles;
					// NOTE: don't use Transform.Rotate(), it will skew the z-angle, even when only rotating around a single axis, and even with Space.World!
					if(minLimit>maxLimit)
						rot.y=maxHorizontal;
					else
						rot.y=minHorizontal;
					camTrans.localEulerAngles=rot;
				}
				*/
			}

			if(limitX){
				if(viewFrustumLimits){
					// auto-adjust maxAngle to accommodate the given rotational limits
					float limitAngle=maxVertical-minVertical;
					if(limitAngle<180){ // no need to adjust if >=180
						float fovY=limitAngle/2;
						if(maxAngle>fovY*2)
							maxAngle=fovY*2;
					}
				}

				/*
		 		 * old code: restrict movement, but do not clamp
				 */
				/*
				float currentX=camTrans.localEulerAngles.x;
				while(maxVertical-currentX>=360) currentX+=360;
				while(currentX>maxVertical) currentX-=360;
				if(minVertical>currentX||maxVertical<currentX){
					// current angle is not in allowed interval; initialize to nearest valid boundary
					// (we might want to interpolate to that value, though)
					float maxLimit=Mathf.Min(Mathf.Abs(maxVertical-currentX),360-Mathf.Abs(maxVertical-currentX));
					float minLimit=Mathf.Min(Mathf.Abs(minVertical-currentX),360-Mathf.Abs(minVertical-currentX));
					Vector3 rot=camTrans.localEulerAngles;
					// NOTE: don't use Transform.Rotate(), it will skew the z-angle, even when only rotating around a single axis, and even with Space.World!
					if(minLimit>maxLimit)
						rot.x=maxVertical;
					else
						rot.x=minVertical;
					camTrans.localEulerAngles=rot;
				}
				*/
			}
		}

		// reset
		ResetAll();
	}
	
	void OnEnabled(){
		// reset
		ResetAll();
	}

	void FixedUpdate() {
		float currentX=camTrans.localEulerAngles.x;
		float currentY=camTrans.localEulerAngles.y;
		if(reverseRotation){
			// TODO: the 360-wrapping shouldn't matter!
			currentX=360-currentX;
			currentY=360-currentY;
		}

		// apply zoom before the limit checks
		renderingCamera.fieldOfView -= zoomFactor * Time.smoothDeltaTime;

		// hard limit (=clamp) out-of-range zooms
		if(renderingCamera.fieldOfView<minAngle){
			renderingCamera.fieldOfView=minAngle;
			zoomFactor=0;
		}
		if(renderingCamera.fieldOfView>maxAngle){
			renderingCamera.fieldOfView=maxAngle;
			zoomFactor=0;
		}

		float fovX=0;
		float fovY=0;
		if(viewFrustumLimits){
			// compute fov half angles
			fovY=renderingCamera.fieldOfView/2;
			fovX=Mathf.Atan(Mathf.Tan(fovY*Mathf.Deg2Rad)*renderingCamera.aspect)*Mathf.Rad2Deg;
		}

		/*
		 * variant: this will keep our initial center always in view, independent of cam.fieldOfView
		 */
		/*
		// compute fov half angles
		float fovY=renderingCamera.fieldOfView/2;
		float fovX=Mathf.Atan(Mathf.Tan(fovY*Mathf.Deg2Rad)*renderingCamera.aspect)*Mathf.Rad2Deg;
		if(!viewFrustumLimits){
			// use the fov's as a dynamic limit variation
			fovY=initialFovY-fovY;
			fovX=initialFovX-fovX;
		}
		*/

		// progress X rotation if above threshold
		float new_fraction=currentX;

		// TODO: for reverseRotation and !viewFrustumLimits, ATM this clamps to the wrong limit if outside
		/*
		 * new code: clamp if outside! (which could also happen on zoom out)
		 */
		new_fraction+=rotVelX*Time.smoothDeltaTime;
		if(limitX){
			while(new_fraction>maxVertical-fovY)
				new_fraction-=360f;
			if(new_fraction<minVertical+fovY)
				if(rotVelX<=0)
					new_fraction=minVertical+fovY; // rotate up limit
				else
					new_fraction=maxVertical-fovY; // rotate down limit
		}

		/*
		 * old code: restrict movement, but do not clamp
		 */
		/*
		if(Mathf.Abs(rotVelX*Time.smoothDeltaTime)>=minAngularVelocity){
			new_fraction+=rotVelX*Time.smoothDeltaTime;
			if(limitX){
				while(currentX>maxVertical-fovY)
					currentX-=360f;
				if(currentX>=minVertical+fovY)
				{
					// clamp to limit
					while(new_fraction>maxVertical-fovY)
						new_fraction-=360f;
					if(new_fraction<minVertical+fovY)
						if(rotVelX<0)
							new_fraction=minVertical+fovY; // rotate up limit
						else
							new_fraction=maxVertical-fovY; // rotate down limit
				}else{
					// we are outside the limit (proably due to set_target[XY]Rotation)
					// so only allow movement towards nearest limit
					float distToDown=Mathf.Abs(currentX-maxVertical+fovY);
					distToDown=Mathf.Min(Mathf.Abs(currentX-maxVertical+fovY-360f),distToDown);
					distToDown=Mathf.Min(Mathf.Abs(currentX-maxVertical+fovY+360f),distToDown);
					float distToUp=Mathf.Abs(currentX-minVertical-fovY);
					distToUp=Mathf.Min(Mathf.Abs(currentX-minVertical-fovY-360f),distToUp);
					distToUp=Mathf.Min(Mathf.Abs(currentX-minVertical-fovY+360f),distToUp);
					if(distToUp>distToDown&&rotVelX>0 || distToUp<distToDown&&rotVelX<0){
						new_fraction=currentX;
					}
				}
			}
		}
		*/
		float Xdelta=new_fraction-currentX;
		
		// progress Y rotation if above threshold
		new_fraction=currentY;

		/*
		 * new code: clamp if outside! (which could also happen on zoom out)
		 */
		new_fraction+=rotVelY*Time.smoothDeltaTime;
		if(limitY){
			while(new_fraction>maxHorizontal-fovX)
				new_fraction-=360f;
			if(new_fraction<minHorizontal+fovX)
				if(rotVelY<=0)
					new_fraction=minHorizontal+fovX; // rotate left limit
				else
					new_fraction=maxHorizontal-fovX; // rotate right limit
		}

		/*
		 * old code: restrict movement, but do not clamp
		 */
		/*
		if(Mathf.Abs(rotVelY*Time.smoothDeltaTime)>=minAngularVelocity){
			new_fraction+=rotVelY*Time.smoothDeltaTime;
			if(limitY){
				while(new_fraction>maxHorizontal-fovX)
					new_fraction-=360f;
				if(new_fraction<minHorizontal+fovX)
					if(rotVelY<0)
						new_fraction=minHorizontal+fovX; // rotate left limit
					else
						new_fraction=maxHorizontal-fovX; // rotate right limit
				while(currentY>maxHorizontal-fovX)
					currentY-=360f;
				if(currentY>=minHorizontal+fovX)
				{
					// clamp to limit
					while(new_fraction>maxHorizontal-fovX)
						new_fraction-=360f;
					if(new_fraction<minHorizontal+fovX)
						if(rotVelY<0)
							new_fraction=minHorizontal+fovX; // rotate left limit
						else
							new_fraction=maxHorizontal-fovX; // rotate right limit
				}else{
					// we are outside the limit (proably due to set_target[XY]Rotation)
					// so only allow movement towards nearest limit
					float distToRight=Mathf.Abs(currentY-maxHorizontal+fovX);
					distToRight=Mathf.Min(Mathf.Abs(currentY-maxHorizontal+fovX-360f),distToRight);
					distToRight=Mathf.Min(Mathf.Abs(currentY-maxHorizontal+fovX+360f),distToRight);
					float distToLeft=Mathf.Abs(currentY-minHorizontal-fovX);
					distToLeft=Mathf.Min(Mathf.Abs(currentY-minHorizontal-fovX-360f),distToLeft);
					distToLeft=Mathf.Min(Mathf.Abs(currentY-minHorizontal-fovX+360f),distToLeft);
					if(distToLeft>distToRight&&rotVelY>0 || distToLeft<distToRight&&rotVelY<0){
						new_fraction=currentY;
					}
				}
			}
		}
		*/
		float Ydelta=new_fraction-currentY; 

		// apply rotations
		if(Xdelta!=0||Ydelta!=0)
			if(reverseRotation)
				camTrans.localEulerAngles-=new Vector3(Xdelta,Ydelta,0);
			else
				camTrans.localEulerAngles+=new Vector3(Xdelta,Ydelta,0);

		// Apply the transformation damping
		zoomFactor *= zoomDamping;	
		rotVelX *= rotationDamping;
		rotVelY *= rotationDamping;
	}
	
	public override void handleSingleTouch(BBTouchEvent touch0) {
		// simulate double touch with identical coordinates
		BBTouchEvent[] simulateDoubleTouch=new BBTouchEvent[2];
		simulateDoubleTouch[0]=touch0;
		simulateDoubleTouch[1]=touch0;
		handleDoubleTouch(simulateDoubleTouch);
	}

	public override void noTouches() {
		// reset memory
		ResetAll();
	}

	public override void handleDoubleTouch(BBTouchEvent[] events) {
		// NOTE: for multi touches we CANNOT rely on event.lastScreenPosition, since they are updated
		// individually, and do *not* contain the position when handleDoubleTouch() was last called!
		// So we need to store our own version of that.

		BBTouchEvent touch0 = events[0];
		BBTouchEvent touch1 = events[1];
		
		// first time: initialize
		if(eventID0!=touch0.eventID||eventID1!=touch1.eventID){
			lastScreenPosition0=touch0.screenPosition;
			lastScreenPosition1=touch1.screenPosition;
			eventID0=touch0.eventID;
			eventID1=touch1.eventID;
		}

		// === prepare rotation

		// get center hitpoint of both touches
		//Vector3 centeredHitPoint=(touch0.rayCastHitPosition+touch1.rayCastHitPosition)*0.5f;

		// get rotation deltas
		// NOTE: X and Y reversed since we're talking about the *rotational* axes here
		float deltaX = ((touch0.screenPosition.y+touch1.screenPosition.y)-(lastScreenPosition0.y+lastScreenPosition1.y))*0.5f;
		float deltaY = ((touch0.screenPosition.x+touch1.screenPosition.x)-(lastScreenPosition0.x+lastScreenPosition1.x))*0.5f;

		// === prepare zoom

		// get distance of both touches
		float touchDistNow=Vector3.Distance(touch0.screenPosition,touch1.screenPosition);
		float touchDistThen=Vector3.Distance(lastScreenPosition0,lastScreenPosition1);

		// old firmware failsafes (which might place both touches on top of each other)
		if(touchDistNow==0f)
			touchDistNow=touchDistThen;
		else if(touchDistThen==0f)
			touchDistThen=touchDistNow;

		// approximate "distance" by view angle
		float distance=renderingCamera.fieldOfView;

		// === rotation

		// We rotate around the hitpoint on the ground. If no hitpoint, no rotate...
		// apply translational parts
		rotVelY += deltaY * rotationSpeed * distance/1000f;
		rotVelX -= deltaX * rotationSpeed * distance/1000f;

		// === zoom

		if (distance > minAngle && touchDistNow-touchDistThen > 0 ||
			distance < maxAngle && touchDistNow-touchDistThen < 0) {
				// Modify the scaling velocity depending on the distance
				// Don't include the targetDirection, since it changes with rotation
				// The targetDirection will be applied on-the-fly in FixedUpdate
				zoomFactor += (touchDistNow-touchDistThen) * (distance / 2000.0f) * zoomSpeed;
		}
		
		// Make sure the velocity does not exceed the maxVelocity setting	
		if (zoomFactor > maxVelocity)
			zoomFactor = maxVelocity;
			
		// store our own values
		lastScreenPosition0=touch0.screenPosition;
		lastScreenPosition1=touch1.screenPosition;
	}

	public override void handleTripleTouch(BBTouchEvent[] events) {
		// more than two events: for now, just ignore them and evaluate the first two
		handleDoubleTouch(events);
	}

	void ResetAll(){
		// reset memory
		lastScreenPosition0=Vector3.zero;
		lastScreenPosition1=Vector3.zero;
		eventID0=-1;
		eventID1=-1;
	}
}

} // namespace
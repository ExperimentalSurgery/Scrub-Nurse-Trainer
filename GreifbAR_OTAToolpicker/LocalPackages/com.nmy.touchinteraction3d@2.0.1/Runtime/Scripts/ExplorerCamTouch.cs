using UnityEngine;
using System.Collections;

namespace NMY.TouchInteraction3D {

/// <summary>
/// A navigation camera for the System Explorer,
/// rotating around a center with single touch, and zooming.
/// With smoothFollowWeight>0, a moving target can be auto-followed by position and/or orientation.
/// </summary>

public class ExplorerCamTouch : BBTouchableObject {

	// the center of view pivot point
	public Transform target=null;

	public bool useFixedUpdate=true;

	public enum TTouchOrientation {
		Standard,
		Deg90Left,
		Deg90Right,
		Deg180
	};
	public TTouchOrientation touchOrientation=TTouchOrientation.Standard;
	// our "up/down" tilt axis changes, depending on the cam orientation
	private Vector3 touchRight {
		get {
			switch(touchOrientation){
				case TTouchOrientation.Deg90Left: return -camTrans.up;
				case TTouchOrientation.Deg90Right: return camTrans.up;
				case TTouchOrientation.Deg180: return -camTrans.right;
				default: return camTrans.right;
			}
		}
	}
	
	[Header("Sensitivity")]
	public float rotationSpeed = 10.0f;
	public float scaleSpeed = 10.0f;
	
	public float rotationDamping = 0.92f;
	public float scaleDamping = 0.9f;
	
	[Header("Limits")]
	// Minimum distance between camera and target.
	// no limit if min==max
	// NOTE: at the moment, vertical limits cannot be <-90 or >90, due to internal Unity angle computations
	public float minDistance = 1.0f;	
	public float maxDistance = 0f;	// no limit if 0
	public float maxHeight = 0f;	// maximum allowed absolute Y pos of camera. constrain maxVertical if exceeded. no limit if 0.
	public bool hardLimit = false; // hard-limit for minDistance *ONLY*. NOTE: smoothFollow may still cause values <minDistance!
	public float minHorizontal = 0f;
	public float maxHorizontal = 0f;
	public float minVertical = 2.0f;
	public float maxVertical = 75.0f;
	
	// if true, force limits on camera at startup, although we didn't touch anything yet
	public bool initializeCameraToLimits=false;

	/* The maximum zoomout velocity of the camera. */
	public float maxVelocity = 5000.0f;
	public float minAngularVelocity = 0.01f;
	public float maxAngularVelocity = 10.0f;
	
	/* true: double touch will rotate, too (=much smoother interaction in combination with scaling. can only do Y-rotation, though (X-rotation is done via single touch))
	 * false: only single touch will rotate
	 */
	public bool doubleTouchRotation = true;
	public bool horizontalRotation = true; // false to disable - more precise than using limits
	public bool verticalRotation = true; // false to disable - more precise than using limits

	[Header("Mouse Wheel")]
	// support for mouse wheel
	public bool wheelIsGlobal = false; // does the mouse wheel always react, or only if the cursor is over the associated collider(s)?
	public bool invertWheelZoom = false;
	public float wheelSensitivity=1f;
	
	public float unclampedYAngle=0; // just a (debug-) output of the current angle, not clamped to [0..360]
	
	[Header("Smooth Follow")]
	public float smoothFollowDistance = 20f;
	public float smoothFollowHeight = 10f;
	public float smoothFollowWeight = 0f;
	public float explorerWeight = 1f;
	// smoothLookAtWeight:
	// 0: never look at target
	// large value (>100): immediatedly look at target
	// other: smoothly look at target
	public float smoothLookAtWeight = 1000f;

	[HideInInspector] public float scaleFactor;
	[HideInInspector] public float rotVelX,rotVelY;

	private Vector3 lastScreenPosition0;
	private Vector3 lastScreenPosition1;
	private long eventID0, eventID1;
	
	private Transform camTrans;  // the camera transform (local variable for speed reasons)
	
	override protected void StartupEnter() {
		base.StartupEnter();
		camTrans = renderingCamera.transform;

		// use self as fallback
		if(!target)
			target=transform;

		if(minDistance<=0)
			minDistance=0.1f; // must never be 0! NOTE: will be troublesome for small scale scenes... 

		// We cannot simply use Mathf.Clamp(), because of the usual wraparound problems for angles, so we use the
		// rather complex but highly configurable version we used in Touchlab 1.
		//
		// during initialization, we normalize angles so we can clamp to arbitrary limits
		// - max to >=0
		// - max-min to [0..360)
		while(maxHorizontal<0) maxHorizontal+=360;
		while(maxHorizontal-minHorizontal>=360) minHorizontal+=360;
		while(minHorizontal>maxHorizontal) minHorizontal-=360;

		// NOTE: for the vertical axis, we need to limit the possible range to -90..90,
		// otherwise we can't use the localEulerAngles routines and would have to compute 
		// the precise angle on our own
		while(maxVertical>=360) maxVertical-=360;
		while(maxVertical<0) maxVertical+=360;
		if(maxVertical>90&&maxVertical<270){
			Debug.LogWarning("WARNING! Cannot set vertical limit outside -90..90! Clamping.");
			if(maxVertical>180)
				maxVertical=270;
			else
				maxVertical=90;
		}
		while(maxVertical-minVertical>=360) minVertical+=360;
		while(minVertical>maxVertical) minVertical-=360;
		if(maxVertical<=90&&minVertical<-90 || maxVertical>=270&&minVertical<270){
			Debug.LogWarning("WARNING! Cannot set vertical limit outside -90..90! Clamping.");
			if(minVertical<-180)
				minVertical=maxVertical;
			else if(minVertical<-90)
				minVertical=-90;
			else if(minVertical>180)
				minVertical=270;
			else
				minVertical=maxVertical;
		}

		if(initializeCameraToLimits){
			if(minHorizontal!=maxHorizontal){ // do we have limits set?
				float currentY=camTrans.localEulerAngles.y;
				while(maxHorizontal-currentY>=360) currentY+=360;
				while(currentY>maxHorizontal) currentY-=360;
				if(minHorizontal>currentY||maxHorizontal<currentY){
					// current angle is not in allowed interval; initialize to nearest valid boundary
					// (we might want to interpolate to that value, though)
					float maxLimit=Mathf.Min(Mathf.Abs(maxHorizontal-currentY),360-Mathf.Abs(maxHorizontal-currentY));
					float minLimit=Mathf.Min(Mathf.Abs(minHorizontal-currentY),360-Mathf.Abs(minHorizontal-currentY));
					if(minLimit>maxLimit)
						camTrans.RotateAround(target.position, Vector3.up, maxHorizontal-currentY);
					else
						camTrans.RotateAround(target.position, Vector3.up, minHorizontal-currentY);
				}
			}

			if(minVertical!=maxVertical){ // do we have limits set?
				float currentX=camTrans.localEulerAngles.x;
				while(maxVertical-currentX>=360) currentX+=360;
				while(currentX>maxVertical) currentX-=360;
				if(minVertical>currentX||maxVertical<currentX){
					// current angle is not in allowed interval; initialize to nearest valid boundary
					// (we might want to interpolate to that value, though)
					float maxLimit=Mathf.Min(Mathf.Abs(maxVertical-currentX),360-Mathf.Abs(maxVertical-currentX));
					float minLimit=Mathf.Min(Mathf.Abs(minVertical-currentX),360-Mathf.Abs(minVertical-currentX));
					if(minLimit>maxLimit)
						camTrans.RotateAround(target.position, touchRight, maxVertical-currentX);
					else
						camTrans.RotateAround(target.position, touchRight, minVertical-currentX);
				}
			}
		}

		// reset memory
		lastScreenPosition0=Vector3.zero;
		lastScreenPosition1=Vector3.zero;
		eventID0=-1;
		eventID1=-1;
	}
	
	void FixedUpdate() {
		if(useFixedUpdate)
			DoUpdate();
	}
	
	void DoUpdate() {
		if(scaleFactor==0&&rotVelX==0&&rotVelY==0&&smoothFollowWeight==0)
			return;

		if(!horizontalRotation)
			rotVelY=0;
		if(!verticalRotation)
			rotVelX=0;

		float currentX=camTrans.localEulerAngles.x;
		float currentY=camTrans.localEulerAngles.y;

		// progress X rotation if above threshold
		float new_fraction=currentX;
		if(Mathf.Abs(rotVelX*Time.smoothDeltaTime)>=minAngularVelocity){
			new_fraction+=rotVelX*Time.smoothDeltaTime;
			if(minVertical!=maxVertical){ // this is limitX, but checking it here enables support of on-the-fly limit changes
				while(currentX>maxVertical)
					currentX-=360f;
				if(currentX>=minVertical)
				{
					// clamp to limit
					while(new_fraction>maxVertical)
						new_fraction-=360f;
					if(new_fraction<minVertical)
						if(rotVelX<0)
							new_fraction=minVertical; // rotate up limit
						else
							new_fraction=maxVertical; // rotate down limit
				}else{
					// we are outside the limit (proably due to set_target[XY]Rotation)
					// so only allow movement towards nearest limit
					float distToDown=Mathf.Abs(currentX-maxVertical);
					distToDown=Mathf.Min(Mathf.Abs(currentX-maxVertical-360f),distToDown);
					distToDown=Mathf.Min(Mathf.Abs(currentX-maxVertical+360f),distToDown);
					float distToUp=Mathf.Abs(currentX-minVertical);
					distToUp=Mathf.Min(Mathf.Abs(currentX-minVertical-360f),distToUp);
					distToUp=Mathf.Min(Mathf.Abs(currentX-minVertical+360f),distToUp);
					if(distToUp>distToDown&&rotVelX>0 || distToUp<distToDown&&rotVelX<0){
						new_fraction=currentX;
					}
				}
			}
		}
		float Xdelta=new_fraction-currentX;
		
		// progress Y rotation if above threshold
		new_fraction=currentY;
		if(Mathf.Abs(rotVelY*Time.smoothDeltaTime)>=minAngularVelocity){
			new_fraction+=rotVelY*Time.smoothDeltaTime;
			if(minHorizontal!=maxHorizontal){ // this is limitY, but checking it here enables support of on-the-fly limit changes
				while(currentY>maxHorizontal)
					currentY-=360f;
				if(currentY>=minHorizontal)
				{
					// clamp to limit
					while(new_fraction>maxHorizontal)
						new_fraction-=360f;
					if(new_fraction<minHorizontal)
						if(rotVelY<0)
							new_fraction=minHorizontal; // rotate left limit
						else
							new_fraction=maxHorizontal; // rotate right limit
				}else{
					// we are outside the limit (proably due to set_target[XY]Rotation)
					// so only allow movement towards nearest limit
					float distToRight=Mathf.Abs(currentY-maxHorizontal);
					distToRight=Mathf.Min(Mathf.Abs(currentY-maxHorizontal-360f),distToRight);
					distToRight=Mathf.Min(Mathf.Abs(currentY-maxHorizontal+360f),distToRight);
					float distToLeft=Mathf.Abs(currentY-minHorizontal);
					distToLeft=Mathf.Min(Mathf.Abs(currentY-minHorizontal-360f),distToLeft);
					distToLeft=Mathf.Min(Mathf.Abs(currentY-minHorizontal+360f),distToLeft);
					if(distToLeft>distToRight&&rotVelY>0 || distToLeft<distToRight&&rotVelY<0){
						new_fraction=currentY;
					}
				}
			}
		}
		float Ydelta=new_fraction-currentY; 

		// stop zoomout/vertical tilt if allowed height exceeded
		// NOTE: hard limits not implemented!
		if(maxHeight!=0 && camTrans.position.y>=maxHeight){
			if(scaleFactor<0)
				scaleFactor=0;
			if(Xdelta>0)
				Xdelta=0;
		}

		if(Xdelta!=0){
			camTrans.RotateAround(target.position, touchRight, Xdelta);
			rotVelX *= rotationDamping;
		}else
			rotVelX=0;
		if(Ydelta!=0){
			camTrans.RotateAround(target.position, Vector3.up, Ydelta);
			rotVelY *= rotationDamping;
		}else
			rotVelY=0;
		
		unclampedYAngle+=Ydelta;


		if(smoothFollowWeight>0){
			// apply scale/zoom and rotation
			Vector3 smoothFollowVec = calcSmoothFollowVec()*Time.smoothDeltaTime;
			camTrans.position += smoothFollowVec*smoothFollowWeight;		
			// smooth Schwenk
			float dist=Vector3.Distance(target.position,camTrans.position);
			camTrans.LookAt(Vector3.Lerp(camTrans.position+camTrans.forward*dist,target.position,smoothLookAtWeight*Time.smoothDeltaTime));
		}

		// apply transformations. make sure scale refers to *current* camera orientation
		if(Mathf.Abs(scaleFactor)>0.1f*minDistance){
			Vector3 targetVec = target.position-camTrans.position;
			float movement=scaleFactor*Time.smoothDeltaTime;
			float dist=targetVec.magnitude;
			if(movement>0){ // only for zoomin!
				if(hardLimit){
					if(dist-movement<minDistance)
						movement=dist-minDistance; // only the portion until we reach minDistance
				}
				if(movement>=dist-0.1f)
					movement=dist-0.1f; // emergency stop to prevent movement through the target pos, causing camera flipping and whatnot
				if(dist<0.1f)
					movement=0; // another emergency stop...
			}
			camTrans.position += movement/dist*targetVec*explorerWeight;
			scaleFactor *= scaleDamping;
		}else
			scaleFactor=0;
	}
	
	// specialized treatment for mouse wheel zoom
	// use Update; querying Input in FixedUpdate is non-deterministic (at least for buttons)
	void Update(){
		float wheel=Input.GetAxis("Mouse ScrollWheel");
		if(wheel!=0 && !isPushed) { // DisplaX workaround: ignore wheel events when happening during (preferrably multi-)touch input
			// some tuning tweaks as we only get per-frame values, not the real scroll distance for fast scrolling...
			wheel=Mathf.Pow(Mathf.Abs(10*wheel),3)*(wheel<0?-1:1)/100*wheelSensitivity;
			if(invertWheelZoom)
				wheel=-wheel;

			Ray screenRay=renderingCamera.ScreenPointToRay(Input.mousePosition);
		
			if(wheelIsGlobal || Physics.Raycast(screenRay, Mathf.Infinity, 1 << LayerMask.NameToLayer("touchableObjects"))) {	
				float distance=Vector3.Distance(target.position,camTrans.position);
				if (wheel < 0 && distance > minDistance || // zoom in and >minHeight
					wheel > 0 && (maxDistance == 0 || distance < maxDistance)) { // zoom out and <maxHeight
					// Modify the scaling velocity depending on the distance
					// Don't include the targetDirection, since it changes with rotation
					// The targetDirection will be applied on-the-fly in FixedUpdate
					scaleFactor += -wheel * distance * scaleSpeed;
				} else if (distance < minDistance || 
						   (maxDistance!=0 && distance > maxDistance)) {
					scaleFactor *= 0.01f;
				}
			
				// Make sure the velocity does not exceed the maxVelocity setting	
				if (scaleFactor > maxVelocity) 
					scaleFactor = maxVelocity;
			}
		}

		if(!useFixedUpdate)
			DoUpdate();
	}
	
	/// <summary>
	/// stop all movement.
	/// </summary>
	/// <remarks>
	/// for now, it ignores SmoothFollow!
	/// </remarks>
	public void StopMotion(){
		rotVelX=0;
		rotVelY=0;
		scaleFactor=0;
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
		lastScreenPosition0=Vector3.zero;
		lastScreenPosition1=Vector3.zero;
		eventID0=-1;
		eventID1=-1;
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
		
		// different interpretations of touch axes depending on cam orientation
		switch(touchOrientation){
			case TTouchOrientation.Deg90Left:
				float flip=deltaX;
				deltaX=deltaY;
				deltaY=-flip;
				break;
			case TTouchOrientation.Deg90Right:
				flip=deltaX;
				deltaX=-deltaY;
				deltaY=flip;
				break;
			case TTouchOrientation.Deg180:
				deltaX=-deltaX;
				deltaY=-deltaY;
				break;
			default:break; // nothing to do
		}
		
		// === prepare scale

		// get distance of both touches
		float touchDistNow=Vector3.Distance(touch0.screenPosition,touch1.screenPosition);
		float touchDistThen=Vector3.Distance(lastScreenPosition0,lastScreenPosition1);

		// old firmware failsafes (which might place both touches on top of each other)
		if(touchDistNow==0f)
			touchDistNow=touchDistThen;
		else if(touchDistThen==0f)
			touchDistThen=touchDistNow;

		// get distance to pivot target
		//Vector3 targetDirection=target.position-camTrans.position;
		float distance=Vector3.Distance(target.position,camTrans.position);

		// === prepare rotation

		float angleDelta;

		// old firmware failsafes - distance might be zero
		// (this will also happen if we disallow double touch rotation)
		if(touchDistNow==0f||!doubleTouchRotation){
			angleDelta=0f;
		}else{
			// get rotation angle
			float angleNow = Mathf.Atan2(touch0.screenPosition.y - touch1.screenPosition.y, touch0.screenPosition.x - touch1.screenPosition.x) * Mathf.Rad2Deg;
			float angleThen = Mathf.Atan2(lastScreenPosition0.y - lastScreenPosition1.y, lastScreenPosition0.x - lastScreenPosition1.x) * Mathf.Rad2Deg;
			// wraparound failsafes (wraparound of Mathf.Atan2 is at +-180)
			if(angleNow>90&&angleThen<-90)
				angleNow-=360;
			if(angleNow<-90&&angleThen>90)
				angleNow+=360;
			angleDelta = angleNow - angleThen;
			// Make sure the angleDelta does not exceed the maxAngularVelocity setting	
			angleDelta=Mathf.Clamp(angleDelta,-maxAngularVelocity,maxAngularVelocity);
		}

		// === rotation

		// We rotate around the hitpoint on the ground. If no hitpoint, no rotate...
		// apply translational parts
		rotVelY += deltaY * rotationSpeed / 10.0f;
		rotVelX -= deltaX * rotationSpeed / 10.0f;
		// apply double touch rotation
		rotVelY += angleDelta * rotationSpeed;

		// === scale/zoom

		if (distance > minDistance && touchDistNow-touchDistThen > 0 ||
				(maxDistance==0 || distance < maxDistance) && touchDistNow-touchDistThen < 0) {
			// Modify the scaling velocity depending on the distance
			// Don't include the targetDirection, since it changes with rotation
			// The targetDirection will be applied on-the-fly in FixedUpdate
			scaleFactor += (touchDistNow-touchDistThen) * (distance / 2000.0f) * scaleSpeed;
		}
		
		if (scaleFactor > maxVelocity)
			scaleFactor = maxVelocity;
			
		// store our own values
		lastScreenPosition0=touch0.screenPosition;
		lastScreenPosition1=touch1.screenPosition;
	}

	public override void handleTripleTouch(BBTouchEvent[] events) {
		// more than two events: for now, just ignore them and evaluate the first two
		handleDoubleTouch(events);
	}

	public Vector3 behindTargetPos {
		get { return target.position + (Vector3.up*smoothFollowHeight); }
	}
	
	private Vector3 calcSmoothFollowVec() {
		
		// Calc the smooth follow velocity vector
		Vector3 smoothFollowVelocity = behindTargetPos - camTrans.position;
		float dist = smoothFollowVelocity.magnitude;
		smoothFollowVelocity /= 10f;
		
		if (dist < smoothFollowDistance) {
			if(dist>0.9f*minDistance){
				float f = dist / smoothFollowDistance; 
				f = (float)System.Math.Pow(f, 50);
				smoothFollowVelocity *= f;
			}else{
				// too close? smooth towards smoothFollowDistance (needs finetuning: causes jerkyness for high weights)
				smoothFollowVelocity-=minDistance*camTrans.forward;
				dist = smoothFollowVelocity.magnitude;
				float f = dist / minDistance; 
				f = (float)System.Math.Pow(f, 50);
				smoothFollowVelocity *= f;
			}
		}
				
		return smoothFollowVelocity;
	}              
}

} // namespace

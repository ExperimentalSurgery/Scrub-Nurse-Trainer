using UnityEngine;
using System.Collections;

namespace NMY.TouchInteraction3D {

/** A terrain following camera using Ruse like interaction for panning,
    rotating and zooming above a terrain.
  */

public class RuseCamTouch : BBTouchableObject {

	/* The speed factor for panning in x-direction */
	public float translationSpeedX = 10.0f;
	
	/* The speed factor for panning in y-direction */
	public float translationSpeedY = 10.0f;
	
	public float rotationSpeed = 10.0f;
	public float scaleSpeed = 10.0f;
	
	public float translationDamping = 0.9f;
	public float rotationDamping = 0.9f;
	public float scaleDamping = 0.9f;
	
	/* Minimum distance between camera and ground. */
	public float minHeight = 0.5f;	
	/* Maximum absolute camera height. (0 for no limit) */
	public float maxHeight = 0;	

	// Minimum distance between camera and target.
	// no limit if min==max
	// NOTE: at the moment, vertical limits cannot be <-90 or >90, due to internal Unity angle computations
	//public float minDistance = 1.0f;	// NYI
	//public float maxDistance = 0f;	// no limit if 0   // NYI
	public float minHorizontal = 0f;
	public float maxHorizontal = 0f;
	public float minVertical = 2.0f;
	public float maxVertical = 75.0f;
	
	// translation limits
	// no limit if min==max
	public float minX;
	public float maxX;
	public float minZ;
	public float maxZ;

	private bool _isMoving;
	public bool isMoving { get { return _isMoving; }}

	private bool limitX, limitY;
	
	/* The maximum velocity of the camera. */
	public float maxVelocity = 5000.0f;
	public float minAngularVelocity = 0.01f;
	public float maxAngularVelocity = 10.0f;
	
	/* true: single touch will translate, too
	 * false: single touch will rotate around the initial hitpoint (similar to ExplorerCam)
	 */
	public bool singleTouchTranslation = false; // no longer necessary, default to "false".
	public bool invertWheelZoom = false;
	
	private Vector3 velocity;
	private float scaleFactor;
	private float rotVelX,rotVelY;

	private Vector3 lastHitPoint;
	private Vector3 lastScreenPosition0;
	private Vector3 lastScreenPosition1;
	private long eventID0, eventID1;
	
	private Transform camTrans;  // the camera transform (local variable for speed reasons)
	private RaycastHit hit = new RaycastHit();
	
	override protected void StartupEnter() {
		base.StartupEnter();
		camTrans = renderingCamera.transform;

		if(maxX<minX){
			Debug.LogWarning("WARNING! Illegal X limit interval! Disabling X limit.");
			minX=maxX=0;
		}
		if(maxZ<minZ){
			Debug.LogWarning("WARNING! Illegal Z limit interval! Disabling Z limit.");
			minZ=maxZ=0;
		}

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

		// do we have limits set?
		limitX=minVertical!=maxVertical;
		limitY=minHorizontal!=maxHorizontal;

		// we probably should *not* do this initialization here - we haven't touched anything yet,
		// so we don't have a reference point yet.
		/*
		if(limitY){
			float currentY=camTrans.localEulerAngles.y;
			while(maxHorizontal-currentY>=360) currentY+=360;
			while(currentY>maxHorizontal) currentY-=360;
			if(minHorizontal>currentY||maxHorizontal<currentY){
				// current angle is not in allowed interval; initialize to nearest valid boundary
				// (we might want to interpolate to that value, though)
				float maxLimit=Mathf.Min(Mathf.Abs(maxHorizontal-currentY),360-Mathf.Abs(maxHorizontal-currentY));
				float minLimit=Mathf.Min(Mathf.Abs(minHorizontal-currentY),360-Mathf.Abs(minHorizontal-currentY));
				if(minLimit>maxLimit)
					camTrans.RotateAround(lastHitPoint, Vector3.up, maxHorizontal-currentY);
				else
					camTrans.RotateAround(lastHitPoint, Vector3.up, minHorizontal-currentY);
			}
		}

		if(limitX){
			float currentX=camTrans.localEulerAngles.x;
			while(maxVertical-currentX>=360) currentX+=360;
			while(currentX>maxVertical) currentX-=360;
			if(minVertical>currentX||maxVertical<currentX){
				// current angle is not in allowed interval; initialize to nearest valid boundary
				// (we might want to interpolate to that value, though)
				float maxLimit=Mathf.Min(Mathf.Abs(maxVertical-currentX),360-Mathf.Abs(maxVertical-currentX));
				float minLimit=Mathf.Min(Mathf.Abs(minVertical-currentX),360-Mathf.Abs(minVertical-currentX));
				if(minLimit>maxLimit)
					camTrans.RotateAround(lastHitPoint, camTrans.right, maxVertical-currentX);
				else
					camTrans.RotateAround(lastHitPoint, camTrans.right, minVertical-currentX);
			}
		}
		*/

		// reset memory
		lastScreenPosition0=Vector3.zero;
		lastScreenPosition1=Vector3.zero;
		eventID0=-1;
		eventID1=-1;
	}
	
	// reset any ongoing transformations
	public void StopMotion() {
		scaleFactor=0;
		velocity=Vector3.zero;
		rotVelX=0;
		rotVelY=0;
		_isMoving=false;
	}

	void FixedUpdate() {
		// early bailout: nothing to do
		// this fixes the "camera jumps away when loooking down to an object that has a label attached" bug
		// NOTE: if we do this, camera animations or SmoothFollows may cause the cam to enter the terrain and such,
		// but terrain following is disabled this way, unless we actually interact once again.
		// The better way to fix this is to disable the label colliders while close to an object
		if(scaleFactor==0&&velocity.sqrMagnitude==0&&rotVelX==0&&rotVelY==0){
			_isMoving=false;
			return;
		}

		_isMoving=true;

		// Terrain following.. cast a ray straight down 
		// move the starting point up a bit, to make sure we don't miss anything, in case we are inside/below
		if (Physics.Raycast(camTrans.position+1000f*minHeight*Vector3.up, Vector3.up*-1.0f, out hit, Mathf.Infinity, 1 << LayerMask.NameToLayer("touchableObjects"))) {	
			float actualDistance=hit.distance-1000f*minHeight;
			if (actualDistance < minHeight) {
  				// we want to increase the velocity by a value that results in a translation of exactly +h once damping subsided
  				// for this we can use the infinity limit of our damping function h*lim(q^x), which is h/(1-q)
				// however, we need to compensate in case we ran in here before, so we remove any previous speed components along our normal
				velocity += hit.normal * ((minHeight-actualDistance)/(1f-translationDamping)-Vector3.Dot(hit.normal,velocity));
			}
		}

		float currentX=camTrans.localEulerAngles.x;
		float currentY=camTrans.localEulerAngles.y;

		// progress X rotation if above threshold
		float new_fraction=currentX;
		if(Mathf.Abs(rotVelX*Time.smoothDeltaTime)>=minAngularVelocity){
			new_fraction+=rotVelX*Time.smoothDeltaTime;
			if(limitX){
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
			if(limitY){
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

		// apply transformations. make sure scale refers to *current* camera orientation
		if(scaleFactor>0.002f||scaleFactor<-0.002f){
			camTrans.position += (scaleFactor * (lastHitPoint - camTrans.position ).normalized) * Time.smoothDeltaTime;
			scaleFactor *= scaleDamping;
		}else
			scaleFactor=0;
		if(velocity.sqrMagnitude>0.000003f){ /* minVelocity^2 */
			if(minX!=maxX){
				if(camTrans.position.x<minX&&velocity.x<0||
						camTrans.position.x>maxX&&velocity.x>0)
					velocity.x=0;
			}
			if(minZ!=maxZ){
				if(camTrans.position.z<minZ&&velocity.z<0||
						camTrans.position.z>maxZ&&velocity.z>0)
					velocity.z=0;
			}
			camTrans.position += velocity * Time.smoothDeltaTime;
			velocity *= translationDamping;	
		}else
			velocity=Vector3.zero;
		if(Xdelta!=0){
			camTrans.RotateAround(lastHitPoint, camTrans.right, Xdelta);
			rotVelX *= rotationDamping;
		}else
			rotVelX=0;
		if(Ydelta!=0){
			camTrans.RotateAround(lastHitPoint, Vector3.up, Ydelta);
			rotVelY *= rotationDamping; 
		}else
			rotVelY=0;
	}

	// specialized treatment for mouse wheel zoom
	// use Update; querying Input in FixedUpdate is non-deterministic (at least for buttons)
	void Update(){
		float wheel=Input.GetAxis("Mouse ScrollWheel");
		if(wheel==0 || isPushed) // DisplaX workaround: ignore wheel events when happening during (preferrably multi-)touch input
			return;

		// some tuning tweaks as we only get per-frame values, not the real scroll distance for fast scrolling...
		wheel=Mathf.Pow(Mathf.Abs(10*wheel),3)*(wheel<0?-1:1)/10;
		if(invertWheelZoom)
			wheel=-wheel;

		Ray screenRay=renderingCamera.ScreenPointToRay(Input.mousePosition);
		
		if (Physics.Raycast(screenRay, out hit, Mathf.Infinity, 1 << LayerMask.NameToLayer("touchableObjects"))) {	
			float distance=Vector3.Distance(hit.point,camTrans.position);
			if (wheel < 0 && distance > minHeight || // zoom in and >minHeight
			    wheel > 0 && (maxHeight == 0 || camTrans.position.y < maxHeight)) { // zoom out and <maxHeight
				// Modify the scaling velocity depending on the distance
				// Don't include the targetDirection, since it changes with rotation
				// The targetDirection will be applied on-the-fly in FixedUpdate
				scaleFactor += -wheel * distance * scaleSpeed;
			} else if (distance - velocity.magnitude < minHeight) {
				scaleFactor *= 0.01f;
			}
			
//			// Make sure the velocity does not exceed the maxVelocity setting	
			if (scaleFactor > maxVelocity) 
				scaleFactor = maxVelocity;
			lastHitPoint=hit.point;
		}
	}

	public override void handleSingleTouch(BBTouchEvent touch0) {
		if(singleTouchTranslation){
			// single touch translates as a double touch does

			BBTouchEvent[] simulateDoubleTouch=new BBTouchEvent[2];
			simulateDoubleTouch[0]=touch0;
			simulateDoubleTouch[1]=touch0;
			handleDoubleTouch(simulateDoubleTouch);
		}else{
			// single touch rotates as the ExplorerCam does, but around the first point we hit

			// first time: initialize
			if(eventID0!=touch0.eventID){
				lastScreenPosition0=touch0.screenPosition;
				eventID0=touch0.eventID;
				// remember hitpoint, our center of rotation
				lastHitPoint=touch0.rayCastHitPosition;
			}

			// === prepare rotation

			// get rotation deltas
			// NOTE: X and Y reversed since we're talking about the *rotational* axes here
			float deltaX = touch0.screenPosition.y-lastScreenPosition0.y;
			float deltaY = touch0.screenPosition.x-lastScreenPosition0.x;

			// === rotation

			// We rotate around the initial hitpoint on the ground. If no hitpoint, no rotate...
			// apply translational parts
			rotVelY += deltaY * rotationSpeed / 10.0f;
			rotVelX -= deltaX * rotationSpeed / 10.0f;

			// store our own values
			lastScreenPosition0=touch0.screenPosition;

			// reset memory for 2nd touch
			lastScreenPosition1=Vector3.zero;
			eventID1=-1;
		}
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

		// === prepare translation

		// get center hitpoint of both touches
		Vector3 centeredHitPoint=(touch0.rayCastHitPosition+touch1.rayCastHitPosition)*0.5f;

		// get translation deltas
		float deltaX = ((touch0.screenPosition.x+touch1.screenPosition.x)-(lastScreenPosition0.x+lastScreenPosition1.x))*0.5f;
		float deltaY = ((touch0.screenPosition.y+touch1.screenPosition.y)-(lastScreenPosition0.y+lastScreenPosition1.y))*0.5f;

		// === prepare scale

		// get distance of both touches
		float touchDistNow=Vector3.Distance(touch0.screenPosition,touch1.screenPosition);
		float touchDistThen=Vector3.Distance(lastScreenPosition0,lastScreenPosition1);

		// old firmware failsafes (which might place both touches on top of each other)
		if(touchDistNow==0f)
			touchDistNow=touchDistThen;
		else if(touchDistThen==0f)
			touchDistThen=touchDistNow;

		// get distance to hitpoint
		//Vector3 targetDirection=centeredHitPoint-camTrans.position;
		float distance=Vector3.Distance(centeredHitPoint,camTrans.position);

		// === prepare rotation

		float angleDelta;

		// old firmware failsafes - distance might be zero
		// (this will also happen if singleTouchTranslation==true)
		if(touchDistNow==0f){
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

		// === translation

		velocity += camTrans.right * -deltaX * translationSpeedX * (distance/400.0f);
		// remove y-component from camera's forward vec
		Vector3 camForwardFixedHeight = camTrans.forward;
		camForwardFixedHeight.y = 0;
		if(camForwardFixedHeight.sqrMagnitude<0.01f){
			camForwardFixedHeight = camTrans.up;
			camForwardFixedHeight.y = 0;
		}
		camForwardFixedHeight.Normalize();
		velocity += camForwardFixedHeight * -deltaY * translationSpeedY * (distance/400.0f);	

		// === rotation

		// We rotate around the hitpoint on the ground. If no hitpoint, no rotate...
		// apply double touch rotation
		rotVelY += angleDelta * rotationSpeed;

		// === scale/zoom

		if (touchDistNow-touchDistThen > 0 && distance > minHeight || // zoom in and >minHeight
				touchDistNow-touchDistThen < 0 && (maxHeight == 0 || camTrans.position.y < maxHeight)) { // zoom out and <maxHeight
			// Modify the scaling velocity depending on the distance
			// Don't include the targetDirection, since it changes with rotation
			// The targetDirection will be applied on-the-fly in FixedUpdate
			scaleFactor += (touchDistNow-touchDistThen) * (distance / 2000.0f) * scaleSpeed;
		} else if (distance - velocity.magnitude < minHeight) {
			scaleFactor *= 0.1f;
		}
		
		// Make sure the velocity does not exceed the maxVelocity setting	
		float velocityMagnitude=velocity.magnitude;
		if (velocityMagnitude > maxVelocity) 
			velocity = velocity * maxVelocity / velocityMagnitude;
		if (scaleFactor > maxVelocity) 
			scaleFactor = maxVelocity;
			
		lastHitPoint=centeredHitPoint;

		// store our own values
		lastScreenPosition0=touch0.screenPosition;
		lastScreenPosition1=touch1.screenPosition;
	}

	public override void handleTripleTouch(BBTouchEvent[] events) {
		// more than two events: for now, just ignore them and evaluate the first two
		handleDoubleTouch(events);
	}
}

} // namespace

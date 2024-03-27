using UnityEngine;
using System.Collections;

namespace NMY.TouchInteraction3D {

/// <summary>
/// object manipulation equivalent of RuseCamTouch: translate, scale, rotate an object with 1 or 2 touches
/// </summary>
public class ObjectRotaterTouch : BBTouchableObject 
{
	public Transform target; // the object that is being transformed
	/* The speed factor for panning in x-direction */
	public float translationSpeedX = 10.0f;
	
	/* The speed factor for panning in y-direction */
	public float translationSpeedY = 10.0f;

	public float rotationSpeed = 10.0f;
	public float scaleSpeed = 10.0f;
	
	public float translationDamping = 0.9f;
	public float rotationDamping = 0.9f;
	//public Vector3 pivot = Vector3.zero;
	public float scaleDamping = 0.9f;
	
	// object scale (="zoom") limits
	// NOTE: limit will always be enforced (unless scaleSpeed==0, disabling zoom), to prevent inf or 0 scales.
	public float minScale = 0.1f;	
	public float maxScale = 10f;
	// optional limitation of rotation angle
	// no limit if min==max
	public float minHorizontal = 0f;
	public float maxHorizontal = 0f;
	public float minVertical = 0f;
	public float maxVertical = 0f;
	// translation limits
	// no limit if min==max
	public float minX;
	public float maxX;
	public float minZ;
	public float maxZ;
	public bool useObjectYAxis = false; // true: this will stabilize rotation around the object's Y-axis, instead of a "free" rotation
	private bool limitX, limitY;
	
	public float maxVelocity = 5000.0f;
	public float minAngularVelocity = 0.01f;
	public float maxAngularVelocity = 10.0f;
	
	public bool singleTouchTranslation = false;
	public bool rotScaleAroundHitPoint = false; // true: use hitPoint. false: use transform pivot.
	public bool invertWheelZoom = false;
	public float wheelSensitivity=1f;
	public bool horizontalRotation = true;
	public bool verticalRotation = true;

	private Vector3 velocity;
	private float scaleFactor;
	private float rotVelY, rotVelX;

	// expose delta rotations, so external scripts can evaulate themselves
	[HideInInspector] public float Xdelta, Ydelta;

	private Vector3 vectorOne=Vector3.one;

	private Vector3 lastHitPoint;
	private Vector3 lastScreenPosition0;
	private Vector3 lastScreenPosition1;
	private long eventID0;
	private long eventID1;
	
	[HideInInspector] public float currentSpeed = 0f;
	
	override protected void StartupEnter() {
		Init ();

		// reset memory
		lastScreenPosition0=Vector3.zero;
		lastScreenPosition1=Vector3.zero;
		eventID0=-1;
		eventID1=-1;

		base.StartupEnter();
	}

	public void Init() {
		if(!hasStartedUp){
			Startup(); // will call Init() itself...
			return;  // ...so bailout
		}

		// default: transform ourself
		if(target==null)
			target=transform;
		
		if(maxX<minX){
			Debug.LogWarning("WARNING! Illegal X limit interval! Disabling X limit.");
			minX=maxX=0;
		}
		if(maxZ<minZ){
			Debug.LogWarning("WARNING! Illegal Z limit interval! Disabling Z limit.");
			minZ=maxZ=0;
		}

		// limits are not supported for free rotation...because its FREE! the object can assume ANY orientation
		if(!useObjectYAxis){
			Debug.LogWarning("WARNING! No limits allowed in free rotation mode (useObjectYAxis==false)! Resetting limits.");
			minHorizontal=maxHorizontal=0;
			minVertical=maxVertical=0;
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
		// => doesn't apply to ObjectRT
		while(maxVertical>=360) maxVertical-=360;
		while(maxVertical<0) maxVertical+=360;
		//if(maxVertical>90&&maxVertical<270){
		//	Debug.LogWarning("WARNING! Cannot set vertical limit outside -90..90! Clamping.");
		//	if(maxVertical>180)
		//		maxVertical=270;
		//	else
		//		maxVertical=90;
		//}
		while(maxVertical-minVertical>=360) minVertical+=360;
		while(minVertical>maxVertical) minVertical-=360;
		//if(maxVertical<=90&&minVertical<-90 || maxVertical>=270&&minVertical<270){
		//	Debug.LogWarning("WARNING! Cannot set vertical limit outside -90..90! Clamping.");
		//	if(minVertical<-180)
		//		minVertical=maxVertical;
		//	else if(minVertical<-90)
		//		minVertical=-90;
		//	else if(minVertical>180)
		//		minVertical=270;
		//	else
		//		minVertical=maxVertical;
		//}

		// do we have limits set?
		limitX=minVertical!=maxVertical;		
		limitY=minHorizontal!=maxHorizontal;
	}

	// reset any ongoing transformations
	public void StopMotion() {
		velocity=Vector3.zero;
		rotVelY=0;
		rotVelX=0;		
		scaleFactor=0;
	}

	void FixedUpdate() {
		// early bailout: nothing to do
		if(scaleFactor==0&&velocity.sqrMagnitude==0&&rotVelX==0&&rotVelY==0)
			return;

		if(!horizontalRotation)
			rotVelY=0;
		if(!verticalRotation)
			rotVelX=0;
		
		bool invert=false; // inversion of horizontal rotation necessary?
		float currentX=target.localEulerAngles.x;
		float currentY=target.localEulerAngles.y;
		if(useObjectYAxis){
			if(Vector3.Dot(target.up,renderingCamera.transform.up)<0)
				invert=true;
			currentX=-Mathf.Atan2(Vector3.Dot(target.up,renderingCamera.transform.forward),Vector3.Dot(target.up,renderingCamera.transform.up))*Mathf.Rad2Deg;
			if(currentX<0)
				currentX+=360; // stay positive
			Vector3 objectZcamForward=Vector3.Cross(renderingCamera.transform.right,target.up).normalized;
			currentY=Mathf.Atan2(Vector3.Dot(target.right,renderingCamera.transform.right),Vector3.Dot(target.right,objectZcamForward))*Mathf.Rad2Deg;
			if(currentY<0)
				currentY+=360; // stay positive
		}
		
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
					if(distToUp>distToDown&&rotVelX<0 || distToUp<distToDown&&rotVelX>0){ // NOTE: here it's inverted, as opposed to all Camera-based *CamTouch scripts!
						new_fraction=currentX;
					}
				}
			}
		}
		Xdelta=new_fraction-currentX;

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
					if(invert && (distToLeft>distToRight&&rotVelY>0 || distToLeft<distToRight&&rotVelY<0) ||
							!invert && (distToLeft>distToRight&&rotVelY<0 || distToLeft<distToRight&&rotVelY>0)){ // NOTE: here it's inverted, as opposed to all Camera-based *CamTouch scripts!
						new_fraction=currentY;
					}
				}
			}
		}
		Ydelta=new_fraction-currentY; 
		
		if(scaleFactor>0.01f||scaleFactor<-0.01f){
			float scale=target.localScale.x*((scaleFactor*Time.smoothDeltaTime)+1);
			scale=Mathf.Clamp(scale,minScale,maxScale); // hard clamping
			if(rotScaleAroundHitPoint)
				ScaleAround(target,lastHitPoint,vectorOne*scale);
			else
				target.localScale = vectorOne*scale;
			scaleFactor *= scaleDamping;
		}else
			scaleFactor=0;
		if(velocity.sqrMagnitude>0.000003f){ /* minVelocity^2 */
			if(minX!=maxX){
				if(target.position.x<minX&&velocity.x<0||
						target.position.x>maxX&&velocity.x>0)
					velocity.x=0;
			}
			if(minZ!=maxZ){
				if(target.position.z<minZ&&velocity.z<0||
						target.position.z>maxZ&&velocity.z>0)
					velocity.z=0;
			}
			target.position += velocity * Time.smoothDeltaTime;
			velocity *= translationDamping;	
		}else
			velocity=Vector3.zero;
		if(Xdelta!=0){
			if(rotScaleAroundHitPoint)
				target.RotateAround(lastHitPoint, renderingCamera.transform.right, Xdelta);
			else
				target.RotateAround(target.position, renderingCamera.transform.right, Xdelta);
			rotVelX *= rotationDamping;
		}else
			rotVelX=0;

		if(Ydelta!=0){
			// update inversion flag, as Xdelta rotation might have modified target.up
			if(useObjectYAxis && Vector3.Dot(target.up,renderingCamera.transform.up)<0)
				invert=true;
			else
				invert=false;
			if(rotScaleAroundHitPoint)
				target.RotateAround(lastHitPoint, useObjectYAxis?target.up:renderingCamera.transform.up, invert?Ydelta:-Ydelta);
			else
				target.RotateAround(target.position, useObjectYAxis?target.up:renderingCamera.transform.up, invert?Ydelta:-Ydelta);
			rotVelY *= rotationDamping; 
		}else
			rotVelY=0;

		currentSpeed = new Vector2(Xdelta, Ydelta).magnitude;
	}

	// specialized treatment for mouse wheel zoom
	// use Update; querying Input in FixedUpdate is non-deterministic (at least for buttons)
	void Update(){
		float wheel=Input.GetAxis("Mouse ScrollWheel");
		if(wheel==0 || isPushed) // DisplaX workaround: ignore wheel events when happening during (preferrably multi-)touch input
			return;
		
		// some tuning tweaks as we only get per-frame values, not the real scroll distance for fast scrolling...
		wheel=Mathf.Pow(Mathf.Abs(10*wheel),3)*(wheel<0?-1:1)/100*wheelSensitivity;
		if(invertWheelZoom)
			wheel=-wheel;

		Ray screenRay=renderingCamera.ScreenPointToRay(Input.mousePosition);

		if (Physics.Raycast(screenRay, Mathf.Infinity, 1 << LayerMask.NameToLayer("touchableObjects"))) {	
			float scale=target.localScale.x;
			if (wheel < 0 && scale < maxScale || // zoom in and >minHeight
			    wheel > 0 && scale > minScale) { // zoom out and <maxHeight
				// Modify the scaling velocity depending on the scale
				// Don't include the targetDirection, since it changes with rotation
				// The targetDirection will be applied on-the-fly in FixedUpdate
				scaleFactor += -wheel * scale * scaleSpeed;
			} else if (scale /* - velocity.magnitude*/ <= minScale || 
			           scale >= maxScale) {
				scaleFactor *= 0.01f;
			}
			
			//// Make sure the velocity does not exceed the maxVelocity setting	
			//if (scaleFactor > maxVelocity) 
			//	scaleFactor = maxVelocity;
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
			// single touch rotates

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

			// apply translational parts
			rotVelX += deltaX * rotationSpeed / 10.0f;
			rotVelY += deltaY * rotationSpeed / 10.0f;

			// store our own values
			lastScreenPosition0=touch0.screenPosition;

			// reset memory for 2nd touch
			lastScreenPosition1=Vector3.zero;
			eventID1=-1;
		}
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
		float distance=Vector3.Distance(centeredHitPoint,renderingCamera.transform.position);

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

		velocity += renderingCamera.transform.right * deltaX * translationSpeedX * (distance/400.0f);
		// remove y-component from camera's forward vec
		Vector3 camForwardFixedHeight = renderingCamera.transform.forward;
		camForwardFixedHeight.y = 0;
		if(camForwardFixedHeight.sqrMagnitude<0.01f){
			camForwardFixedHeight = renderingCamera.transform.up;
			camForwardFixedHeight.y = 0;
		}
		camForwardFixedHeight.Normalize();
		velocity += camForwardFixedHeight * deltaY * translationSpeedY * (distance/400.0f);	

		// === rotation

		if(!singleTouchTranslation){
			// apply translational parts
			rotVelX += deltaX * rotationSpeed / 10.0f;
			rotVelY += deltaY * rotationSpeed / 10.0f;
		}
		
		// We rotate around the hitpoint on the ground. If no hitpoint, no rotate...
		// apply double touch rotation
		rotVelY += angleDelta * rotationSpeed;
		
		// === scale/zoom

		if (target.localScale.x < maxScale && touchDistNow-touchDistThen > 0 ||
				(target.localScale.x > minScale) && touchDistNow-touchDistThen < 0) {
			// Modify the scaling velocity depending on the distance
			scaleFactor += (touchDistNow-touchDistThen) * (1 / 2000.0f) * scaleSpeed;
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

	public override void noTouches() {
		// reset memory
		lastScreenPosition0=Vector3.zero;
		lastScreenPosition1=Vector3.zero;
		eventID0=-1;
		eventID1=-1;
	}

	/// <summary>
	/// Scales the target around an arbitrary point by scaleFactor.
	/// This is relative scaling, meaning using  scale Factor of Vector3.one
	/// will not change anything and new Vector3(0.5f,0.5f,0.5f) will reduce
	/// the object size by half.
	/// The pivot is assumed to be the position in the space of the target.
	/// Scaling is applied to localScale of target.
	/// https://forum.unity.com/threads/scale-around-point-similar-to-rotate-around.232768/#post-5505829
	/// </summary>
	/// <param name="target">The object to scale.</param>
	/// <param name="pivot">The point to scale around in space of target.</param>
	/// <param name="scaleFactor">The factor with which the current localScale of the target will be multiplied with.</param>
	public static void ScaleAroundRelative(Transform target, Vector3 pivot, Vector3 scaleFactor)
	{
		// pivot
		var pivotDelta = target.localPosition - pivot;
		pivotDelta.Scale(scaleFactor);
		target.localPosition = pivot + pivotDelta;
 
		// scale
		var finalScale = target.localScale;
		finalScale.Scale(scaleFactor);
		target.localScale = finalScale;
	}
 
	/// <summary>
	/// Scales the target around an arbitrary pivot.
	/// This is absolute scaling, meaning using for example a scale factor of
	/// Vector3.one will set the localScale of target to x=1, y=1 and z=1.
	/// The pivot is assumed to be the position in the space of the target.
	/// Scaling is applied to localScale of target.
	/// https://forum.unity.com/threads/scale-around-point-similar-to-rotate-around.232768/#post-5505829
	/// </summary>
	/// <param name="target">The object to scale.</param>
	/// <param name="pivot">The point to scale around in the space of target.</param>
	/// <param name="scaleFactor">The new localScale the target object will have after scaling.</param>
	public static void ScaleAround(Transform target, Vector3 pivot, Vector3 newScale)
	{
		// pivot
		Vector3 pivotDelta = target.localPosition - pivot; // diff from object pivot to desired pivot/origin
		Vector3 scaleFactor = new Vector3(
			newScale.x / target.localScale.x,
			newScale.y / target.localScale.y,
			newScale.z / target.localScale.z );
		pivotDelta.Scale(scaleFactor);
		target.localPosition = pivot + pivotDelta;
 
		//scale
		target.localScale = newScale;
	}
}

} // namespace

using UnityEngine;
using System.Collections;

namespace NMY.TouchInteraction3D {

/** A zoom+pan script to 2D navigate an object. no rotation.
 *  The object is assumed to be a standard 10x10 plane in XY (rotX=270)
 *  Dual finger Zoom is absolute; the touchpoints determine the size+shape of the plane directly
  */

public class PanObjTouch : BBTouchableObject {

	/* The speed factor for panning in x-direction */
	public float translationSpeedX = 10.0f;
	
	/* The speed factor for panning in y-direction */
	public float translationSpeedY = 10.0f;
	
	/* The speed factor for panning in y-direction */
	public float translationSpeedZ = 10.0f;
	
	public float zoomSpeed = 10.0f;
	
	public float translationDamping = 0.9f;
	public float zoomDamping = 0.9f;
	
	// Minimum view angle of camera.
	// no limit if min==max
	// NOTE: at the moment, vertical limits cannot be <-90 or >90, due to internal Unity angle computations
	public float minAngle = 1.0f;	
	public float maxAngle = 90f;

	public bool round=false;
	public bool uniformScaling=false;
	
	// translation limits
	// no limit if min==max
	public float minX;
	public float maxX;
	public float minY;
	public float maxY;
	public float minZ;
	public float maxZ;
	public bool lockZ=false;

	/* The maximum velocity of the camera. */
	public float maxVelocity = 5000.0f;
	
	private Vector3 velocity;
	private float zoomFactor;

	private Vector3 lastScreenPosition0;
	//private Vector3 lastScreenPosition1; // unused. commented out to prevent stupid warning...
	private long eventID0, eventID1;
	
	private Transform camTrans;  // the camera transform (local variable for speed reasons)
	
	private float screenScale;

	override protected void StartupEnter() {
		base.StartupEnter();
		camTrans = renderingCamera.transform;

		// for resolution independent translations
		screenScale = 1080f/Screen.height;

		if(maxX<minX){
			print("WARNING! Illegal X limit interval! Disabling X limit.");
			minX=maxX=0;
		}
		if(maxY<minY){
			print("WARNING! Illegal Y limit interval! Disabling Y limit.");
			minY=maxY=0;
		}
		if(maxZ<minZ){
			print("WARNING! Illegal Z limit interval! Disabling Z limit.");
			minZ=maxZ=0;
		}

		// reset memory
		lastScreenPosition0=Vector3.zero;
		//lastScreenPosition1=Vector3.zero;
		eventID0=-1;
		eventID1=-1;
	}
	
	// explicitly force size to given extremas (plus margin)
	// NOTE: this will only work for a Plane with (10/10/0), rotated by x=270...
	public void SetSize(Vector3 minP, Vector3 maxP){
		SetSize(minP,maxP,false);
	}

	public void SetSize(Vector3 minP, Vector3 maxP, bool forceZ){
		float distance=Vector3.Dot(renderingCamera.transform.forward,minP-renderingCamera.transform.position);
		float height=2*Mathf.Tan(renderingCamera.fieldOfView/2*Mathf.Deg2Rad)*distance;
		float xScale=Mathf.Abs(maxP.x-minP.x);
		float yScale=Mathf.Abs(maxP.y-minP.y);
		xScale+=0.1f*height;
		yScale+=0.1f*height;
		if(round){
			// add some pretty strange magic to ensure we're always in the circle with a fixed safety margin
			float angle=Mathf.Atan2(yScale,xScale);
			xScale*=Mathf.Sin(angle+Mathf.PI/4)*1.414f;
			yScale*=Mathf.Sin(angle+Mathf.PI/4)*1.414f;
		}
		if(uniformScaling){
			if(xScale<yScale)
				xScale=yScale;
			else if(yScale<xScale)
				yScale=xScale;
		}

		Vector3 newPos=(minP+maxP)*0.5f;
		if(lockZ&&!forceZ)
			newPos.z=transform.position.z;
		transform.position=newPos;
		transform.localScale=new Vector3(xScale,1,yScale)*0.1f;
	}

	void FixedUpdate() {
		// apply transformations
		if(velocity.sqrMagnitude>0.0001f){
			transform.localPosition += velocity * Time.smoothDeltaTime;
			Vector3 clampedPosition=transform.localPosition;
			if(minX!=maxX)
				clampedPosition.x=Mathf.Clamp(clampedPosition.x,minX,maxX);
			if(minY!=maxY)
				clampedPosition.y=Mathf.Clamp(clampedPosition.y,minY,maxY);
			if(minZ!=maxZ)
				clampedPosition.z=Mathf.Clamp(clampedPosition.z,minZ,maxZ);
			transform.localPosition=clampedPosition;
			// Apply the transformation damping
			velocity *= translationDamping;	
		}
	}
	
	public override void handleSingleTouch(BBTouchEvent touch0) {
		// NOTE: for multi touches we CANNOT rely on event.lastScreenPosition, since they are updated
		// individually, and do *not* contain the position when handleDoubleTouch() was last called!
		// So we need to store our own version of that.

		// first time: initialize
		if(eventID0!=touch0.eventID){
			lastScreenPosition0=touch0.screenPosition;
			eventID0=touch0.eventID;
		}

		// === prepare translation

		// get translation deltas
		float deltaX = touch0.screenPosition.x-lastScreenPosition0.x;
		float deltaY = touch0.screenPosition.y-lastScreenPosition0.y;

		// approximate "distance" by view angle
		float distance=renderingCamera.fieldOfView;

		// === translation

		velocity += camTrans.right * deltaX * translationSpeedX * screenScale * (distance/1000.0f);
		velocity += camTrans.up * deltaY * translationSpeedY * screenScale * (distance/1000.0f);	
		velocity += camTrans.forward * deltaY * translationSpeedZ * screenScale * (distance/1000.0f);	
		if(lockZ)
			velocity.z=0;

		// Make sure the velocity does not exceed the maxVelocity setting	
		if (velocity.magnitude > maxVelocity) 
			velocity = velocity.normalized * maxVelocity;
			
		// store our own values
		lastScreenPosition0=touch0.screenPosition;
	}

	public override void noTouches() {
		// reset memory
		lastScreenPosition0=Vector3.zero;
		//lastScreenPosition1=Vector3.zero;
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
			//lastScreenPosition1=touch1.screenPosition;
			eventID0=touch0.eventID;
			eventID1=touch1.eventID;
		}

		// === prepare zoom

		// get distance of both touches
		float touchDistNow=Vector3.Distance(touch0.screenPosition,touch1.screenPosition);

		// old firmware failsafes (which might place both touches on top of each other)
		if(touchDistNow!=0f&&zoomSpeed>0)
			SetSize(touch0.rayCastHitPosition,touch1.rayCastHitPosition);
			
		// store our own values
		lastScreenPosition0=touch0.screenPosition;
		//lastScreenPosition1=touch1.screenPosition;
	}

	public override void handleTripleTouch(BBTouchEvent[] events) {
		// more than two events: for now, just ignore them and evaluate the first two
		handleDoubleTouch(events);
	}
}

} // namespace
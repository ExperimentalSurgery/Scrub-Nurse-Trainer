using UnityEngine;
using System.Collections;

/*
 * this is no longer a requirement, it was just used for experimenting
[System.FlagsAttribute]
public enum BBTouchEventState {
	Began=0x01,
	Moved=0x02,
	Stationary=0x04,
	Ended=0x08
};
*/

public enum BBTouchEventState {
	Began,
	Moved,
	Stationary,
	Ended
};

/*
 * note: symbolID and tuioAngle are unused so far
 */
public class BBTouchEvent {
	public GameObject hitObject;
	public Camera camera; // the camera associated with this event (required if we have >1 or non-fullscreen cameras)

	public BBTouchEventState eventState;
	public bool didChange = false;

	public long eventID; // the unique ID for this touch, it will be the same throught the life of this touch
	public int symbolID; // the symbolID for this touch, it is unique for each symbol and set by convention to -1 for cursors

	public Vector2 normPosition; // the 2d position of this touch normalized to 0..1,0..1
	public Vector2 initialPosition; // the 2d position this touch event had during it's "down" phase, normalized to 0..1,0..1
	public Vector3 lastScreenPosition; // the most recent 2d position of this touch on the screen
	public Vector3 screenPosition; // the 2d position of this touch on the screen
	public float tuioAngle;
	public float lastAngle;
		
	public Vector3 rayCastHitPosition; // the 3d point where this touch event ray cast into teh scene and collided with something
	public Vector3 lastRayCastHitPosition; // the previous hit location
	public Vector3 rayCastClosestHitPosition; // if the touch left the object, the point on the ray closest to the object
	public Vector3 colliderClosestHitPosition; // if the touch left the object, the point on the collider bbox closest to the ray
	public Vector2 textureCoordHitPosition;

	public float touchTime;
	public float lastTouchTime;

	public BBTouchEvent(long id)
	{
		this.eventID = id;
		this.symbolID = -1;
	}
	
	public BBTouchEvent(long id, int symbol)
	{
		this.eventID = id;
		this.symbolID = symbol;
	}
	
	// copy constructor
	public BBTouchEvent(BBTouchEvent source)
	{
		hitObject=source.hitObject;
		camera=source.camera;
		eventState=source.eventState;
		eventID=source.eventID;
		symbolID=source.symbolID;
		normPosition=source.normPosition;
		initialPosition=source.initialPosition;
		lastScreenPosition=source.lastScreenPosition;
		screenPosition=source.screenPosition;
		tuioAngle=source.tuioAngle;
		lastAngle=source.lastAngle;
		rayCastHitPosition=source.rayCastHitPosition;
		lastRayCastHitPosition=source.lastRayCastHitPosition;
		rayCastClosestHitPosition=source.rayCastClosestHitPosition;
		colliderClosestHitPosition=source.colliderClosestHitPosition;
		textureCoordHitPosition=source.textureCoordHitPosition;
		touchTime=source.touchTime;
		lastTouchTime=source.lastTouchTime;
	}
}


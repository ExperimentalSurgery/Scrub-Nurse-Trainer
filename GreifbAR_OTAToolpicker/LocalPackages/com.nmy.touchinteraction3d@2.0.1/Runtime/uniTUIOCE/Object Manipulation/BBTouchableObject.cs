using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using NMY;

// based on the "original" uniTUIO code (with object association) from Unity 2.6
// restructured and severely de-bugged by W.Kresse
public class BBTouchableObject : StartupBehaviour {

	private Dictionary<long,BBTouchEvent> touchEvents = new Dictionary<long,BBTouchEvent>(19);

	// NOTE: this is null by default!
	// If "objectToManipulate" is set to a GameObject other than ourself,
	// all touch events will be piped to that object, too.
	// This can be used to let another GameObject know of an already ongoing touch associated with *this* object.
	// If that other object is merely another touchable Component, or resides within the touch hierarchy of this object,
	// it suffices to just enable/disable that component instead.
	// Disabled Components will receive all touch events, they just won't process them.
	private GameObject _objectToManipulate = null;
	public GameObject objectToManipulate
	{
		get { return _objectToManipulate; }
		set {
			if(value!=_objectToManipulate){
				if(value!=null && value.transform.IsChildOf(transform)){
					Debug.LogError("ASSERT: BBTouchableObject.objectToManipulate mustn't be set to a child of itself, this should be corrected! Ignoring request.");
				}else{
					if(_objectToManipulate!=null){
						// cleanup: the slave object changes, so tell it to clear its events and finish processing
						// NOTE we only call _objectToManipulate directly, not its ancestors,
						// since we shouldn't unconditionally delete *all* active touch events of that hierarchy,
						// only the ones affected by _objectToManipulate. clearAndFinishFrame() will take care of that.
						_objectToManipulate.SendMessage("clearAndFinishFrame",SendMessageOptions.DontRequireReceiver);
					}
					_objectToManipulate = value;
					if(_objectToManipulate!=null){
						// clone current event list and send to new object
#if UNITY_FLASH
						foreach(long flashFix in touchEvents.Keys)
							_objectToManipulate.SendMessageUpwards("queueEvent",touchEvents[flashFix],SendMessageOptions.DontRequireReceiver);
#else
						foreach(BBTouchEvent e in touchEvents.Values)
							_objectToManipulate.SendMessageUpwards("queueEvent",e,SendMessageOptions.DontRequireReceiver);
#endif
					}
				}
			}
		}
	}

	public Camera renderingCamera;

	// publicly accessible: is at least one touch event associated with this object?
	public bool isPushed {
		get {
			return touchEvents.Count>0;
		}
	}

	override protected void StartupEnter() {
		if (renderingCamera == null)
			renderingCamera=Camera.main;
	}

	override protected void StartupExit(){
		noTouches();
		base.StartupExit();
	}

	virtual protected void queueEvent(BBTouchEvent theEvent)
	{
		// Began: add. Moved: replace. Ended: remove.
		if (theEvent.eventState != BBTouchEventState.Began)
			touchEvents.Remove(theEvent.eventID);
		if (theEvent.eventState != BBTouchEventState.Ended)
			touchEvents.Add(theEvent.eventID,theEvent);

		// do we have a slave object? let it know of the event.
		if(_objectToManipulate!=null)
			_objectToManipulate.SendMessageUpwards("queueEvent",theEvent,SendMessageOptions.DontRequireReceiver);
	}

	void OnDisable(){
		// Cleanup!
		// This is essential if we deactivate (or more importantly: destroy) objects while being touched (e.g., by fading them out and setting active=false).
		// Otherwise we end up with event zombies that are never removed!
		// This might also solve our persistent-scene-switch-pending-event-problems
//		if(isPushed)
//			DebugConsole.Log("CLEANUP "+name+" "+touchEvents.Count,"warning");
		clearAndFinishFrame();
	}

	// only called if we are the slave object of another BBTouchableObject
	virtual protected void clearAndFinishFrame(){
		if(isPushed){
			// if we have touch scripts attached to any of our parents and we are destroyed (a simple GO.active=false seems to be unaffected),
			// they won't be updated of our local cleanup. so we selectively remove all our touch events from their queues.
			BBTouchEvent[] eventList=new BBTouchEvent[touchEvents.Count];
			touchEvents.Values.CopyTo(eventList,0);
			foreach(BBTouchEvent ev in eventList){
				ev.eventState=BBTouchEventState.Ended; // NOTE: directly modifies all references - which is OK here, since they are being removed anyways
				if(!gameObject.activeInHierarchy)
					queueEvent(ev); // workaround: SendMessageUpwards() is not received if we got deactivated! NOTE: deactivated objects further up in the hierarchy will NOT cleanup correctly this way!
				else
					SendMessageUpwards("queueEvent",ev,SendMessageOptions.DontRequireReceiver);
			}
			
			// cleanup locally
			touchEvents.Clear();
			// process pending events (including us)
			if(!gameObject.activeInHierarchy)
				finishFrame(); // workaround: SendMessageUpwards() is not received if we got deactivated! NOTE: deactivated objects further up in the hierarchy will NOT cleanup correctly this way!
			else
				SendMessageUpwards("finishFrame",null,SendMessageOptions.DontRequireReceiver);
		}
	}

	virtual protected void finishFrame()
	{
		// do we have a slave object? let it process the events, too.
		if(_objectToManipulate!=null)
			_objectToManipulate.SendMessageUpwards("finishFrame",SendMessageOptions.DontRequireReceiver);

		if (touchEvents.Count == 0)
			this.noTouches();
		else if(enabled){ // don't do nothing if we're disabled (however, do the cleanup above)
			BBTouchEvent[] eventList=new BBTouchEvent[touchEvents.Count];
#if UNITY_FLASH
				int i=0;
				foreach(long flashFix in touchEvents.Keys)
					eventList[i++]=touchEvents[flashFix];
#else
			touchEvents.Values.CopyTo(eventList,0);
#endif
			if (touchEvents.Count == 1)
				this.handleSingleTouch(eventList[0]);
			else if (touchEvents.Count == 2)
				this.handleDoubleTouch(eventList);
			else
				this.handleTripleTouch(eventList);
		}
	}
	
	virtual public void handleTripleTouch(BBTouchEvent[] events) {}
	virtual public void handleDoubleTouch(BBTouchEvent[] events) {}
	virtual public void handleSingleTouch(BBTouchEvent aTouch) {}
	virtual public void noTouches() {}

}

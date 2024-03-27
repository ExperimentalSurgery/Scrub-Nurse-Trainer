//#define TOUCHDEBUG
// #define NMY_HAS_STEREO3D // TODO: set globally or by automatic means...
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Net;
using System.Linq;

// based on the "original" uniTUIO code (with object association) from Unity 2.6
// restructured and severely de-bugged by W.Kresse

// Marker Tracking
// ===============
// As opposed to touch cursors (which determine their target object(s) via Raycast),
// objects interested in marker events need to register/unregister with us.
// There is no "BBTouchableObject" class for markers, so any class can register.
//
// The callback methods that will then be called are:
// MarkerAdded(BBTouchEvent);
// MarkerMoved(BBTouchEvent);
// MarkerRemoved(BBTouchEvent);
//
// Query functions:
// int GetMarkerIDFromIndex(symbolIndex): map symbol index to binary ID via markerMapping[]
// BBTouchEvent GetMarkerEvent(symbolIndex): get the current state of the marker
// bool IsSameID(symbolID, symbolIndex): compare two symbol IDs (one will be mapped via markerMapping[]) using bit tolerance

// TODO 20130515 WK: marker positions for cameras with non-fullscreen viewports

	
// uniTUIO bugfix 100526:
// The most important change is that we mustn't modify pending events, especially if it's a "Began" event that hasn't been processed yet
// However, we cannot call Physics.Raycast() outside Update()-functions, because Unity will barf.
// So we keep this silly eventQueue, but actually COPY events we want to hold in there, if their status (Began/Moved/Ended) changes.
// If we receive a Moved event for an event that already has the Moved state, we can safely replace it regularly (if we don't, we'd gather up 
// zillions of Moved events which need to be processed once Update() (or FixedUpdate() for that matter) is launched, because TUIO might easily
// generate events faster than a slow performance scene gets to call Update/FixedUpdate).
// However, now we need to take care that events in the queue - which are modified themselves in Update()... - update these changes to all other
// events in the queue or in that activeEvents-Dictionary which have the same ID. Also, we have to be careful to always refer to the most recent
// version (and ONLY that) of any events with the same ID in BBTouchableObject
//
// The effect is that touches are now correctly recognized, even if we receive a cursorUp before we even process the event caused by cursorDown.
// The only semi-important information that gets lost at the moment is any change in movement between a cursorDown and the current cursor position when
// the cursorDown event is processed.

// Windows native touch events and forced OS mouse emulation
// =========================================================
//
// By now (20141201), this code is cluttered with special cases trying to catch and workaround Windows touch bugs where Windows will fire 
// mouse-emulation events, marking some *actual* touch events as mouse events in the process (see cursorDown() and the isPen-test).
// Maybe there is some better "isPen"-test out there somewhere which prevents these problems altogether? (haha, right...)
// In addition, the behaviour is completely different not only between Windows7 and Windows8, but also for different touch hardware...

// NOTE: config flags should be passed through from BBTouchManagerStarter, since BBTouchEventManager will be created on-the-fly.
public abstract class BBInputDelegate : MonoBehaviour {

#if UNITY_STANDALONE_WIN
	[DllImport("user32.dll")]
	private static extern uint GetMessageExtraInfo( );
#endif
	
	public static bool useTUIO=true; // static bool to preempt choice of singleton via BBTouchManagerStarter...

	// true: create touch events from mouse events
	public bool emulateMouseEvents = true;

	public float displayStretchFactor = 1;
	
	// Set this to true if the number of active Cameras in the scene does not change.
	// Will increase performance, especially for large scenes.
	// Required to true in ATM FlightPath scene due to severe performance hits.
	// Phenomenon when set to false: on every touch, the scene freezes shortly.
	// Drawback when set to true: set of Cameras that are tested for touches is not updated.
	public bool searchCamsOnDemandOnly = false;

	// false: original variant: asynchronous TUIO, pushing events
	// true: new variant: inputDelegate is polling events by himself
	// NOTE: this will not influence emulated mouse events
	public bool usePolling=true;
	//public float TUIOUpdateFrequency = 100.0f;

	// act as a TUIO server, distributing the events
	public bool sendTUIO = false;
	public string remoteIP = "192.168.0.152";
	public int remotePort = 3333;
	private OSC.NET.OSCTransmitter oscTransmitter;
	private string sourceName;
	private int currentTUIOSeqID; // sequence IDs
	
	// so far supported for native TUIO only!
	public bool detectMarkers = false;
	
	// map from consecutive indices to marker IDs
	public List<int> markerMapping = new List<int>();
	// reverse-mapping from markerIDs to markerMapping indices
	public Dictionary<int,int> markerIDtoIndex = new Dictionary<int,int>();
	
	// number of allowed bit errors to still correctly identify a registered marker
	// if tolerance is not possible, print error on DebugConsole
	// NOTE: using a property to circumvent initialization order problems when setting
	public int _markersMaxBitTolerance = 2;
	public int markersMaxBitTolerance {
		get {
			return _markersMaxBitTolerance;
		}
		set {
			_markersMaxBitTolerance=value;
			// recompute bit differences of all markerMapping[] markers to figure out actual allowed bit tolerance
			markersCurrentBitTolerance=_markersMaxBitTolerance;
			for(int i=0;i<markerMapping.Count-1;i++){
				for(int j=i+1;j<markerMapping.Count;j++){
					int hammingDistance=NMY.StaticUtils.GetHammingDistance(markerMapping[i],markerMapping[j]); // can't be 0, since this is the "else" case
					int allowedTolerance=(hammingDistance-1)>>1;
//					print (symbol+" "+key+" -> "+hammingDistance+" "+allowedTolerance);
					if(allowedTolerance<markersCurrentBitTolerance)
						markersCurrentBitTolerance=allowedTolerance;
				}
			}
			if(markersCurrentBitTolerance<_markersMaxBitTolerance){
				// Console.isVisible=true;
				Debug.LogWarning("Warning! Requested bit tolerance "+markersMaxBitTolerance+" not achievable with given marker set! Using "+markersCurrentBitTolerance+" instead.");
			}
		}
	}
	protected int markersCurrentBitTolerance;
	
	// symbolID->receiving scripts
	protected Dictionary<int, List<Component>> allMarkers = new Dictionary<int, List<Component>>();
	
	protected List<GameObject> objectsToNotify = new List<GameObject>();
 
 	public string touchableObjectLayer = "touchableObjects"; // this is our primary layer for touchable objects
 	public string controlObjectLayer = "controlObjects"; // this layer has priority over the standard layer. objects in here will be hit/checked first, and only if nothing is hit, the primary layer will be checked
 	
	protected int layerControlMask;
	protected int layerTouchMask;
	
	public Dictionary<long,BBTouchEvent> activeEvents = new Dictionary<long,BBTouchEvent>(100);
	volatile protected List<BBTouchEvent> eventQueue = new List<BBTouchEvent>();
	protected Object eventQueueLock = new Object();
	protected BBCrosshairController crosshairController;

	public const int STARTID_EXTERNAL =    100000000; // used for TUIO(!) events, to distinguish them from Unity's Input.Touch events
	protected const int STARTID_MOUSEMARKER = 400000000; // something large we hopefully never reach for regular TUIO events...(but not fatal if we do)
	protected const int STARTID_MOUSETOUCH =  500000000;
	private int fakeEventID = STARTID_MOUSETOUCH;
	// for 2-finger scale/rotation
#if !UNITY_FLASH
	private Vector3 fakeposCenter;
#endif
	private Vector3 fakeposCenterFar;
	private int fakeMarkerIndex;
	private float fakeMarkerSqrRadius=(58/1080f)*(58/1080f); // 58 pixels for 52mm markers on 55" 1920x1080 display
	private Vector3 clickoffset; // offset from marker center when clicking
	private float clickangle; // angle of marker when clicking
	private BBTouchEvent currentFakeMarker;
	private int oldScreenHeight;
	private int oldScreenWidth;
	
	private int previousDownFrame;
	private float previousDownX;
	private float previousDownY;

	protected float isSamePosDelta=1e-4f; // something smaller than 1/maxResolution, but also used for rad angles

	protected const float TAU=Mathf.PI*2;

	// TouchManager singleton
	// This defines a static instance property that attempts to find the manager object in the scene and
	// returns it to the caller.
	public static BBInputDelegate instance {
		get {
            if (sharedEventManager == null) {
				// This is where the magic happens.
				//  FindObjectOfType(...) returns the first Manager object in the scene.
				sharedEventManager =  FindObjectOfType(typeof (BBInputDelegate)) as BBInputDelegate;
				if (useTUIO && sharedEventManager!=null && !(sharedEventManager is BBTouchEventManager) ||
						!useTUIO && (sharedEventManager is BBTouchEventManager)){
					Debug.LogError("Found TouchManager instance of incorrect type! Destroying existing one!");
					Destroy(sharedEventManager.gameObject);
					sharedEventManager=null;
				}
				if (sharedEventManager == null){
					if(useTUIO){
						GameObject go=new GameObject("TUIOEventManager");
						sharedEventManager = go.AddComponent<BBTouchEventManager>();
					}else{
						GameObject go=new GameObject("iPhoneEventManager");
						sharedEventManager = go.AddComponent<BBIPhoneTouchManager>();
					}
				}
			}
			return sharedEventManager;
		}
	}
	
	protected static BBInputDelegate sharedEventManager = null; // the actual singleton, as determined by the child class

	// test for Singleton instance without instancing
	public static bool Exists(){
		return sharedEventManager != null;
	}

	protected virtual void Awake () {
		layerControlMask = 1 << LayerMask.NameToLayer(controlObjectLayer);		
		layerTouchMask = 1 << LayerMask.NameToLayer(touchableObjectLayer);		
		DontDestroyOnLoad(this);
		crosshairController=FindObjectOfType(typeof(BBCrosshairController)) as BBCrosshairController;
		markersCurrentBitTolerance=_markersMaxBitTolerance;
		oldScreenWidth=Screen.width;
		oldScreenHeight=Screen.height;
#if UNITY_SAMSUNGTV
		SamsungTV.touchPadMode=SamsungTV.TouchPadMode.Mouse;
#endif
#if TOUCHDEBUG
		DebugConsole.isVisible=true;
#endif
		SceneManager.sceneLoaded+=OnLevelWasLoadedDelegate;
	}
	
	protected virtual void OnLevelWasLoadedDelegate(Scene scene, LoadSceneMode m){
		crosshairController=FindObjectOfType(typeof(BBCrosshairController)) as BBCrosshairController;
		markersCurrentBitTolerance=_markersMaxBitTolerance;
	}

	public void ReplaceMarkerMapping(int[] newMapping){
		markerMapping=new List<int>(newMapping);
		// update reverse mapping
		markerIDtoIndex.Clear();
		for(int i=0;i<markerMapping.Count;i++)
			markerIDtoIndex[markerMapping[i]]=i;
	}

	// if we get destroyed, make sure we don't have any pending events
	void OnDisable(){
		eventQueue.Clear();
		foreach(KeyValuePair<long,BBTouchEvent> entry in activeEvents){
			if(entry.Value.symbolID<0)
				Debug.LogWarning("Cleaning up active event on scene switch "+entry.Value.eventState+" id "+entry.Value.eventID+". This should be ok.");
			entry.Value.eventState = BBTouchEventState.Ended;	
			lock (eventQueueLock) { 
				eventQueue.Add(new BBTouchEvent(entry.Value));
			}		
		}
		activeEvents.Clear();
	}

	void OnEnable(){
		// debug - shouldn't happen
		foreach(KeyValuePair<long,BBTouchEvent> entry in activeEvents){
			Debug.LogError("Cleaning up active event in OnEnable! "+entry.Value.eventState+" id "+entry.Value.eventID);
			entry.Value.eventState = BBTouchEventState.Ended;	
			lock (eventQueueLock) { 
				eventQueue.Add(new BBTouchEvent(entry.Value));
			}		
		}
		activeEvents.Clear();
	}

	public void RegisterToMarker(Component obj, int symbolIndex){
		if(symbolIndex<0 || symbolIndex>=markerMapping.Count){
			// DebugConsole.isVisible=true;
			Debug.LogWarning("Warning! Trying to register index "+symbolIndex+" outside predefined range! Assigning ID "+GetMarkerIDFromIndex(symbolIndex)+".");
		}
		int symbol=GetMarkerIDFromIndex(symbolIndex);
#if TOUCHDEBUG
        Debug.Log("Registering " + obj.name + " to marker " + symbolIndex + "(" + symbol + ").");
#endif
		List<Component> registeredComponents=null;
		if(allMarkers.ContainsKey(symbol))
			registeredComponents=allMarkers[symbol];
		else{
			registeredComponents=new List<Component>();
			allMarkers[symbol]=registeredComponents;
		}
		if(registeredComponents.Contains(obj)){
			Debug.LogError("Object \""+obj.name+"\" already registered to marker "+symbol+"!");
			return;
		}
		registeredComponents.Add(obj);
		
		// marker already active at registration time? spawn activation message!
		BBTouchEvent activeEvent=GetMarkerEvent(symbolIndex);
		if(activeEvent!=null)
			obj.SendMessage("MarkerAdded",activeEvent,SendMessageOptions.DontRequireReceiver);
	}
	
	public void UnregisterFromMarker(Component obj, int symbolIndex){
		int symbol=GetMarkerIDFromIndex(symbolIndex);
#if TOUCHDEBUG
        Debug.Log("Unregistering "+obj.name+" from marker " + symbolIndex + "(" + symbol + ").");
#endif
		if(!allMarkers.ContainsKey(symbol) || !allMarkers[symbol].Contains(obj)){
			Debug.LogError("Object \""+obj.name+"\" not registered with marker "+symbol+"!");
			return;
		}
		allMarkers[symbol].Remove(obj);
		
		// marker still active at deregistration time? spawn deactivation message!
		BBTouchEvent activeEvent=GetMarkerEvent(symbolIndex);
		if(activeEvent!=null)
			obj.SendMessage("MarkerRemoved",activeEvent,SendMessageOptions.DontRequireReceiver);
	}
	
	private void updateEvent(BBTouchEvent anEvent, float x, float y)
	{
		anEvent.lastScreenPosition = anEvent.screenPosition;
		anEvent.normPosition = new Vector2(x,(1.0f - y));
		anEvent.screenPosition = new Vector3(x * Screen.width,(1.0f - y) * Screen.height,0.3f); 
		anEvent.lastTouchTime = anEvent.touchTime;
		anEvent.touchTime = Time.unscaledTime;
		anEvent.didChange = true;
	}

	private void updateEvent(BBTouchEvent anEvent, int symbol, float x, float y, float a)
	{
		anEvent.symbolID = symbol;
		anEvent.lastAngle = anEvent.tuioAngle;
		anEvent.tuioAngle = a;
		updateEvent(anEvent,x,y);
	}

	// objectAdd is for new marker events. we take the touch data (TUIO 2D object) and convert it
	// into a touch event, and add it to our active list of events
	// marker equivalent to cursorDown
	public virtual BBTouchEvent objectAdd(long id, int symbol, float x, float y, float angle){
		if(!detectMarkers)
			return null;
		
		// first, make a new BBTouchEvent, tag it with the unique touch id
		BBTouchEvent newEvent = new BBTouchEvent(id);
#if TOUCHDEBUG
		DebugConsole.Log ("object add "+id+" "+Time.unscaledTime+" "+x+" "+y+" "+angle);
#endif
		// set the initial information		

		x/=displayStretchFactor;
		newEvent.eventState = BBTouchEventState.Began;
		// set all the rest of the info
		updateEvent(newEvent,symbol,x,y,angle);
		newEvent.initialPosition = newEvent.normPosition;
		newEvent.lastScreenPosition = newEvent.screenPosition; // new event - set it identical to current position
		newEvent.lastAngle = newEvent.tuioAngle; // new event - set it identical to current angle
		newEvent.lastTouchTime = newEvent.touchTime; // new event - set it identical to current time

		// find camera in which the touch occured
		newEvent.camera=FindCamera(newEvent);
		// do *not* ignore marker event if outside all viewports
		//if(!newEvent.camera)
		//	return;
		
		// Add it to our active event dictionary so we can retrieve it based on its unique ID.
		// If events come in too fast (e.g., simultaneous mouse up and down, which can happen
		// if we release AND click between two Update()'s), we need to cleanup our event list properly.
		// (can this happen with TUIO/iPhone? Or is it guaranteed that IDs are always unique there?)
		if (activeEvents.ContainsKey(id)) {
			Debug.LogError("Catched an Up/Down clash! This shouldn't happen anymore...");
			// proper cleanup of events that spawned too fast - we mustn't lose the "up" event!
			BBTouchEvent anEvent = activeEvents[id];
			anEvent.eventState = BBTouchEventState.Ended;
			lock (eventQueueLock) eventQueue.Add(new BBTouchEvent(anEvent));
			activeEvents.Remove(id);
			if(currentFakeMarker==anEvent)
				currentFakeMarker=null;
		}
		activeEvents.Add(id, newEvent);
		// queue up a COPY for processing
		lock (eventQueueLock) eventQueue.Add(new BBTouchEvent(newEvent));
		return newEvent;
	}
	
	// marker equivalent to cursorMove
	public virtual void objectUpdate(long id, int symbol, float x, float y, float angle){
		if(!detectMarkers)
			return;
		
#if TOUCHDEBUG
		//DebugConsole.Log ("object update "+id+" "+Time.unscaledTime+" "+x+" "+y+" "+angle);
#endif
		// find the matching event object, set th state to 'moved'
		// and update it with the new position info
		if (!activeEvents.ContainsKey(id))
			return;
		BBTouchEvent anEvent = activeEvents[id];
		
		// debug
		if(anEvent.symbolID!=symbol)
			Debug.LogError("id/symbol mismatch in objectUpdate! id="+id+", expected "+symbol+", found "+anEvent.symbolID+"!");
			
		x/=displayStretchFactor;

		// ignore identical events - not required, but will prevent unnecessary touch callbacks (esp. for mouse)
		foreach(BBTouchEvent ev in activeEvents.Values)
		if(Mathf.Abs(ev.normPosition.x-x)<isSamePosDelta && Mathf.Abs(ev.normPosition.y-(1-y))<isSamePosDelta &&
			  Mathf.Abs(ev.tuioAngle-angle)<isSamePosDelta){
#if TOUCHDEBUG
//			DebugConsole.Log ("skip move doublette!");
#endif
			return;
		}
		
		updateEvent(anEvent,symbol,x,y,angle);
		if(anEvent.eventState==BBTouchEventState.Began){
			// add a COPY to the queue
			anEvent.eventState = BBTouchEventState.Moved;
			lock (eventQueueLock) eventQueue.Add(new BBTouchEvent(anEvent));
		}else{
			// replace event
			lock (eventQueueLock){
				// but only if it's not yet contained in our unprocessed events!
				if(!eventQueue.Contains(anEvent)) // this is O(n), but it can't be helped. luckily, the eventQueue is generally quite small
					eventQueue.Add(anEvent);
			}
		}
	}
	
	// marker equivalent to cursorUp
	public virtual void objectRemove(long id, int symbol, float x, float y, float angle){
		if(!detectMarkers)
			return;
		
#if TOUCHDEBUG
		DebugConsole.Log ("object remove "+id+" "+Time.unscaledTime+" "+x+" "+y+" "+angle);
#endif
		// find the matching event object, set the state to 'ended'
		// and remove it from our actives
		if (!activeEvents.ContainsKey(id))
			return;
		BBTouchEvent anEvent = activeEvents[id];
		
		// debug
		if(anEvent.symbolID!=symbol)
			Debug.LogError("id/symbol mismatch in objectRemove! id="+id+", expected "+symbol+", found "+anEvent.symbolID+"!");
			
		x/=displayStretchFactor;
		// ignore identical unprocessed move events
		// TODO: possible problem: this will get rid of the final move; there are no ending events in activeEvents -> no coord info!
		//       but if we don't do this, NGUI/iPhoneTouch-emulation coughs up
		lock (eventQueueLock){
			for(int i=0;i<eventQueue.Count;i++){
				if(eventQueue[i].eventState==BBTouchEventState.Moved &&
				   Mathf.Abs(eventQueue[i].normPosition.x-x)<isSamePosDelta && Mathf.Abs(eventQueue[i].normPosition.y-(1-y))<isSamePosDelta &&
				   Mathf.Abs(eventQueue[i].tuioAngle-angle)<isSamePosDelta){
#if TOUCHDEBUG
//					DebugConsole.Log ("skip move doublette on ending!");
#endif
					eventQueue.RemoveAt(i);
					i--;
				}
			}
			
			BBTouchEvent eventClone=new BBTouchEvent(activeEvents[id]);
			eventClone.eventState=BBTouchEventState.Ended;
			updateEvent(eventClone,symbol,x,y,angle);
			eventQueue.Add(eventClone);
		}
		activeEvents.Remove(id);		
		if(currentFakeMarker==anEvent)
			currentFakeMarker=null;
	}
	
	// Cursor down is for new touch events. we take the touch data (TUIO cursor object or iPhoneTouch) and convert it
	// into a touch event, and add it to our active list of events
	public virtual void cursorDown(long id, float x, float y){
#if TOUCHDEBUG
		DebugConsole.Log (Time.unscaledTime+" "+Time.frameCount+" cursor down "+id+" "+Time.unscaledTime+" "+x.ToString("G8")+" "+y.ToString("G8"));
#endif
		x/=displayStretchFactor;
		
		// discard events with identical positions
		// (this will filter out TUIO/Win7 double events)
		// (this will also filter out one type (a) of buggy Win7 mouse emulated touches which are incorrectly considered mouse events)
		// NOTE: all Began events in activeEvents *will* have the same Time.frameCount, so no need to check (also for polling==false? do we care?)
		foreach(BBTouchEvent ev in activeEvents.Values.ToList()){ // need to make a clone, as we want to remove stuff
			if(ev.eventState!=BBTouchEventState.Began) // we're only interested in events registered in this frame
				continue;
			if((ev.eventID>=STARTID_MOUSEMARKER) != (id>=STARTID_MOUSEMARKER)){ // one is mouse, the other isn't?
				if(id>=STARTID_MOUSEMARKER){
					// new one is mouse - we're fine, just discard it
#if TOUCHDEBUG
					DebugConsole.Log ("-----skip mouse-after-touch doublette! "+ev.eventState+", "+ev.eventID+" vs. "+id,"warning");
#endif
					return;
				}else{
					// new one is touch! need to remove already queued mouse doublette!
					// NOTE: we ignore any coord checks here! a regular MouseDown in the same frame as a regular touch will "never" really happen
#if TOUCHDEBUG
					DebugConsole.Log ("-----discarding already queued erroneous mouse doublette! "+ev.eventState+", "+ev.eventID+" vs. "+id,"warning");
#endif
					lock (eventQueueLock){
						for(int j=0;j<eventQueue.Count;j++){
							if(eventQueue[j].eventID==ev.eventID){
								eventQueue.RemoveAt(j);
								j--; // there should only be one, but make sure anyway!
							}
						}
					}
					activeEvents.Remove(ev.eventID);
				}
			}else{
				if(Mathf.Abs(ev.initialPosition.x-x)<isSamePosDelta && Mathf.Abs(ev.initialPosition.y-(1-y))<isSamePosDelta){
#if TOUCHDEBUG
					DebugConsole.Log ("-----skip down doublette! "+ev.eventState+", "+ev.eventID+" vs. "+id,"warning");
#endif
					return; // both are touch, or both are mouse. discard either one of them.
				}
			}
		}

		// discard events coming from mouse but identical to an actual touch
		// (this will also filter out the other type (b + some c) of buggy Win7 mouse emulated touches which are incorrectly considered mouse events (see below))
		// NOTE we can (and must, due to the nature of the bug) use a much higher tolerance here,
		// which is fine as we use very specialized checks (same frame, mouse after touch)
		// NOTE: the case that is *still* not being catched are multi-touches that were released in the same frame,
		// as we only store ONE previousDownFrame ATM, which is always the more recent one (and thus overwriting the older one).
		// For these, there will still be additional mouse events created, doubling their touch counterparts.
		// Care must be taken that these events do not mess up our move-doublette optimizations (see cursorUp()).
		// 20151119: NOTE this probably will/can no longer happen, but we keep it in anyways
		if(id>=STARTID_MOUSETOUCH && previousDownFrame==Time.frameCount){
			if(Mathf.Abs(previousDownX-x)<2e-2f && Mathf.Abs(previousDownY-y)<2e-2f){
#if TOUCHDEBUG
				DebugConsole.Log ("-----skip other down doublette! "+id,"warning");
#endif
				return;
			}
		}

		// first, make a new BBTouchEvent, tag it with the unique touch id
		BBTouchEvent newEvent = new BBTouchEvent(id); 
		// set the initial information		

		newEvent.eventState = BBTouchEventState.Began;
		// set all the rest of the info
		updateEvent(newEvent,x,y);
		newEvent.initialPosition = newEvent.normPosition;
		newEvent.lastScreenPosition = newEvent.screenPosition; // new event - set it identical to current position
		newEvent.lastTouchTime = newEvent.touchTime; // new event - set it identical to current time

		// find camera in which the touch occured
		newEvent.camera=FindCamera(newEvent);
		// ignore event if outside all viewports
		if(!newEvent.camera)
			return;

		// Store specs to identify incorrect mouse emulations
		// (=Windows bug where short taps are incorrectly identified as coming from the mouse, not the touchscreen).
		// NOTE there are three cases:
		// a) touch begin, mouse begin, touch end -> this case will be catched by the regular doublette test (if the pos diff is small enough!)
		// b) touch begin, touch end, mouse begin -> this case needs special treatment, as the regular touch has already finished
		// c) touch begin (in an older frame), touch end, mouse begin -> this case needs special treatment, as the regular touch has already finished
		if(id<STARTID_MOUSETOUCH){
			previousDownFrame=Time.frameCount;
			previousDownX=x;
			previousDownY=y;
		}

		// Add it to our active event dictionary so we can retrieve it based on its unique ID.
		// If events come in too fast (e.g., simultaneous mouse up and down, which can happen
		// if we release AND click between two Update()'s), we need to cleanup our event list properly.
		// (can this happen with TUIO/iPhone? Or is it guaranteed that IDs are always unique there?)
		if (activeEvents.ContainsKey(id)) {
			Debug.LogError("Catched an Up/Down clash! This shouldn't happen anymore...");
			// proper cleanup of events that spawned too fast - we mustn't lose the "up" event!
			BBTouchEvent anEvent = activeEvents[id];
			anEvent.eventState = BBTouchEventState.Ended;
			lock (eventQueueLock) eventQueue.Add(new BBTouchEvent(anEvent));
			activeEvents.Remove(id);
		}
		activeEvents.Add(id, newEvent);
		// queue up a COPY for processing
		lock (eventQueueLock) eventQueue.Add(new BBTouchEvent(newEvent));
	}
	
	private Camera FindCamera(BBTouchEvent newEvent){
		return FindCamera(newEvent,null);
	}

	private Camera FindCamera(BBTouchEvent newEvent, Camera eventCam){
		Camera topmostCam=null;
		float targetDepth=0; // initial value is irrelevant
		foreach(Camera cam in Camera.allCameras){
			if((!topmostCam||cam.depth>targetDepth)&&									// camera is in front
			   		(!eventCam ||                                            			// first attempt to find camera
						cam.depth<eventCam.depth &&										// cam is deeper than original cam...
			 			(eventCam.clearFlags==CameraClearFlags.Depth ||					// ...and original cam doesn't clear (i.e., is transparent)
			 				eventCam.clearFlags==CameraClearFlags.Nothing))&&
					!cam.targetTexture&&												// not a RenderedTexture
					(
#if NMY_HAS_STEREO3D
						cam.cullingMask==0 &&												// camera doesn't render anything *and*...
							cam.GetComponent<AbstractStereoRendering>()!=null &&
							(cam.GetComponent<AbstractStereoRendering>().curMask&(layerTouchMask|layerControlMask))!=0 || // ...an active stereo script uses a "disabled" main cam as a touch event catcher
#endif
						(cam.cullingMask&(layerTouchMask|layerControlMask))!=0)&&		// ...*or* we're in a touchable layer ourselves
					cam.enabled){														// camera is enabled
				// within viewport of current camera?
				Vector3 vpPoint = cam.ScreenToViewportPoint(newEvent.screenPosition);
				if(vpPoint.x>=0&&vpPoint.x<=1&&vpPoint.y>=0&&vpPoint.y<=1){
					// yes. camera is a candidate
					topmostCam=cam;
					targetDepth=cam.depth;
				}
			}
		}
		return topmostCam;
	}

	public virtual  void cursorMove(long id, float x, float y){
#if TOUCHDEBUG
		// stage 0: print *all* even non-existing mouse-emulation events (=ctrl, shift, ...)
		//DebugConsole.Log (Time.unscaledTime+" "+Time.frameCount+" cursor move "+id+" "+Time.unscaledTime+" "+x.ToString("G8")+" "+y.ToString("G8"));
#endif
		// find the matching event object, set th state to 'moved'
		// and update it with the new position info
		if (!activeEvents.ContainsKey(id))
			return;
#if TOUCHDEBUG
		// stage 1: print every move event for an actual touch event
		//DebugConsole.Log (Time.unscaledTime+" "+Time.frameCount+" cursor move "+id+" "+Time.unscaledTime+" "+x.ToString("G8")+" "+y.ToString("G8"));
#endif

		x/=displayStretchFactor;
		
		// ignore identical events - not required, but will prevent unnecessary touch callbacks (esp. for mouse)
		foreach(BBTouchEvent ev in activeEvents.Values)
			if(Mathf.Abs(ev.normPosition.x-x)<isSamePosDelta && Mathf.Abs(ev.normPosition.y-(1-y))<isSamePosDelta){
#if TOUCHDEBUG
//				DebugConsole.Log ("skip move doublette!");
#endif
				return;
			}
#if TOUCHDEBUG
		// stage 2: print move events only if position changed
		//DebugConsole.Log (Time.unscaledTime+" "+Time.frameCount+" cursor move "+id+" "+Time.unscaledTime+" "+x.ToString("G8")+" "+y.ToString("G8"));
#endif

		BBTouchEvent anEvent = activeEvents[id];
		updateEvent(anEvent,x,y);
		if(anEvent.eventState==BBTouchEventState.Began){
			// add a COPY to the queue
			anEvent.eventState = BBTouchEventState.Moved;
			lock (eventQueueLock) eventQueue.Add(new BBTouchEvent(anEvent));
		}else{
			// replace event
			lock (eventQueueLock){
				// but only if it's not yet contained in our unprocessed events!
				if(!eventQueue.Contains(anEvent)) // this is O(n), but it can't be helped. luckily, the eventQueue is generally quite small
					eventQueue.Add(anEvent);
			}
		}
	}
	
	public virtual  void cursorUp(long id, float x, float y){
#if TOUCHDEBUG
//		DebugConsole.Log (Time.unscaledTime+" "+Time.frameCount+" cursor up "+id+" "+Time.unscaledTime+" "+x.ToString("G8")+" "+y.ToString("G8"));
#endif
		// find the matching event object, set the state to 'ended'
		// and remove it from our actives
		if (!activeEvents.ContainsKey(id))
			return;
#if TOUCHDEBUG
		DebugConsole.Log (Time.unscaledTime+" "+Time.frameCount+" cursor up "+id+" "+Time.unscaledTime+" "+x.ToString("G8")+" "+y.ToString("G8"));
#endif

		x/=displayStretchFactor;

		// Store specs to identify incorrect mouse emulations
		// (=Windows bug where short taps are incorrectly identified as coming from the mouse, not the touchscreen).
		// We need to update our tmp values even though this is an Up event, to catch case c) described in cursorDown().
		if(id<STARTID_MOUSETOUCH){
			previousDownFrame=Time.frameCount;
			previousDownX=x;
			previousDownY=y;
		}

		// Ignore identical unprocessed move events
		// TODO: possible problem: this will get rid of the final move; there are no ending events in activeEvents -> no coord info!
		//       but if we don't do this, NGUI/iPhoneTouch-emulation coughs up
		// NOTE: We must not remove "Ended" events here, as this would create Zombie touches if an
		// (incorrectly detected) Begin-Ended "Mouse" event was generated in the same frame.
		lock (eventQueueLock){
			for(int i=0;i<eventQueue.Count;i++){
				if(eventQueue[i].eventState==BBTouchEventState.Moved &&
						Mathf.Abs(eventQueue[i].normPosition.x-x)<isSamePosDelta && Mathf.Abs(eventQueue[i].normPosition.y-(1-y))<isSamePosDelta){
#if TOUCHDEBUG
					DebugConsole.Log ("skip move doublette on ending! "+eventQueue[i].eventState+", "+eventQueue[i].eventID+" vs. "+id);
#endif
					eventQueue.RemoveAt(i);
					i--;
				}
			}
		
			BBTouchEvent eventClone=new BBTouchEvent(activeEvents[id]);
			eventClone.eventState=BBTouchEventState.Ended;
			updateEvent(eventClone,x,y);
			eventQueue.Add(eventClone);
		}
		activeEvents.Remove(id);		
	}
	
	public virtual List<BBCursorEvent> getAndClearCursorEvents(){
		// will be handled in the derived class
		return new List<BBCursorEvent>();
	}

	public virtual List<BBObjectEvent> getAndClearObjectEvents(){
		// will be handled in the derived class
		return new List<BBObjectEvent>();
	}

	// this is only called if usePolling==true
	public void processEvents()
	{
		List<BBCursorEvent> events = getAndClearCursorEvents();
		// go through the events and dispatch
		foreach (BBCursorEvent cursorEvent in events) {
			if (cursorEvent.state == BBCursorState.Add)
				cursorDown(cursorEvent.id,cursorEvent.x,cursorEvent.y);
			else if (cursorEvent.state == BBCursorState.Update)
				cursorMove(cursorEvent.id,cursorEvent.x,cursorEvent.y);
			else if (cursorEvent.state == BBCursorState.Remove)
				cursorUp(cursorEvent.id,cursorEvent.x,cursorEvent.y);
		}
		
		List<BBObjectEvent> oevents = getAndClearObjectEvents();
		// go through the events and dispatch
		foreach (BBObjectEvent objectEvent in oevents) {
			if (objectEvent.state == BBCursorState.Add)
				objectAdd(objectEvent.id,objectEvent.symbol,objectEvent.x,objectEvent.y,objectEvent.a);
			else if (objectEvent.state == BBCursorState.Update)
				objectUpdate(objectEvent.id,objectEvent.symbol,objectEvent.x,objectEvent.y,objectEvent.a);
			else if (objectEvent.state == BBCursorState.Remove)
				objectRemove(objectEvent.id,objectEvent.symbol,objectEvent.x,objectEvent.y,objectEvent.a);
		}
		
		finishFrame();
	}

	public virtual void finishFrame() {
		// this is called when the TUIO fseq message comes through, and it is
		// the end of this cycle.
		if(usePolling){
#if UNITY_FLASH
			foreach (long flashFix in activeEvents.Keys) {
				BBTouchEvent touch=activeEvents[flashFix];
#else
			foreach (BBTouchEvent touch in activeEvents.Values) {
#endif
				// any unchanging events need to have their screen position updated
				// any changing events need to be set to unchanged
				// for the next round
				if (touch.didChange) {
					touch.didChange = false;
				} else {
					touch.lastScreenPosition = touch.screenPosition;
				}
			}
		}
	}

	// compare two symbol identifers, with bit tolerance
	// WARNING: one is an actual ID, the other is an index into markerMapping!
	public bool IsSameID(int symbolID, int symbolIndex){
		return NMY.StaticUtils.IsSameID(symbolID,GetMarkerIDFromIndex(symbolIndex),markersCurrentBitTolerance);
	}
		
	public virtual int GetNumberOfActiveMarkers(){
		if(!detectMarkers)
			return 0;
		
		int numMarkers=0;
		foreach(BBTouchEvent anEvent in activeEvents.Values)
			if(anEvent.symbolID>=0)
				numMarkers++;
		
		return numMarkers;
	}
	
	// This can be queried to test the current state of a marker.
	// This is useful e.g. if an object registers with us while the marker is already active/in place.
    // (that particular case, however, will already be taken care of by this class)
	public virtual BBTouchEvent GetMarkerEvent(int symbolIndex){
		if(!detectMarkers)
			return null;
		
		foreach(BBTouchEvent anEvent in activeEvents.Values)
			//if(anEvent.symbolID==GetMarkerIDFromIndex(symbolIndex))
			if(IsSameID(anEvent.symbolID,symbolIndex)) // with error tolerance!
				return new BBTouchEvent(anEvent); // return a COPY of the event, to stay separate from the internal, asynchronous events of the TouchManager
		
		return null;
	}
	
	// map into markerMapping[].
	// if not in range, find next possible index respecting markersCurrentBitTolerance, and add it to markerMapping[]
	public int GetMarkerIDFromIndex(int symbolIndex){
		// return valid ID
		if(symbolIndex>=0 && symbolIndex<markerMapping.Count)
			return markerMapping[symbolIndex];
		
		// none found: find new index starting from 0, and respecting markersCurrentBitTolerance
		int newID=0;
		while(markerMapping.Count<=symbolIndex){
			for(int i=0;i<markerMapping.Count;i++){
				int hammingDistance=NMY.StaticUtils.GetHammingDistance(newID,markerMapping[i]);
				int allowedTolerance=(hammingDistance-1)>>1;
				if(allowedTolerance<markersCurrentBitTolerance){
					newID++;
					i=-1; // restart search
				}
			}
			markerMapping.Add(newID);
			markerIDtoIndex[newID]=markerMapping.Count-1;
		}
		return newID;
	}
		
	// NOTE: returns global var fakeMarkerIndex as a conveniency
	protected int GetNextFakeMarkerID(){
		// find next valid markerID
		int origIndex;
		fakeMarkerIndex=0;
		do{
			origIndex=fakeMarkerIndex;
			foreach(BBTouchEvent ev in activeEvents.Values){
				if(GetMarkerIDFromIndex(fakeMarkerIndex)==ev.symbolID){
					fakeMarkerIndex++;
					break;
				}
			}
		}while(origIndex!=fakeMarkerIndex);
		return fakeMarkerIndex;
	}

	// NOTE: Input.GetMouseButtonDown() does not work consistently when queried in FixedUpdate()
	protected virtual void Update()
	{
		if(oldScreenHeight!=Screen.height || oldScreenWidth!= Screen.width){
#if UNITY_EDITOR
			// WK 20130722 (maybe they fix this some day?)
			// this is a weird hack to determine whether the Game view is currently inactive
			// (i.e., closed or in a background tab)
			// because if it is, for some reason the Screen size resets to 640x480, and therefore messes
			// with all scripts checking that...
			if(Screen.width!=640 || Screen.height!=480 || Screen.currentResolution.width==640 && Screen.currentResolution.height==480)
#endif
			{
				// whoa. somebody changed our screen resolution (e.g., in the Editor)
				// update the event info accordingly.
				foreach (BBTouchEvent anEvent in activeEvents.Values) {
					if(anEvent.symbolID>=0) // ignore touches, we don't care
						objectUpdate(anEvent.eventID,anEvent.symbolID,anEvent.normPosition.x,1-anEvent.normPosition.y,anEvent.tuioAngle);
				}
				oldScreenHeight=Screen.height;
				oldScreenWidth=Screen.width;
			}
		}
			
		//////////////////////////////////////////////////
		// this is all about making fake events from the mouse for testing
		////////////////////////////////////////////////////

// force mouse emulation in both editor and webplayer
#if (!(UNITY_EDITOR||UNITY_WEBPLAYER||UNITY_WEBGL))
		if(emulateMouseEvents)
#endif
		{
			bool processClicks=false;
			bool cleanupButtonRelease=false;
			// complicated test whether we need to do something
			if (Input.GetMouseButtonDown(0)||Input.GetMouseButtonUp(0)||Input.GetMouseButton(0)||Input.GetMouseButtonDown(1)){
				// this is the normal case
				processClicks=true;
			}else{
				// For very low frame rates, we might lose vital button state changes, since Unity/the Input class
				// just returns "yes, this buttons has been clicked during the previous frame".
				// However, this will fail if we do a double-click within a frame (even a 0-1-0 will fail),
				// since then we definitely lose all but one of the release events.
				// To overcome this, we test here whether we still have active events while no button is clicked - which
				// is not valid for normal touches - and simply issue an additional cursorUp() in that case.
#if UNITY_FLASH
				foreach(long flashFix in activeEvents.Keys){
					BBTouchEvent anEvent=activeEvents[flashFix];
#else
				foreach(BBTouchEvent anEvent in activeEvents.Values){
#endif
					if(anEvent.eventState!=BBTouchEventState.Ended && anEvent.eventID>=STARTID_MOUSETOUCH && anEvent.symbolID==-1){ // this should always be true for mouse-only events, but we test anyway
						cleanupButtonRelease=true;
						break;
					}
				}
			}

#if UNITY_STANDALONE_WIN
			uint extra = GetMessageExtraInfo();
			bool isPen = ( ( extra &  0xFFFFFF00 ) == 0xFF515700 );
			// TODO: WARNING! ELO "true" touches will be completely ignored on Win7 in the Editor with this,
			// due to stupid OS touch handling and TouchScript not sending touches in the Editor at all
			// (only via OS single touch mouse emulation).
			if(isPen)
				processClicks=false; // ignore mouse events that are actually emulated from touch events
			// However, in plenty of cases, Windows still incorrectly reports regular touch echoes as isPen==false
			// for example, EVERY cursorUp, sometimes Input.GetMouseButton(0) continuations,
			// and especially single-frame clashes with GetMouseButtonDown(0) *and* GetMouseButtonUp(0) set.
			// By now, half of the code in BBInputDelegate is just there trying to catch and workaround these bugs... (see esp. comments in cursorDown())
#endif
					
			if (processClicks||cleanupButtonRelease){
				Vector3 fakepos = new Vector3(Input.mousePosition.x/Screen.width,Input.mousePosition.y/Screen.height,Input.mousePosition.z);
				bool downIsNewID=false;

				// Framerate too low, clicks too fast? Button can be both clicked and released...
				if (processClicks && Input.GetMouseButtonUp(0) && Input.GetMouseButtonDown(0)){
					// now we need to determine whether this is a 1-0 event (click-release in quick succession),
					// or 0-1 (release-click while button is pressed)
	
					// if it's 0-1, the current ID (plus key modifier variants) will be already in our event list
					if(activeEvents.ContainsKey(fakeEventID)||
					   activeEvents.ContainsKey(fakeEventID+1)||
					   activeEvents.ContainsKey(fakeEventID+2)||
					   activeEvents.ContainsKey(fakeEventID+3))
							downIsNewID=true;
				}
	
				// regular click
				if (processClicks && Input.GetMouseButtonDown(0) && !downIsNewID) {
					if(detectMarkers){
						if(Input.GetKey(KeyCode.RightShift) || Input.GetKey(KeyCode.RightControl)){
							// did we hit a marker?
							currentFakeMarker=null;
							foreach(BBTouchEvent ev in activeEvents.Values){
								Vector2 dist=new Vector2((ev.normPosition.x-fakepos.x)*Screen.width/(float)Screen.height,ev.normPosition.y-fakepos.y);
								if(ev.symbolID>=0 && dist.x*dist.x+dist.y*dist.y<fakeMarkerSqrRadius){
									currentFakeMarker=ev;
									break;
								}
							}
							// no marker at mouse position? create new!
							if(currentFakeMarker==null){
								float newAngle=0;
								if(Input.GetKey(KeyCode.RightControl))
									newAngle=fakepos.x*TAU;
								GetNextFakeMarkerID(); // increment fakeMarkerIndex
								currentFakeMarker=this.objectAdd(STARTID_MOUSEMARKER+fakeMarkerIndex,GetMarkerIDFromIndex(fakeMarkerIndex),fakepos.x,1.0f-fakepos.y,newAngle);
								clickoffset=new Vector3(0,0,0);
								clickangle=0;
							}else{
								clickoffset=fakepos-new Vector3(currentFakeMarker.normPosition.x,currentFakeMarker.normPosition.y,0);
								clickangle=fakepos.x*TAU-currentFakeMarker.tuioAngle;
							}
						}
					}
							
					// regular "touch", not a marker
					if(currentFakeMarker==null){
#if !UNITY_FLASH && !UNITY_WEBPLAYER
						fakeposCenter=fakepos+new Vector3(0.05f,0,0);
#endif
						fakeposCenterFar=fakepos+new Vector3(0.15f,0,0);

						// 20160104 TODO: originally, the main event (for cursorDown/Move/Up resp.) was only spawned according this line:
						//if(!NMY.StaticUtils.isWin8) // ignore buggy Win8 emulation for main click
						// => is the main fix simply removing that line, and everything else (TUIO, TUIO->iPhoneTouch, TouchScript.dll's) can stay the same?
						//    probably not, as I seem to remember that uGUI really conflicted with TouchScript.dll, and only one of them could get the main event, but not both
						//    => the hope is, that it is/would have been enough to remove all TouchScript.dll code, but keep the TUIO->iPhoneTouch stuff, and it still works as it does now...
						this.cursorDown(fakeEventID+1,fakepos.x,1.0f-fakepos.y);
#if !UNITY_FLASH && !UNITY_WEBPLAYER
						// ctrl: point symmetric to center of both touches
						if (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl)&&!detectMarkers) {
							this.cursorDown(fakeEventID,2*fakeposCenter.x-fakepos.x,1.0f - (2*fakeposCenter.y-fakepos.y));
						}
#endif
						// shift: point symmetric to center of both touches, but at greater distance (for zoom-out)
						if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift)&&!detectMarkers) {
							this.cursorDown(fakeEventID+3,2*fakeposCenterFar.x-fakepos.x,1.0f - (2*fakeposCenterFar.y-fakepos.y));
						}
						// alt: synchronous with first click
#if UNITY_FLASH || UNITY_WEBPLAYER
						if (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl)&&!detectMarkers) {
#else
						if (Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt)&&!detectMarkers) {
#endif
							this.cursorDown(fakeEventID+2,fakepos.x + 0.05f,1.0f - fakepos.y);
						}
					}
				}

				// regular release - ignore for markers
				if (processClicks && Input.GetMouseButtonUp(0) || cleanupButtonRelease) { // special case: with no button clicked, cleanup obsolete events
					// only if not a marker (don't care about possible clashes combining left/right shift (= simultaneous touch/marker...)						
					if(currentFakeMarker==null){
						this.cursorUp(fakeEventID + 1,fakepos.x,1.0f - fakepos.y);
						// just kill any secondary mouse events here too
#if !UNITY_FLASH && !UNITY_WEBPLAYER
						this.cursorUp(fakeEventID,2*fakeposCenter.x-fakepos.x,1.0f - (2*fakeposCenter.y-fakepos.y));		
#endif
						this.cursorUp(fakeEventID + 3,2*fakeposCenterFar.x-fakepos.x,1.0f - (2*fakeposCenterFar.y-fakepos.y));
						this.cursorUp(fakeEventID + 2,fakepos.x + 0.05f,1.0f - fakepos.y);
									
						fakeEventID += 4; // current event is finished. increment to provide new ID for following events
					}
					currentFakeMarker=null;
				}
							
				// regular right-click
				// NOTE: not yet low-framerate safe!
				if (processClicks && Input.GetMouseButtonDown(1) && !Input.GetMouseButton(0)){
					if(detectMarkers){
						// did we hit a marker?
						currentFakeMarker=null;
						foreach(BBTouchEvent ev in activeEvents.Values){
							Vector2 dist=new Vector2((ev.normPosition.x-fakepos.x)*Screen.width/(float)Screen.height,ev.normPosition.y-fakepos.y);
							if(ev.symbolID>=0 && dist.x*dist.x+dist.y*dist.y<fakeMarkerSqrRadius){
								currentFakeMarker=ev;
								break;
							}
						}
						if(currentFakeMarker!=null){
							// remove marker
							Vector3 oldPos=currentFakeMarker.normPosition;
							float oldAngle=currentFakeMarker.tuioAngle;
							this.objectRemove(currentFakeMarker.eventID,currentFakeMarker.symbolID,fakepos.x,1.0f-fakepos.y,currentFakeMarker.tuioAngle);
							if(Input.GetKey(KeyCode.RightShift) || Input.GetKey(KeyCode.RightControl)){
								// increment ID, replace marker
								GetNextFakeMarkerID(); // increment fakeMarkerIndex
								currentFakeMarker=this.objectAdd(STARTID_MOUSEMARKER+fakeMarkerIndex,GetMarkerIDFromIndex(fakeMarkerIndex),oldPos.x,1.0f-oldPos.y,oldAngle);
							}
						}
					}
				}

				// special case 0-1: process this as a new event (=new ID) *after* the release has been taken care of
				if (processClicks && Input.GetMouseButtonDown(0) && downIsNewID) {
					if(detectMarkers){
						// no marker at mouse position? create new!
						if(Input.GetKey(KeyCode.RightShift) || Input.GetKey(KeyCode.RightControl)){
							// did we hit a marker?
							currentFakeMarker=null;
							foreach(BBTouchEvent ev in activeEvents.Values){
								Vector2 dist=new Vector2((ev.normPosition.x-fakepos.x)*Screen.width/(float)Screen.height,ev.normPosition.y-fakepos.y);
								if(ev.symbolID>=0 && dist.x*dist.x+dist.y*dist.y<fakeMarkerSqrRadius){
									currentFakeMarker=ev;
									break;
								}
							}
							if(currentFakeMarker==null){
								float newAngle=0;
								if(Input.GetKey(KeyCode.RightControl))
									newAngle=fakepos.x*TAU;
								GetNextFakeMarkerID(); // increment fakeMarkerIndex
								currentFakeMarker=this.objectAdd(STARTID_MOUSEMARKER+fakeMarkerIndex,GetMarkerIDFromIndex(fakeMarkerIndex),fakepos.x,1.0f-fakepos.y,newAngle);
								clickoffset=new Vector3(0,0,0);
								clickangle=0;
							}else{
								clickoffset=fakepos-new Vector3(currentFakeMarker.normPosition.x,currentFakeMarker.normPosition.y,0);
								clickangle=currentFakeMarker.tuioAngle;
							}
						}
					}
								
					if(currentFakeMarker==null){
#if !UNITY_FLASH && !UNITY_WEBPLAYER
						fakeposCenter=fakepos+new Vector3(0.05f,0,0);
#endif
						fakeposCenterFar=fakepos+new Vector3(0.15f,0,0);
						// no mod keys and not a marker
						if(!detectMarkers || (!Input.GetKey(KeyCode.RightShift) && !Input.GetKey(KeyCode.RightControl))){
							this.cursorDown(fakeEventID+1,fakepos.x,1.0f-fakepos.y);
						}
#if !UNITY_FLASH && !UNITY_WEBPLAYER
						if (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl)&&!detectMarkers) {
							this.cursorDown(fakeEventID,2*fakeposCenter.x-fakepos.x,1.0f - (2*fakeposCenter.y-fakepos.y));
						}
#endif
						if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift)&&!detectMarkers) {
							this.cursorDown(fakeEventID+3,2*fakeposCenterFar.x-fakepos.x,1.0f - (2*fakeposCenterFar.y-fakepos.y));
						}
#if UNITY_FLASH || UNITY_WEBPLAYER
						if (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl)&&!detectMarkers) {
#else
						if (Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt)&&!detectMarkers) {
#endif
							this.cursorDown(fakeEventID+2,fakepos.x + 0.05f,1.0f - fakepos.y);
						}
					}
				}

				// regular drag
				if (processClicks && Input.GetMouseButton(0) && !Input.GetMouseButtonDown(0)) {
					if(detectMarkers){
						if(currentFakeMarker!=null){
							if (Input.GetKey(KeyCode.RightShift)){
								this.objectUpdate(currentFakeMarker.eventID,currentFakeMarker.symbolID,fakepos.x-clickoffset.x,1.0f-(fakepos.y-clickoffset.y),currentFakeMarker.tuioAngle);
							}
							if (Input.GetKey(KeyCode.RightControl)){
								float repeat=fakepos.x-clickangle/TAU;
								float newAngle=(repeat-(int)repeat)*TAU;
								if(newAngle<0) newAngle+=TAU;
								this.objectUpdate(currentFakeMarker.eventID,currentFakeMarker.symbolID,currentFakeMarker.normPosition.x,1.0f-currentFakeMarker.normPosition.y,newAngle);
							}
						}
					}
										
					if(currentFakeMarker==null){
						this.cursorMove(fakeEventID + 1,fakepos.x,1.0f - fakepos.y);
#if !UNITY_FLASH && !UNITY_WEBPLAYER
						this.cursorMove(fakeEventID,2*fakeposCenter.x-fakepos.x,1.0f - (2*fakeposCenter.y-fakepos.y));		
#endif
						this.cursorMove(fakeEventID+3,2*fakeposCenterFar.x-fakepos.x,1.0f - (2*fakeposCenterFar.y-fakepos.y));		
						// alt: synchronous with first click
						this.cursorMove(fakeEventID+2,fakepos.x + 0.05f,1.0f - fakepos.y);
					}
				}
			}
		}
						
		if(usePolling)
			processEvents(); // calls finishFrame() itself
		else
			finishFrame();
						
		if(sendTUIO)
			SendTouchesViaTUIO();
						
		//////////////////////////////////////////////////
		// from here is the 'real' meat of this method, where we distro all the events
		// the reason we dont distro them right wehn we get them is that they need to
		// be send during an update or the scene model will be in an unknown state
		// and strange things sometimes happen (like the raycasts dont work quite right)
		////////////////////////////////////////////////////

		// now distro the events
		RaycastHit hit = new RaycastHit();
		lock (eventQueueLock) {
			foreach (BBTouchEvent anEvent in eventQueue) {
				if(anEvent.symbolID>=0){
					// send messages to all registered objects with matching ID (respecting current bit tolerance)
					foreach(KeyValuePair<int,List<Component>> symbol in allMarkers){
						if(NMY.StaticUtils.IsSameID(symbol.Key,anEvent.symbolID,markersCurrentBitTolerance)){
							List<Component> registeredComponents=symbol.Value;
							foreach(Component obj in registeredComponents){
								switch(anEvent.eventState){
									case BBTouchEventState.Began:	obj.SendMessage("MarkerAdded",anEvent,SendMessageOptions.DontRequireReceiver);
																	break;
									case BBTouchEventState.Moved:	obj.SendMessage("MarkerMoved",anEvent,SendMessageOptions.DontRequireReceiver);
																	break;
									case BBTouchEventState.Ended:	obj.SendMessage("MarkerRemoved",anEvent,SendMessageOptions.DontRequireReceiver);
																	break;
									default:						break; // state "Stationary" is unused anyway
								}
							}
						}
					}
				}else{
					// we ignore marker events in this section - different paradigm
					Camera eventCam=anEvent.camera;
					if (eventCam){
						Ray screenRay=eventCam.ScreenPointToRay(anEvent.screenPosition); // slight performance optimization: precompute
						
						// we are going to ray cast into the scene and see what we hit.
						// first the 3D controlObjects,
						// then the 3D touchableObjects
						bool hitSomething=false;
						GameObject hitObject=null;
						do{
							int camMask=eventCam.cullingMask;
#if NMY_HAS_STEREO3D
							if(camMask==0){
								AbstractStereoRendering asr=eventCam.GetComponent<AbstractStereoRendering>();
								if(asr)
									camMask=asr.curMask;
							}
#endif

							// 3D elements, controlObjects layer
							if (Physics.Raycast(screenRay,out hit, Mathf.Infinity,layerControlMask&camMask)){
								hitObject=hit.transform.gameObject;
								hitSomething=true;
							}

							// 3D elements, touchableObjects layer
							if(!hitSomething)
								if (Physics.Raycast(screenRay,out hit, Mathf.Infinity,layerTouchMask&camMask)){
									hitObject=hit.transform.gameObject;
									hitSomething=true;
								}
							
							// still nothing? try next camera, we might be transparent!
							if(!hitSomething){
								eventCam=FindCamera(anEvent,eventCam);
								if(eventCam)
									screenRay=eventCam.ScreenPointToRay(anEvent.screenPosition);
							}
						}while(!hitSomething && eventCam!=null);

						if (hitSomething){
							// found hit on possibly different cam? update!
							anEvent.camera=eventCam;
							// if we just began, then we assign our hit object right away
							if (anEvent.eventState == BBTouchEventState.Began) {
								foreach (BBTouchEvent updateEvent in eventQueue) {
									if(updateEvent.eventID==anEvent.eventID){
										updateEvent.camera = eventCam;			
										updateEvent.hitObject = hitObject;				
										updateEvent.lastRayCastHitPosition = updateEvent.rayCastHitPosition;
										updateEvent.rayCastHitPosition = hit.point;				
										updateEvent.rayCastClosestHitPosition = hit.point;			
										updateEvent.colliderClosestHitPosition = hit.point;	
										updateEvent.textureCoordHitPosition = hit.textureCoord;
									}
								}
	#if UNITY_FLASH
								foreach (long flashFix in activeEvents.Keys) {
									BBTouchEvent updateEvent=activeEvents[flashFix];
	#else
								foreach (BBTouchEvent updateEvent in activeEvents.Values) {
	#endif
									if(updateEvent.eventID==anEvent.eventID){
										updateEvent.camera = eventCam;			
										updateEvent.hitObject = hitObject;				
										updateEvent.lastRayCastHitPosition = updateEvent.rayCastHitPosition;
										updateEvent.rayCastHitPosition = hit.point;				
										updateEvent.rayCastClosestHitPosition = hit.point;			
										updateEvent.colliderClosestHitPosition = hit.point;		
										updateEvent.textureCoordHitPosition = hit.textureCoord;
									}
								}
							}				
							// if we have been around, we should already have a hit object, so
							// we need to check to see if it is the same one.
							// if it is, then we update the hit position,
							// if it is not the same object, we leave the hit position alone (in case you
							// drag off of an object, the hit pos will stay at the last point on the obj
							if (hitObject == anEvent.hitObject) {
								anEvent.lastRayCastHitPosition = anEvent.rayCastHitPosition;
								anEvent.rayCastHitPosition = hit.point;				
								anEvent.rayCastClosestHitPosition = hit.point;
								anEvent.colliderClosestHitPosition = hit.point;
								anEvent.textureCoordHitPosition = hit.textureCoord;
							} else
								hitSomething=false; // force computation of approximate hit below
						}
						
						// Do we have an active event where the touchpoint has left the object?
						if(!hitSomething&&anEvent.hitObject&&anEvent.eventState!=BBTouchEventState.Began){
							// compute an approximate position outside of the object:						
							// find point along ray, using previous hit position as distance estimate
							anEvent.rayCastClosestHitPosition=screenRay.origin+screenRay.direction*Vector3.Distance(anEvent.colliderClosestHitPosition,screenRay.origin);
							if(anEvent.hitObject.GetComponent<Collider>()){
								// NOTE: we will not get here if the touch script object itself has no collider (i.e., only its children)
								// get closest point on collider bbox to that ray point
								anEvent.colliderClosestHitPosition=anEvent.hitObject.GetComponent<Collider>().ClosestPointOnBounds(anEvent.rayCastClosestHitPosition);
							}
						}
					}
	
		        	if (anEvent.hitObject) {
		        		// if we have a hit object, we blindly send it the queueEvent message
		        		// if it is not a touchable objec,then this message will be ignored
						anEvent.hitObject.SendMessageUpwards("queueEvent",anEvent,SendMessageOptions.DontRequireReceiver);
						if (anEvent.eventState == BBTouchEventState.Began) {
							// if this is a new event, we need to notify our objects NOW!
							anEvent.hitObject.SendMessageUpwards("finishFrame",SendMessageOptions.DontRequireReceiver);
						}else{
							// otherwise we add this object to our notification set
							if (!objectsToNotify.Contains(anEvent.hitObject)) objectsToNotify.Add(anEvent.hitObject);
						}
					}
				} // close else symbolID>=0
			}			
			eventQueue.Clear();	
		}

		// each object needs to be told that the frame is over and it can 
		// process all the new events
		foreach (GameObject objectToNotify in objectsToNotify){
			objectToNotify.SendMessageUpwards("finishFrame",SendMessageOptions.DontRequireReceiver);
		}	
		
		// clear our queues for the next set
		objectsToNotify.Clear();
	}
		
	void SendTouchesViaTUIO(){
		if(oscTransmitter==null){
			sourceName="NMYTUIOServer@"+(Dns.GetHostEntry(Dns.GetHostName())).AddressList[0].ToString();
			if(remoteIP=="")
				oscTransmitter = new OSC.NET.OSCTransmitter(remotePort); // local IP
			else
				oscTransmitter = new OSC.NET.OSCTransmitter(remoteIP,remotePort);
		}

		OSC.NET.OSCBundle bundle = new OSC.NET.OSCBundle((long)(Time.realtimeSinceStartup*1000)); // timestamps are ignored...
		OSC.NET.OSCMessage msg = new OSC.NET.OSCMessage("/tuio/2Dcur");
		msg.Append("source");
		msg.Append(sourceName);
		bundle.Append(msg);

		msg = new OSC.NET.OSCMessage("/tuio/2Dcur");
		msg.Append("alive");
		foreach(BBTouchEvent touch in activeEvents.Values)
			msg.Append((int)touch.eventID);

		bundle.Append(msg);

		foreach(BBTouchEvent touch in activeEvents.Values){
			msg = new OSC.NET.OSCMessage("/tuio/2Dcur");
			msg.Append("set");
			msg.Append((int)touch.eventID);
			msg.Append(touch.normPosition.x);
			msg.Append(1-touch.normPosition.y);
			msg.Append(0f);
			msg.Append(0f);
			msg.Append(0f);
			bundle.Append(msg);
		}

		msg = new OSC.NET.OSCMessage("/tuio/2Dcur");
		msg.Append("fseq");
		msg.Append(currentTUIOSeqID++);
		bundle.Append(msg);

		oscTransmitter.Send(bundle);
	}
}

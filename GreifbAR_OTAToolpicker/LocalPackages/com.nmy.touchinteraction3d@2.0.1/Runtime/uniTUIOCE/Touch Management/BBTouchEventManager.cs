using UnityEngine;
using UnityEngine.SceneManagement;
using System;
using System.Collections;
using TUIO;
using System.Collections.Generic;
using NMY;

// Manager for TUIO events

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


// NOTE: config flags should be passed through from BBTouchManagerStarter, since BBTouchEventManager will be created on-the-fly.
public class BBTouchEventManager : BBIPhoneTouchManager {

#if UNITY_STANDALONE
	public bool flipTUIO=false; // rotate events by 180 degrees. might be extendable to 90/270 degrees.
	
	// the number of simultaneous TUIO clients, starting with the default port 3333
	private int _numListeners = 1;
	public int numListeners {
		get { return _numListeners; }
		set {
			_numListeners=value;
			tuioInput.numClients=_numListeners;
		}
	}		

	protected BBInputController tuioInput;
	protected UnityEngine.EventSystems.EventSystem uGUIes;
	protected UnityEngine.EventSystems.TUIOInputModule uGUIim;
	
	protected override void Awake(){
		tuioInput = new BBInputController();
		tuioInput.eventDelegate = this;
		tuioInput.numClients=_numListeners;
		base.Awake();
	}

	protected override void Start(){
		// can't do this in Awake(), since it will fail if it is set in an outside-Awake() (such as BBTouchManagerStarter)...
		tuioInput.collectEvents = usePolling;
		tuioInput.detectMarkers = detectMarkers;
		tuioInput.flipTUIO = flipTUIO;

		if(!uGUIes) // OnLevelWasLoadedDelegate might have been called before us... -.-
			uGUIes=FindObjectOfType<UnityEngine.EventSystems.EventSystem>();
		if(uGUIes && !uGUIim){
			// we have some UnityGUI!
			uGUIim=uGUIes.gameObject.AddComponent<UnityEngine.EventSystems.TUIOInputModule>();
		}
		base.Start();
	}

	protected override void OnLevelWasLoadedDelegate(Scene scene, LoadSceneMode m){
		base.OnLevelWasLoadedDelegate(scene, m);
		if(uGUIes && uGUIim)
			return; // all good; nothing changed

		if(!uGUIes)
			uGUIes=FindObjectOfType<UnityEngine.EventSystems.EventSystem>(); // maybe this scene has uGUI?
		if(uGUIes && !uGUIim){
			// we have some UnityGUI!
			uGUIim=uGUIes.gameObject.AddComponent<UnityEngine.EventSystems.TUIOInputModule>();
		}
	}

	// Ensure that the instance is destroyed when the game is stopped in the editor.
	void OnApplicationQuit() 
	{
		if (tuioInput != null) {
			tuioInput.eventDelegate = null;
			tuioInput.collectEvents = false;
			tuioInput.disconnectAll();
		}
	}
	
	public BBInputController inputController {
		get {
			return tuioInput;
		}
	}

	public override List<BBCursorEvent> getAndClearCursorEvents(){
		List<BBCursorEvent> eventList=base.getAndClearCursorEvents();
		eventList.AddRange(tuioInput.getAndClearCursorEvents()); // TUIO events have STARTID_EXTERNAL ID, see BBInputController
		return eventList;
	}

	public override List<BBObjectEvent> getAndClearObjectEvents(){
		List<BBObjectEvent> eventList=base.getAndClearObjectEvents();
		eventList.AddRange(tuioInput.getAndClearObjectEvents()); // TUIO events have STARTID_EXTERNAL ID, see BBInputController
		return eventList;
	}
	
	public override void finishFrame(){
		if(uGUIim){
			// pass along the TUIO events (and only those!) to the uGUI EventSystem
			lock (eventQueueLock) {
				uGUIim.currentEventList.Clear();
				foreach(BBTouchEvent anEvent in eventQueue){
					if(anEvent.eventID>=STARTID_EXTERNAL && anEvent.eventID<STARTID_MOUSEMARKER)
						uGUIim.currentEventList.Add(anEvent);
				}
			}
		}
		base.finishFrame();
	}

	protected override void Update(){
		if(emulateMouseEvents){
			bool needsCleanup=false;
	
			// disable mouse support if we have a local TUIO client, as we assume the OS will duplicate all events as emulated mouse input
			for(int i=0;i<tuioInput.numClients;i++)
				if(tuioInput.currentFrame(i)>0 && tuioInput.isLocalClient(i)){
					needsCleanup=true;

					// disable mouse support
					Debug.LogWarning("Found local TUIO device! Disabling mouse emulation!");
					emulateMouseEvents=false;
					break;
				}
			if(needsCleanup)
				CleanupPendingTouches();
		}

		base.Update();
	}

	private void CleanupPendingTouches(){
		// cleanup all currently active win-native and mouse events (=buttons still being pushed)
		// use two steps since we cannot modify activeEvents on-the-fly
		List<BBTouchEvent> deathList=new List<BBTouchEvent>();
		foreach(BBTouchEvent ev in activeEvents.Values)
			if(ev.eventID>=STARTID_MOUSEMARKER) // includes all mouse stuff
				deathList.Add(ev);
		foreach(BBTouchEvent ev in deathList)
			if(ev.symbolID>=0)
				objectRemove(ev.eventID,ev.symbolID,ev.normPosition.x,1-ev.normPosition.y,ev.tuioAngle);
			else
				cursorUp(ev.eventID,ev.normPosition.x,1-ev.normPosition.y);
	}
#endif
}

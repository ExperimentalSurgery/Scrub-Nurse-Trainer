using UnityEngine;
using System.Collections;

// the scene manager's biggest responsibility is to make sure that there is a valid touch event manager 
// in the scene.  The touch event manager should not be destroyed so it will need to 
// persist between scenes.  This is why we are doing this complicated dance to figure
// out if there is already one in the scene or not.

// the first scene in your application should have one of these objects

public class BBTouchManagerStarter : MonoBehaviour {

	// true: create touch events from mouse events
	private bool emulateMouseEvents=true;

	// support for TUIO? ((currently) UNITY_STANDALONE only! set to false for all other platforms)
	public bool useTUIO=true;

	// the number of simultaneous TUIO clients, starting with the default port 3333
	public int numListeners = 1;
	
	// false: original variant: asynchronous TUIO, pushing events
	// true: new variant: inputDelegate is polling events by himself
	// NOTE: this will not influence emulated mouse events
	// NOTE: always true for mobile
	public bool usePolling=true; // default to new variant (seems to improve DisplaX touch recognition)...

	public bool flipTUIO=false;

	// so far supported for native TUIO only!
	public bool detectMarkers = false;
	
	// map from consecutive indices to marker IDs. manually add all desired IDs here
	public int[] markerMapping = new int[0];
	
	// number of allowed bit errors to still correctly identify a registered marker
	// if tolerance is not possible, print error on DebugConsole
	public int markersMaxBitTolerance = 2;
	
	public bool searchCamsOnDemandOnly = false;
	
	public bool sendTUIO = false;
	public string remoteIP = "192.168.0.152";
	public int remotePort = 3333;

	// get the current touch manager singleton,
	// apply the local flags
	void Awake() {
#if !UNITY_STANDALONE
		useTUIO=false; // silently set to false
#endif
		BBInputDelegate.useTUIO=useTUIO; // override flag before creating singleton instance...

#if UNITY_STANDALONE && !UNITY_EDITOR
		Cursor.visible=false; // disable cursor initially. automatically activate 
#endif

#if UNITY_STANDALONE
		if(BBInputDelegate.useTUIO){
			BBTouchEventManager managerTUIO = BBInputDelegate.instance as BBTouchEventManager;
			// pass individual settings
			managerTUIO.numListeners=numListeners;
			managerTUIO.flipTUIO=flipTUIO;
			managerTUIO.searchCamsOnDemandOnly=searchCamsOnDemandOnly;
			BBInputDelegate.instance.usePolling=usePolling;
		}else
#endif
			BBInputDelegate.instance.usePolling=true; // always true

		// pass global settings
		// NOTE: most of them are only valid for TUIO-capable platforms (=UNITY_STANDALONE)! So maybe they should be moved to BBTouchEventManager at some point...
		BBInputDelegate.instance.emulateMouseEvents=emulateMouseEvents;
		BBInputDelegate.instance.detectMarkers=detectMarkers;
		BBInputDelegate.instance.ReplaceMarkerMapping(markerMapping);
		BBInputDelegate.instance.markersMaxBitTolerance=markersMaxBitTolerance;
		BBInputDelegate.instance.sendTUIO=sendTUIO;
		BBInputDelegate.instance.remoteIP=remoteIP;
		BBInputDelegate.instance.remotePort=remotePort;
	}
}

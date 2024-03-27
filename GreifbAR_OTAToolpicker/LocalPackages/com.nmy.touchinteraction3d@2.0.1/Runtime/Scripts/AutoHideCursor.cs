using UnityEngine;
using System.Collections;
using System.Runtime.InteropServices;

namespace NMY.TouchInteraction3D {

public class AutoHideCursor : MonoBehaviour {
	
#if UNITY_STANDALONE_WIN
	[DllImport("user32.dll")]
	private static extern uint GetMessageExtraInfo( );
#endif
	
	public float timeout=2;
	private Vector3 mousePos;
	private float moveTime;
	
// do nothing in the editor (and for now, web)
#if UNITY_STANDALONE && !UNITY_EDITOR
	void Start(){
		// default to off
		Cursor.visible=false;
		mousePos=Input.mousePosition;
	}
	
	void Update(){
		bool hideCursor=false;
		if(BBInputDelegate.Exists() && !BBInputDelegate.instance.emulateMouseEvents)
			hideCursor=true; // ALWAYS hide if we have a TouchInteraction3D system that does NOT want mouse events

		uint extra = 0;
#if UNITY_STANDALONE_WIN
		extra=GetMessageExtraInfo();
#endif
		bool isPen = ( ( extra &  0xFFFFFF00 ) == 0xFF515700 ); // pen or touch
		if(isPen)
			hideCursor=true;

		if(hideCursor){
			Cursor.visible=false;
			mousePos=Input.mousePosition; // update mouse pos anyway, to prevent problems due to incorrect Win7 isPen values
//			enabled=false; // stay enabled, as we now distinguish on-the-fly
			return;
		}
		
		if(Time.time-moveTime>timeout)
			Cursor.visible=false;

		// don't reset timer and show if this is a pen/touch
		if(isPen){
			mousePos=Input.mousePosition; // update mouse pos anyway, to prevent problems due to incorrect Win7 isPen values
			return;
		}
		
		// more hacks, to prevent problems due to incorrect Win7+Win8 isPen values
		if(Input.GetMouseButtonUp(0)){
			mousePos=Input.mousePosition; // force reset
			if(Input.GetMouseButton(0)){
				// NOTE we also arrive here on fast click-release events (for example, if framerate is low). But we don't care.
				Cursor.visible=false; // force hide
			}
		}
			
		if(Input.mousePosition!=mousePos ||
		   Input.GetMouseButton(0) && !Input.GetMouseButtonUp(0) || // more hacks, to prevent problems due to incorrect Win7 isPen values
		   Input.GetMouseButton(1) ||
		   Input.GetMouseButton(2)){
				mousePos=Input.mousePosition;
				moveTime=Time.time;
				Cursor.visible=true;
		}
	}
#endif
}

}
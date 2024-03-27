using UnityEngine;
using System.Collections;
using System.Collections.Generic;

// Manager for iPhone events

// we probably have no control about how and when the iPhoneTouch events are generated,
// so we revert to polling mode, and simply create BBTouchEvents (via BBCursorEvents and BBInputDelegate::processEvents)
// for all iPhoneTouch events, everytime.

// NOTE: config flags should be passed through from BBTouchManagerStarter, since BBIPhoneTouchManager will be created on-the-fly.
public class BBIPhoneTouchManager : BBInputDelegate {

	protected virtual void Start(){
		// we need to default to polling mode
		usePolling=true;

		// ...and disable mouse emulation // 20160428 does it matter? do we rely on doublette catching if we keep it on? NOTE: Webplayer ignores this completely (always on; see BBID.Update())
		//emulateMouseEvents=false;
	}

	// create BBCursorEvents from iPhoneTouches
	// NOTE: if that turns out to not be enough, we need to create BBTouchEvents instead.
	public override List<BBCursorEvent> getAndClearCursorEvents(){
		BBCursorState state;
		List<BBCursorEvent> events=new List<BBCursorEvent>();
		for(int i=0;i<Input.touchCount;i++){
			Touch touch=Input.GetTouch(i);
			switch(touch.phase){
				case TouchPhase.Began:
					state=BBCursorState.Add;
					break;
				case TouchPhase.Ended:
					state=BBCursorState.Remove;
					break;
				default:
					state=BBCursorState.Update;
					break;
			}
			events.Add(new BBCursorEvent(touch.fingerId,touch.position.x/Screen.width,1-touch.position.y/Screen.height,state));
		}
		return events;
	}
}

using UnityEngine;
using System.Collections;

namespace NMY.TouchInteraction3D {

/// <summary>
/// Spawn event if nobody has touched us for a while
/// </summary>
/// <remarks>
/// Externally force timer reset by calling CheckLeaveIdleMode().
/// </remarks>
public class IdleTrigger : BBTouchableObject {

	public event System.EventHandler EnterIdleMode;
	public event System.EventHandler LeaveIdleMode;
	
	public float idleInterval=60; // when to start idle mode
	
	private bool _isIdle;
	public bool isIdle {
		get { return _isIdle; }
	}
	
	private float idleStartTime;
	
	override protected void StartupEnter() {
		base.StartupEnter();
		
		CheckLeaveIdleMode();
	}
	
	// use FixedUpdate since it's not timing critical
	void FixedUpdate () {
		if(isPushed)
			CheckLeaveIdleMode();
		CheckEnterIdleMode();
	}

    // reset on activation
    void OnEnable()
    {
        CheckLeaveIdleMode();
    }

	void CheckEnterIdleMode(){
		if(Time.time-idleStartTime>=idleInterval && !_isIdle){
			_isIdle=true;
			RaiseEnterIdleMode();
		}
	}
	
	public void CheckLeaveIdleMode(){
		if(_isIdle){
			RaiseLeaveIdleMode();
			_isIdle=false;
		}
		idleStartTime=Time.time;
	}
	
	public override void handleSingleTouch(BBTouchEvent touch0){
		CheckLeaveIdleMode();
	}

	public override void handleDoubleTouch(BBTouchEvent[] events){
		CheckLeaveIdleMode();
	}
	
	public override void handleTripleTouch(BBTouchEvent[] events){
		CheckLeaveIdleMode();
	}
	
	public override void noTouches() {
		// do nothing - FixedUpdate() will start idle mode
	}
	
	#region Event raisers
	protected void RaiseEnterIdleMode() {
//		Debug.Log ("entering idle mode");
		if (EnterIdleMode!=null) 
			EnterIdleMode(this, System.EventArgs.Empty);
	}
	protected void RaiseLeaveIdleMode() {
//		Debug.Log ("leaving idle mode");
		if (LeaveIdleMode!=null) 
			LeaveIdleMode(this, System.EventArgs.Empty);
	}
	#endregion
}

} // namespace

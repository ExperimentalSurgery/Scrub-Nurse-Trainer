using UnityEngine;
using System.Collections;

namespace NMY.TouchInteraction3D {

/// <summary>
/// Spawn event if the global touchmanager is idle for a while
/// </summary>
/// <remarks>
/// Externally force timer reset by calling CheckLeaveIdleMode().
/// </remarks>
public class IdleTriggerGlobal : NMY.StartupBehaviour {

	public event System.EventHandler EnterIdleMode;
	public event System.EventHandler LeaveIdleMode;
	
	public float idleInterval=60; // when to start idle mode
	
	private bool _isIdle;
	public bool isIdle {
		get { return _isIdle; }
	}
	
	private float idleStartTime;
	
	override protected void StartupEnter() {
		CheckLeaveIdleMode();
	}
	
	// use FixedUpdate since it's not timing critical
	void FixedUpdate () {
		if(BBInputDelegate.instance.activeEvents.Count>0)
			CheckLeaveIdleMode();
		CheckEnterIdleMode();
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
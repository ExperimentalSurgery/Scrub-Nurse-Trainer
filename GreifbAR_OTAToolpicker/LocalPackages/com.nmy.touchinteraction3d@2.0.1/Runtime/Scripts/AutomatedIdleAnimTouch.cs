using UnityEngine;
using System.Collections;

namespace NMY.TouchInteraction3D {

/// <summary>
/// Script to automatically start an idle-animation within the given RuseCamTouch limits.
/// </summary>
/// <remarks>
/// - Uses some form of Lissajous harmonic.
/// - Takes into account any movement one has already performed during RuseCamTouch interaction
///   (i.e., crossfades between that endposition and the idle-animation position).
/// </remarks>
[RequireComponent (typeof(RuseCamTouch))]
public class AutomatedIdleAnimTouch : MonoBehaviour {
	
	public float delay=5; // delay before idle animation starts
	public float amplitudeFadeIn=15; // time until idle animation reaches full amplitude
	public float speed=0.1f;
	
	private RuseCamTouch ruseCamTouch;
	private Transform camTrans;  // the camera transform (local variable for speed reasons)
	private float idleStart;
	private Vector3 startPos;
	private float freqX;
	private float freqZ;
	private Vector3 oldPosition;
	
	void Awake(){
		ruseCamTouch=GetComponent<RuseCamTouch>();
		camTrans = Camera.main.transform;

		freqX=speed;
		freqZ=speed*0.71f; // less frequent along depth axis. use a factor that does not (noticeable) resonate with freqX

		idleStart=Time.time;
		startPos=camTrans.position;
		oldPosition=startPos;
	}
	
	void OnEnable(){
		idleStart=Time.time;
		startPos=camTrans.position;
		oldPosition=startPos;
	}

	void FixedUpdate(){
		if(ruseCamTouch.isMoving||oldPosition!=camTrans.position){
			idleStart=Time.time;
			startPos=camTrans.position;
			oldPosition=startPos;
			return;
		}
		
		float idleTime=Time.time-idleStart;
		if(idleTime<delay){
			startPos=camTrans.position;
			oldPosition=startPos;
			return;
		}
		
		float factor=Mathf.Min((idleTime-delay)/amplitudeFadeIn,1f);
		
		float amplitudeX=(ruseCamTouch.maxX-ruseCamTouch.minX)/2;
		float amplitudeZ=(ruseCamTouch.maxZ-ruseCamTouch.minZ)/2;
		
		camTrans.position=new Vector3((1-factor)*startPos.x+factor*(ruseCamTouch.minX+amplitudeX+amplitudeX*Mathf.Sin(freqX*Time.time*2*Mathf.PI)),
		                              startPos.y,
		                              (1-factor)*startPos.z+factor*(ruseCamTouch.minZ+amplitudeZ+amplitudeZ*Mathf.Cos(freqZ*Time.time*2*Mathf.PI)));
		oldPosition=camTrans.position;
	}
}

} // namespace
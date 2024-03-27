using UnityEngine;
using System.Collections;

namespace NMY.TouchInteraction3D {

/// - Uses some form of Lissajous harmonic.
public class AutomatedIdleAnim : StartupBehaviour {
	
	public float speed=0.1f;
	public float weight=1;
	public Vector3 offset=Vector3.zero;
	public Vector3 amplitude=Vector3.one;
	public bool randomizeStartT=true;
	
	private Vector3 startPos;
	private float startTime;
	private float freqX;
	private float freqY;
	private float freqZ;
	
	override protected void StartupEnter(){
		freqX=speed;
		freqY=speed*0.71f; // less frequent along depth axis. use a factor that does not (noticeable) resonate with freqX
		freqZ=speed*0.83f; // less frequent along 3rd axis. use a factor that does not (noticeable) resonate with freqX and freqY

		startPos=transform.localPosition;
		if(randomizeStartT)
			startTime=Random.Range(0,1/freqX/freqY);
	}
	
	void Update(){
		transform.localPosition=new Vector3(startPos.x+offset.x+weight*(amplitude.x*Mathf.Sin(freqX*(Time.time+startTime)*2*Mathf.PI)),
		                                    startPos.y+offset.y+weight*(amplitude.y*Mathf.Cos(freqY*(Time.time+startTime)*2*Mathf.PI)),
		                                    startPos.z+offset.z+weight*(amplitude.z*Mathf.Cos(freqZ*(Time.time+startTime)*2*Mathf.PI))); // NOTE: also uses Cos-phase
	}
}

} // namespace
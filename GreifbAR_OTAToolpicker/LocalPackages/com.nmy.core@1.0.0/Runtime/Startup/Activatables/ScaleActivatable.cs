using UnityEngine;
using System.Collections;

namespace NMY {

/// <summary>
/// An ActivatableStartupBehaviour that animates scale instead of alpha for a single group of objects.
/// To use a timed progression of several objects, apply one RecursiveScaleActivator.
/// </summary>
/// <remarks>
/// Uses original local scale as reference target.
/// The local parameters fadeDuration, delay, and totalLength will be overwritten when using a
/// RecursiveScaleActivator. In that case, use index to control the order.
/// NOTE: cleanly sends all {De}Activate{d}-Events
/// </remarks>
public class ScaleActivatable : ActivatableStartupBehaviour {
	
	public enum AxisOrder {
		AllSynchronous,
		YInFirst_YOutFirst,
		YInFirst_YOutLast,
		YInLast_YOutFirst,
		YInLast_YOutLast
	}
	public float fadeDuration=0.5f;
	public float delay=0f;
	public float totalLength=0.5f;
	public iTween.EaseType easeTypeIn=iTween.EaseType.easeOutBack; // NOTE: in/out is kinda reversed!
	public iTween.EaseType easeTypeOut=iTween.EaseType.easeInBack; // NOTE: in/out is kinda reversed!
	public AxisOrder axisOrder=AxisOrder.YInFirst_YOutLast;
	public int index=0; // the order of activation when using a RecursiveScaleActivator
	
	public bool scaleXAxis=true;
	public bool scaleYAxis=true;
	public bool scaleZAxis=true;
	
	public float origX;
	public float origY;
	public float origZ;
	
	public bool deactivateObjects=false; // defaults to false for possible back compatibility conflicts
	public bool scaleToZero=false;	// NOTE: Setting this to true can /mess with Unity's Animation logic ("type == m_TransformInfo.transformType"),
									// and will also mess with (Mesh-?)colliders. Set to "false" if there are any touchable objects in this hierarchy!
	private float zeroValue=0;
	
	override protected void StartupEnter(){
		origX = transform.localScale.x;
		origY = transform.localScale.y;
		origZ = transform.localScale.z;

		// no mixture of Y and X/Z? enforce synchronous! (warning: weird boolean optimization)
		if(!scaleYAxis || !scaleXAxis && !scaleZAxis)
			axisOrder=AxisOrder.AllSynchronous;
			
		if(totalLength<fadeDuration+delay)
			totalLength=fadeDuration+delay;

		if(axisOrder!=AxisOrder.AllSynchronous)
			if(totalLength<fadeDuration*1.1f+delay+fadeDuration/4)
				totalLength=fadeDuration*1.1f+delay+fadeDuration/4;

		if(!scaleToZero)
			zeroValue=1e-4f;
	}

	#region IActivatable
	override protected void ActivateEnter() {
		StopDeactivate();
		
		if(deactivateObjects)
			gameObject.SetActive(true);
		
		if(axisOrder==AxisOrder.AllSynchronous){
			// all axes simultaneously
			Hashtable args = new Hashtable();
			args.Add("time", fadeDuration);
			args.Add("delay", delay);
			args.Add("easetype", easeTypeIn);
			args.Add("scale", new Vector3(scaleXAxis?origX:transform.localScale.x,scaleYAxis?origY:transform.localScale.y,scaleZAxis?origZ:transform.localScale.z));
			args.Add("oncomplete", "OnScaleActivateFinished");
			iTween.ScaleTo(gameObject, args);
		}else{
			// Y first, then XZ
			Hashtable args = new Hashtable();
			args.Add("time", fadeDuration);
			args.Add("delay", delay);
			args.Add("easetype", easeTypeIn);
			args.Add("from", zeroValue);
			args.Add("to", 1);
			args.Add("onupdate", (System.Action<object>) (newVal => {
				// on first call, reset scale, to fix artifacts with concurrent Deactivate/Activate calls
				if((float)newVal==0)
					transform.localScale=new Vector3(scaleXAxis?zeroValue:transform.localScale.x,scaleYAxis?zeroValue:transform.localScale.y,scaleZAxis?zeroValue:transform.localScale.z);
				if(axisOrder==AxisOrder.YInFirst_YOutFirst ||
				   axisOrder==AxisOrder.YInFirst_YOutLast){
					// update Y
					transform.localScale=new Vector3(transform.localScale.x,
													 scaleYAxis?(float)newVal*origY:transform.localScale.y,
													 transform.localScale.z);
				}else{
					// update XZ
					transform.localScale=new Vector3(scaleXAxis?(float)newVal*origX:transform.localScale.x,
													 transform.localScale.y,
													 scaleZAxis?(float)newVal*origZ:transform.localScale.z);
				}
			}));
			iTween.ValueTo(gameObject, args);
			
			args = new Hashtable();
			args.Add("time", fadeDuration*1.1f);
			args.Add("delay", delay+fadeDuration/4);
			args.Add("easetype", easeTypeIn);
			args.Add("from", zeroValue);
			args.Add("to", 1);
			args.Add("onupdate", (System.Action<object>) (newVal => {
				if(axisOrder==AxisOrder.YInFirst_YOutFirst ||
				   axisOrder==AxisOrder.YInFirst_YOutLast){
					// update XZ
					transform.localScale=new Vector3(scaleXAxis?(float)newVal*origX:transform.localScale.x,
													 transform.localScale.y,
													 scaleZAxis?(float)newVal*origZ:transform.localScale.z);
				}else{
					// update Y
					transform.localScale=new Vector3(transform.localScale.x,
													 scaleYAxis?(float)newVal*origY:transform.localScale.y,
													 transform.localScale.z);
				}
			}));
			args.Add("oncomplete", "OnScaleActivateFinished");
			iTween.ValueTo(gameObject, args);
		}
	}

	override protected void ActivateExit() {}
	
	private void OnScaleActivateFinished(){
		SendActivatedEvent();
	}
	
	override protected void DeactivateEnter() {
		StopActivate();
		
		if(axisOrder==AxisOrder.AllSynchronous){
			// all axes simultaneously
			Hashtable args = new Hashtable();
			args.Add("time", fadeDuration);
			args.Add("delay", totalLength-fadeDuration-delay);
			args.Add("easetype", easeTypeOut);
			args.Add("scale", new Vector3(scaleXAxis?zeroValue:transform.localScale.x,scaleYAxis?zeroValue:transform.localScale.y,scaleZAxis?zeroValue:transform.localScale.z));
			args.Add("oncomplete", "OnScaleDeactivateFinished");
			iTween.ScaleTo(gameObject, args);
		}else{
			// be sure to call oncomplete only after both iTweens have finished
			bool firstITweenIsSlowest=delay+fadeDuration*1.1f>totalLength-delay+fadeDuration/4;
			bool firstITweenStartsFirst=delay<totalLength-fadeDuration-delay+fadeDuration/4;
				
			// XZ first, then Y
			Hashtable args = new Hashtable();
			args.Add("time", fadeDuration*1.1f);
			args.Add("delay", totalLength-fadeDuration-delay-fadeDuration/4);
			args.Add("easetype", easeTypeOut);
			args.Add("from", 1f);
			args.Add("to", zeroValue);
			args.Add("onupdate", (System.Action<object>) (newVal => {
				// on first call, reset scale, to fix artifacts with concurrent Deactivate/Activate calls
				if((float)newVal==0 && firstITweenStartsFirst)
					transform.localScale=new Vector3(scaleXAxis?origX:transform.localScale.x,scaleYAxis?origY:transform.localScale.y,scaleZAxis?origZ:transform.localScale.z);
				if(axisOrder==AxisOrder.YInFirst_YOutLast ||
				   axisOrder==AxisOrder.YInLast_YOutLast){
					// update Y
					transform.localScale=new Vector3(transform.localScale.x,
													 scaleYAxis?(float)newVal*origY:transform.localScale.y,
													 transform.localScale.z);
				}else{
					// update XZ
					transform.localScale=new Vector3(scaleXAxis?(float)newVal*origX:transform.localScale.x,
													 transform.localScale.y,
													 scaleZAxis?(float)newVal*origZ:transform.localScale.z);
				}
			}));
			if(firstITweenIsSlowest)
				args.Add("oncomplete", "OnScaleDeactivateFinished");
			iTween.ValueTo(gameObject, args);
			
			args = new Hashtable();
			args.Add("time", fadeDuration);
			args.Add("delay", totalLength-fadeDuration-delay);
			args.Add("easetype", easeTypeOut);
			args.Add("from", 1f);
			args.Add("to", zeroValue);
			args.Add("onupdate", (System.Action<object>) (newVal => {
				// on first call, reset scale, to fix artifacts with concurrent Deactivate/Activate calls
				if((float)newVal==0 && !firstITweenStartsFirst)
					transform.localScale=new Vector3(scaleXAxis?origX:transform.localScale.x,scaleYAxis?origY:transform.localScale.y,scaleZAxis?origZ:transform.localScale.z);
				if(axisOrder==AxisOrder.YInFirst_YOutLast ||
				   axisOrder==AxisOrder.YInLast_YOutLast){
					// update XZ
					transform.localScale=new Vector3(scaleXAxis?(float)newVal*origX:transform.localScale.x,
													 transform.localScale.y,
													 scaleZAxis?(float)newVal*origZ:transform.localScale.z);
				}else{
					// update Y
					transform.localScale=new Vector3(transform.localScale.x,
													 scaleYAxis?(float)newVal*origY:transform.localScale.y,
													 transform.localScale.z);
				}
			}));
			if(!firstITweenIsSlowest)
				args.Add("oncomplete", "OnScaleDeactivateFinished");
			iTween.ValueTo(gameObject, args);
		}
	}

	override protected void DeactivateExit() {}

	private void OnScaleDeactivateFinished(){
		if(deactivateObjects)
			gameObject.SetActive(false);
		SendDeactivatedEvent();
	}
	
	override protected void ActivateImmediatelyEnter() {
		StopDeactivate();

		if(deactivateObjects)
			gameObject.SetActive(true);
		
		transform.localScale=new Vector3(scaleXAxis?origX:transform.localScale.x,scaleYAxis?origY:transform.localScale.y,scaleZAxis?origZ:transform.localScale.z);
	}

	override protected void DeactivateImmediatelyEnter() {
		StopActivate();
		
		transform.localScale=new Vector3(scaleXAxis?zeroValue:transform.localScale.x,scaleYAxis?zeroValue:transform.localScale.y,scaleZAxis?zeroValue:transform.localScale.z);

		if(deactivateObjects)
			gameObject.SetActive(false);
	}

	override protected void StopActivate() {
		iTween.Stop(gameObject,"scale");
		iTween.Stop(gameObject,"value");
	}

	override protected void StopDeactivate() {
		iTween.Stop(gameObject,"value");
		iTween.Stop(gameObject,"scale");
	}
	#endregion	
}

}
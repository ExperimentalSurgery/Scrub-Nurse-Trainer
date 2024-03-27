using UnityEngine;
using System.Collections;

namespace NMY {

/// <summary>
/// Recursively control (de)activation of all ScaleActivatables in this subtree.
/// Pass down parameters for timed (de)activation progression.
/// </summary>
/// <remarks>
/// Need to have "index" assigned in all ScaleActivatables.
/// </remarks>
public class RecursiveScaleActivator : RecursiveActivator {

	// global parameters passed down
	public float fadeDuration=0.5f;
	public float initialDelay=0.5f; // iTween result looked ugly if this was 0
	public float delay=0.5f;

	private ScaleActivatable[] scaleActivatables;

	override protected void StartupEnter(){
		base.StartupEnter();

		// need a subset
		scaleActivatables=GetComponentsInChildren<ScaleActivatable>();

		int maxIndex=0;
		// find max index
		foreach(ScaleActivatable sa in scaleActivatables)
			if(maxIndex<sa.index)
				maxIndex=sa.index;

		// set appropriate parameters
		foreach(ScaleActivatable sa in scaleActivatables){
			sa.fadeDuration=fadeDuration;
			sa.delay=initialDelay+delay*sa.index;
			sa.totalLength=initialDelay+delay*maxIndex+fadeDuration;
		}
		
		// only startup ScaleActivatables
		foreach(ScaleActivatable sa in scaleActivatables){
			sa.Startup();
		}
	}
}

}
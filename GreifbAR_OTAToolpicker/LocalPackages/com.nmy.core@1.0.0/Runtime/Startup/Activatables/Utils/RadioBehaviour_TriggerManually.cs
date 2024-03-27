using UnityEngine;
using System.Collections;
using NMY;

namespace NMY {

	/// <summary>
	/// Small test script to manually control RadioBehaviour in the Inspector.
	/// </summary>
	public class RadioBehaviour_TriggerManually : ActivatableStartupBehaviour_TriggerManually {

		public int currentItem;
		private int previousItem;

		override protected void Awake(){
			if(!activatableStartupBehaviour)
				activatableStartupBehaviour=GetComponent<RadioBehaviour>();
			if(!activatableStartupBehaviour){
				Debug.LogError("Need to assign a RadioBehaviour, or attach this script to a GO that has one.");
				enabled=false;
			}
			currentItem=(activatableStartupBehaviour as RadioBehaviour).currentItem;
			previousItem=currentItem;
			previousState=!activator; ///< force triggering on first frame
		}
		
		override protected void Update(){
			base.Update();
			
			if(previousItem!=currentItem){
				(activatableStartupBehaviour as RadioBehaviour).currentItem=currentItem;
				previousItem=currentItem;
			}
		}
	}

} // namespace
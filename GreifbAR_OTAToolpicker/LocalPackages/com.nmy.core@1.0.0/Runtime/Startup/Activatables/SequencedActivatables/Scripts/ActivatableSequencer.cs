using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using NMY.DelegateCoroutines;

namespace NMY{
	/// <summary>
	/// A class which allows for a sequenced acivation of acitvatables.
	/// The duration each item stays activated as well as the offset with which the next item should be activated can be modified.
	/// </summary>
	public class ActivatableSequencer : ActivatableStartupBehaviour{
		public event EventHandler SequenceFinished;

		public ActivatableStartupBehaviour[] items;

		[Header("Sequence parameters")]
		[Tooltip("Idle duration after which the next item will be activated")]
		public float offsetPerItem;
		[Tooltip("Whether or not to deactivate the previous item before activating the next one.")]
		public bool deactivatePreviousItem;
		public bool loop;
		[Tooltip("If 0, loops infinitely (or until deactivation).")]
		public int loopTimes;

		protected DelegateCoroutine waitingRoutine;
		protected int currentIndex;
		protected int repetitions;

		protected override void StartupEnter(){
			foreach(ActivatableStartupBehaviour item in items){
				item.Startup();
			}
		}

		#region IActivatable
		protected override void ActivateEnter(){
			if(items.Length > 0)
				ActivateCurrentItem();
		}

		protected override void ActivateImmediatelyEnter(){
			if(items.Length > 0)
				ActivateCurrentItem(true);
		}

		protected override void DeactivateEnter(){
			if(!deactivatePreviousItem){
				foreach(ActivatableStartupBehaviour item in items)
					item.Deactivate();
			}
			else if(currentIndex >= 0 && currentIndex < items.Length){
				items[currentIndex].Deactivate();
			}

			Reset();
		}

		protected override void DeactivateImmediatelyEnter(){
			if(!deactivatePreviousItem){
				foreach(ActivatableStartupBehaviour item in items)
					item.DeactivateImmediately();
			}
			else if(currentIndex >= 0 && currentIndex < items.Length){
				items[currentIndex].DeactivateImmediately();
			}

			Reset();
		}
		#endregion

		/// <summary>
		/// Deactivates the current item and triggers the activation of the next item.
		/// Raises the SequenceCompleted event if the last item has been activated and the sequencer does not loop.
		/// </summary>
		protected void TriggerNextItem(){
			if(waitingRoutine != null)
				waitingRoutine.Stop();

			// Optionally deactivate current  item
			if(deactivatePreviousItem && currentIndex >= 0 && currentIndex < items.Length){
				items[currentIndex].Deactivate();
			}
			currentIndex += 1;

			// Trigger next item activation
			if(currentIndex > 0 && currentIndex < items.Length){
				// Trigger next item
				ActivateCurrentItem();
			}
			else if(currentIndex == items.Length && loop && (loopTimes == 0 || repetitions < loopTimes)){
				// Deactivate items
				if(!deactivatePreviousItem){
					foreach(ActivatableStartupBehaviour item in items)
						item.Deactivate();
				}
				// this part was never called (as we are in an "if(currentIndex == items.Length"...)
				//else if(currentIndex >= 0 && currentIndex < items.Length){
				//	items[currentIndex].Deactivate();
				//}

				// Repeat the sequence
				repetitions += 1;
				currentIndex = 0;
				ActivateCurrentItem();
			}
			else{
				// Raise finished event
				RaiseSequenceFinished();
			}
		}

		/// <summary>
		/// Activates the current item and starts the coroutine for the next item's activation.
		/// </summary>
		/// <param name="immediately">If set to <c>true</c> immediately.</param>
		protected void ActivateCurrentItem(bool immediately = false){
			// Use either global or individual item activation duration
			float nextItemDelay = offsetPerItem;
			if(items[currentIndex] is ISequencable && (items[currentIndex] as ISequencable).GetOffset() > 0)
				nextItemDelay = (items[currentIndex] as ISequencable).GetOffset();
			if(nextItemDelay <= 0)
				nextItemDelay = 1f;	// use fallback delay value!
			
			// Activate current item
			if(immediately)
				items[currentIndex].ActivateImmediately();
			else
				items[currentIndex].Activate();
		
			// Wait and trigger next item activation
			waitingRoutine = this.WaitAndExecute(nextItemDelay, () => {
				TriggerNextItem();
			});
		}

		protected void RaiseSequenceFinished(){
			if(SequenceFinished != null)
				SequenceFinished(this, EventArgs.Empty);
		}

		protected void Reset(){
			currentIndex = 0;
			repetitions = 0;
			if(waitingRoutine != null)
				waitingRoutine.Stop();
		}
	}
}
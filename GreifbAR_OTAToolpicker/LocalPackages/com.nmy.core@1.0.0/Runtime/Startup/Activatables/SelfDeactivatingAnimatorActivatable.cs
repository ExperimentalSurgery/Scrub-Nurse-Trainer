using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NMY.DelegateCoroutines;

namespace NMY{
	public class SelfDeactivatingAnimatorActivatable : SimpleAnimatorActivatable{
		public float deactivationDelay = 1f;

		protected DelegateCoroutine deactivationRoutine;

		#region IActivatable
		protected override void ActivateEnter(){
			base.ActivateEnter();

			// Trigger delayed deactivation
			if(deactivationDelay > 0){
				deactivationRoutine = this.WaitAndExecute(deactivationDelay, () => {
					Deactivate();
				});
			}
		}

		protected override void ActivateImmediatelyEnter(){
			base.ActivateImmediatelyEnter();

			// Trigger delayed deactivation
			if(deactivationDelay > 0){
				deactivationRoutine = this.WaitAndExecute(deactivationDelay, () => {
					DeactivateImmediately();
				});
			}
		}

		protected override void DeactivateEnter(){
			base.DeactivateEnter();

			if(deactivationRoutine != null)
				deactivationRoutine.Stop();
		}

		protected override void DeactivateImmediatelyEnter(){
			base.DeactivateImmediatelyEnter();

			if(deactivationRoutine != null)
				deactivationRoutine.Stop();
		}
		#endregion
	}
}
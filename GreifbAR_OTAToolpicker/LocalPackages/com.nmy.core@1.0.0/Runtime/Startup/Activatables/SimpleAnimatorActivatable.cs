using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace NMY{
	public class SimpleAnimatorActivatable : ActivatableStartupBehaviour{
		public Animator animator;
		public string parameterName = "show";
		protected int parameterHash;
		static protected readonly int stateHash_Shown=Animator.StringToHash("Shown");
		static protected readonly int stateHash_Hidden=Animator.StringToHash("Hidden");

		protected override void StartupEnter(){
			parameterHash=Animator.StringToHash(parameterName);
			if(!animator)
				animator = GetComponent<Animator>();
		}

		#region IActivatable
		protected override void ActivateEnter(){
			if(animator && animator.enabled)
				animator.SetBool(parameterHash, true);
			else
				gameObject.SetActive(true);
		}

		protected override void DeactivateEnter(){
			if(animator && animator.enabled)
				animator.SetBool(parameterHash, false);
			else
				gameObject.SetActive(false);
		}

		protected override void ActivateImmediatelyEnter(){
			if(animator && animator.enabled) {
				animator.SetBool(parameterHash, true);
				animator.CrossFade(stateHash_Shown, 0f, 0, 1);	// Skip animation and jump to last frame
			}
			else{
				gameObject.SetActive(true);
			}
		}

		protected override void DeactivateImmediatelyEnter(){
			if(animator && animator.enabled) {
				animator.SetBool(parameterHash, false);
				animator.CrossFade(stateHash_Hidden, 0f, 0, 1);	// Skip animation and jump to last frame
			}
			else{
				gameObject.SetActive(false);
			}
		}
		#endregion

		virtual public void ToggleActivation(bool immediately = false) {
			if(immediately)
				ActivateImmediately(!isActivated);
			else
				Activate(!isActivated);
		}
	}
}

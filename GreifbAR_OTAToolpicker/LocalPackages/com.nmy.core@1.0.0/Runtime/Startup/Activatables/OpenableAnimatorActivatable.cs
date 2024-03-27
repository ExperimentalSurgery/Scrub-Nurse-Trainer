using System;
//using System.Collections.Generic;
using UnityEngine;

namespace NMY{
	public class OpenableAnimatorActivatable : SimpleAnimatorActivatable, IOpenable{
		public event EventHandler OpenEvent;
		public event EventHandler OpenedEvent;
		public event EventHandler CloseEvent;
		public event EventHandler ClosedEvent;
		public OpenableState openableState{get; protected set;}
		[SerializeField] protected OpenableState _initialOpenableState=OpenableState.Closed; // we want to modify this in the inspector
		public OpenableState initialOpenableState { get { return _initialOpenableState; }  }

		public bool canOpen = true;
		public string parameterOpenName = "open";
		protected int parameterOpenHash;
		static protected readonly int stateHash_Opened=Animator.StringToHash("Opened");
		static protected readonly int stateHash_Closed=Animator.StringToHash("Closed");

		protected override void StartupEnter(){
			base.StartupEnter();

			parameterOpenHash=Animator.StringToHash(parameterOpenName);
			if(initialOpenableState==OpenableState.Opened) {
				openableState=OpenableState.Closed; // trigger a change
				OpenImmediately();
			}else{
				openableState=OpenableState.Opened; // trigger a change
				CloseImmediately();
			}
		}

#region IOpenable
		virtual public void Open() {
			if (!canOpen || openableState == OpenableState.Opened) return;
			if (!isActivated){
				OpenImmediately(); // silently sets the state
				return;
			}
		
			RaiseOpenEvent();
			openableState=OpenableState.Opened; // do this at the beginning, to prevent multi-triggers

			if(animator && animator.enabled) {
				animator.SetBool(parameterOpenHash, true);
			}

			RaiseOpenedEvent();
		}

		virtual public void Close() {
			if (!canOpen || openableState == OpenableState.Closed) return;
			if (!isActivated){
				CloseImmediately(); // silently sets the state
				return;
			}
		
			RaiseCloseEvent();
			openableState=OpenableState.Closed; // do this at the beginning, to prevent multi-triggers

			if(animator && animator.enabled) {
				animator.SetBool(parameterOpenHash, false);
			}

			RaiseClosedEvent();
		}

		virtual public void OpenImmediately() {
			if (!canOpen || openableState == OpenableState.Opened) return;
		
			if (isActivated) // only spawn if we're activated
				RaiseOpenEvent();
			openableState=OpenableState.Opened;

			if(animator && animator.enabled) {
				animator.SetBool(parameterOpenHash, true);
				animator.CrossFade(stateHash_Opened, 0f);
			}

			if (isActivated) // only spawn if we're activated
				RaiseOpenedEvent();
		}
	
		virtual public void CloseImmediately() {
			if (!canOpen || openableState == OpenableState.Closed) return;
		
			if (isActivated) // only spawn if we're activated
				RaiseCloseEvent();
			openableState=OpenableState.Closed;
		
			if(animator && animator.enabled) {
				animator.SetBool(parameterOpenHash, false);
				animator.CrossFade(stateHash_Closed, 0f);
			}

			if (isActivated) // only spawn if we're activated
				RaiseClosedEvent();
		}
	
		virtual protected void RaiseOpenEvent() {
			if (OpenEvent!=null)
				OpenEvent(this, EventArgs.Empty);
		}
		
		virtual protected void RaiseOpenedEvent() {
			if (OpenedEvent!=null)
				OpenedEvent(this, EventArgs.Empty);
		}
		
		virtual protected void RaiseCloseEvent() {
			if (CloseEvent!=null)
				CloseEvent(this, EventArgs.Empty);
		}

		virtual protected void RaiseClosedEvent() {
			if (ClosedEvent!=null)
				ClosedEvent(this, EventArgs.Empty);
		}
#endregion

		// bool-version for Event callbacks (such as GUI etc.)
		public void Open(bool open){
			if(open)
				Open();
			else
				Close();
		}
		
		// bool-version for Event callbacks (such as GUI etc.)
		public void OpenImmediately(bool open){
			if(open)
				OpenImmediately();
			else
				CloseImmediately();
		}

		virtual public void ToggleOpenClose(bool immediately) {
			if(immediately)
				OpenImmediately(openableState==OpenableState.Closed);
			else
				Open(openableState==OpenableState.Closed);
		}
	}
}

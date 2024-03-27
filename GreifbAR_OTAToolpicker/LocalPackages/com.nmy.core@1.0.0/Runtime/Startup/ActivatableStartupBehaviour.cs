//#define NMYDEBUG
using UnityEngine;
using System;
using System.Collections;

namespace NMY {
	abstract public class ActivatableStartupBehaviour : StartupBehaviour, IActivatable {
	
		/// <summary>
		/// Raised when the behaviour activation has started.
		/// </summary>
		public event EventHandler ActivateEvent;
		
		/// <summary>
		/// Raised when the behaviour activation has finished.
		/// </summary>
		public event EventHandler ActivatedEvent;
		
		/// <summary>
		/// Raised when the behaviour deactivation has started.
		/// </summary>
		public event EventHandler DeactivateEvent;	
		
		/// <summary>
		/// Raised when the behaviour deactivation has finished.
		/// </summary>
		public event EventHandler DeactivatedEvent;			

		public class ASBEventArgs : EventArgs {
			public ActivatableStartupBehaviour obj;
		}
		public ASBEventArgs eventArgs;

		override protected void StartupExit() {
			InitActivation();
			base.StartupExit();
		}
		
		protected void InitActivation() {
			if (isInitiallyActivated) {
				_isActivated = false;
				ActivateImmediately();
			}
			else {
				_isActivated = true;
				DeactivateImmediately();
			}
		}
	
		#region IActivatable
	
		// **************************************************************************************************************
		// All possible states.
		// NOTE for the Immediately variants, _isTransitioning/isActivating/isDeactivating is never true
		// (well, technically they're true while *in* (De)ActivateImmediatelyEnter, but never outside an ASB class)
		// **************************************************************************************************************
		// State           | Phase               | _isActivated/isActivated | _isTransitioning/isTransitioning | isInactive | isActivating | isFullyActivated | isDeactivating
		// is off          | D(I)Exit->A(I)Enter | false                    | false                            | true       | false        | false            | false
		// is activating   | AEnter->AExit       | true                     | true                             | false      | true         | false            | false
		// is fully act.   | A(I)Exit->D(I)Enter | true                     | false                            | false      | false        | true             | false
		// is deactivating | DEnter->DExit       | false                    | true                             | false      | false        | false            | true
		// **************************************************************************************************************
		protected bool _isActivated = false;
		protected bool _isTransitioning = false;
		public bool isActivated			{ get { return  _isActivated;                      } }
		public bool isTransitioning		{ get { return                   _isTransitioning; } }
		public bool isInactive			{ get { return !_isActivated && !_isTransitioning; } }
		public bool isActivating		{ get { return  _isActivated &&  _isTransitioning; } }
		public bool isFullyActivated	{ get { return  _isActivated && !_isTransitioning; } }
		public bool isDeactivating		{ get { return !_isActivated &&  _isTransitioning; } }
		
		[SerializeField] private bool _isInitiallyActivated = false;
		virtual public bool isInitiallyActivated { 
			get { return _isInitiallyActivated; }
			set { _isInitiallyActivated = value; }
		}
		
		public void Activate() {
#if NMYDEBUG
			if(!hasStartedUp){
				Debug.LogError ("Node \""+name+"\" of type "+GetType()+" has not started up yet! Make sure to cleanly startup your objects!");
				Startup();
			}
#endif
			if (isActivated) return;
			SendActivateEvent();
			ActivateEnter();
			ActivateExit();
		}
		
		// bool-version for Event callbacks (such as GUI etc.)
		public void Activate(bool activate){
			if(activate)
				Activate();
			else
				Deactivate();
		}
		
		abstract protected void ActivateEnter();
		virtual protected void ActivateExit() {
			SendActivatedEvent();
		}

		public void Deactivate() {
#if NMYDEBUG
			if(!hasStartedUp){
				Debug.LogError ("Node \""+name+"\" of type "+GetType()+" has not started up yet! Make sure to cleanly startup your objects!");
				Startup();
			}
#endif
			if (!isActivated) return;
			SendDeactivateEvent();
			DeactivateEnter();
			DeactivateExit();
		}
		
		abstract protected void DeactivateEnter();
		virtual protected void DeactivateExit() {
			SendDeactivatedEvent();
		}
		
		public void ActivateImmediately() {
#if NMYDEBUG
			if(!hasStartedUp){
				Debug.LogError ("Node \""+name+"\" of type "+GetType()+" has not started up yet! Make sure to cleanly startup your objects!");
				Startup();
			}
#endif
			if (isFullyActivated) return;
			if(!isActivating) // if we're still in an Activate(), don't send the ActivateEvent a second time
				SendActivateEvent();
			ActivateImmediatelyEnter();
			ActivateImmediatelyExit();
		}

		// bool-version for Event callbacks (such as GUI etc.)
		public void ActivateImmediately(bool activate){
			if(activate)
				ActivateImmediately();
			else
				DeactivateImmediately();
		}

		abstract protected void ActivateImmediatelyEnter();
		virtual protected void ActivateImmediatelyExit() {
			SendActivatedEvent();
		}
	
		public void DeactivateImmediately() {
#if NMYDEBUG
			if(!hasStartedUp){
				Debug.LogError ("Node \""+name+"\" of type "+GetType()+" has not started up yet! Make sure to cleanly startup your objects!");
				Startup();
			}
#endif
			if (isInactive) return;
			if(!isDeactivating) // if we're still in a Deactivate(), don't send the DeactivateEvent a second time
				SendDeactivateEvent();
			DeactivateImmediatelyEnter();
			DeactivateImmediatelyExit();
		}
		
		abstract protected void DeactivateImmediatelyEnter();
		virtual protected void DeactivateImmediatelyExit() {
			SendDeactivatedEvent();
		}
	
		protected void SendActivateEvent() {
			_isActivated = true;
			_isTransitioning = true;
			if (ActivateEvent!=null) 
				ActivateEvent(this, eventArgs);
		}
		
		protected void SendActivatedEvent() {
			_isTransitioning = false;
			if (ActivatedEvent!=null) 
				ActivatedEvent(this, eventArgs);
		}
		
		protected void SendDeactivateEvent() {
			_isActivated = false;
			_isTransitioning = true;
			if (DeactivateEvent!=null) 
				DeactivateEvent(this, eventArgs);
		}
		
		protected void SendDeactivatedEvent() {
			_isTransitioning = false;
			if (DeactivatedEvent!=null)
				DeactivatedEvent(this, eventArgs);
		}
		
		// New methods for controlled stopping of item activation/deactivation. Mainly
		// called by parent renderers on all children renderers (e.g. a SphereListRenderer
		// might call this on all it's item renderers).
		// Usually this will have to stop/cancel all coroutines, iTweens, and animations:
		//			DelegateCoroutine.StopAll(this); // esp. for this.WaitAndExecute()
		//			iTween.Stop(gameObject);
		//			sourceAnimation.Stop();
		// The default implementation does nothing.
		virtual protected void StopActivate() {
			// We need to stop deactivation of all item renderers. To be able to do this
			// we need an interface method to this. Therefore we have to add an StopDeactivate()
			// and StopActivate() method to ActivatableStartupBehaviour baseclass.
		}
		virtual protected void StopDeactivate() {}
		#endregion IActivatable interface
	}
} // namespace NMY

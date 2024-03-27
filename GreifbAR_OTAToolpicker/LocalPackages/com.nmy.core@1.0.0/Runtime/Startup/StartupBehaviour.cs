using UnityEngine;
using System;
using System.Collections;

namespace NMY {

	/// <summary>
	/// The <c>StartupBehaviour</c> class is the abstract baseclass of all startup controlled classes.
	/// </summary>
	/// <remarks>
	/// <para>
	/// "Startup" is a concept to be able to control the order of <c>MonoBehaviour</c> initialization 
	/// in Unity3D. Unity3D ensures to call all <see cref="Awake" /> functions before any 
	/// <see cref="Start" /> function is called, but that's not enough to control initialization 
	/// of child objects which must be initialized before a parent object accesses them.
	/// </para>
	/// <para>
	/// The only solution we have found is to simply not use Unity3Ds <see cref="Start" /> or 
	/// <see cref="Awake" /> function for initialization. Instead we define our own initialization 
	/// function called <see cref="Startup" />. The <c>StartupBehaviour</c> class defines the   
	/// interface of the <see cref="Startup" /> function as well as an event 
	/// <see cref="StartedUpEvent" /> and a function for sending this event.
	/// </para>
	/// <para>
	/// When subclassing <c>StartupBehaviour</c> all your start code must be put into the 
	/// <see cref="StartupEnter"/> function and NOT into the default Unity <c>Start()</c> function. 
	/// When <see cref="autoStartup"/> is set to <c>true</c> the <see cref="Startup"/> function is 
	/// called automatically thereby simulating Unitys default starting mechanism.
	/// When <see cref="autoStartup"/> is set to <c>false</c> you can (and should) control the 
	/// starting mechanism by manually calling <see cref="Startup"/> depending on your 
	/// application's need.
	/// </para>
	/// </remarks>
	 
	abstract public class StartupBehaviour : MonoBehaviour {
		
		/// <summary>
		/// Must be sent by subclasses when the startup has finished. Can be sent using the 
		/// <see cref="SendStartedUpEvent" /> function or manually. The default implementation of 
		/// <see cref="StartupExit" /> sends the <see cref="StartedUpEvent" /> using the 
		/// <see cref="SendStartedUpEvent" /> method.
		/// </summary>
		public event EventHandler StartedUpEvent;	
	
		/// <summary>
		/// Indicates whether this instance is started up automatically, i.e. the 
		/// <see cref="Startup" /> method is automatically called from within the 
		/// <see cref="Start" /> function if <c>startupInAwake==false</c> or from withon the 
		/// <see cref="Awake" /> method if <c>startupInAwake==true</c>.
		/// </summary>
		public bool autoStartup = true;
		
		/// <summary>
		/// Indicates whether this instance is started up automatically within the
		/// <see cref="Awake" /> method but only if <c>autoStartup==true</c>.
		/// </summary>
		public bool startupInAwake = false;
	
		protected bool _hasStartedUp = false;
		
		/// <summary>
		/// Gets a value indicating whether this <see cref="NMY.StartupBehaviour"/> has started up.
		/// </summary>
		/// <returns>
		/// <c>true</c> if has started up; otherwise, <c>false</c>.
		/// </returns>
		public bool hasStartedUp { get { return _hasStartedUp; } }
	
		/// <summary>The standard Unity <c>Awake()</c> function.</summary>
		/// <remarks>
		/// WARNING! Make sure you know what you're doing when overriding this!
		/// Most importantly, always call <c>base.Awake()</c>!
		/// </remarks>
		protected virtual void Awake() {
			if (autoStartup && startupInAwake && enabled)
				Startup();
		}
		
		/// <summary>The standard Unity <c>Start()</c> function.</summary>
		/// <remarks>
		/// Do not override this function but rather implement your start code in 
		/// <see cref="StartupEnter" /> instead. 
		/// </remarks>
		void Start() {
			if (autoStartup) // startup unconditionally (we might have been disabled on Awake)
				Startup();
		}
	
		/// <summary>
		/// Startup this instance. Calls <see cref="StartupEnter" /> and <see cref="StartupExit" /> if the instance has not
		/// started up yet.
		/// </summary>
		public void Startup() {
			if (hasStartedUp) return;
			_hasStartedUp = true;
			StartupEnter();
			StartupExit();
		}
	
		/// <summary>
		/// Called by the <see cref="Startup" /> method. Subclasses must implement this to do 
		/// the actual startup.
		/// </summary>
		abstract protected void StartupEnter();
	
		/// <summary>
		/// Called at the end of the <see cref="Startup" /> method. Subclasses can implement 
		/// this to do startup stuff at the end of the <see cref="Startup" /> function. This 
		/// function is rarely used - if you re-implement it you must make sure to send the 
		/// <see cref="StartedUpEvent"/> either manually or using the 
		/// <see cref="SendStartedUpEvent" /> method.
		/// </summary>
		virtual protected void StartupExit() {
			SendStartedUpEvent();
		}
	
		/// <summary>
		/// Sends the <see cref="StartedUpEvent" />. After this method <c>hasStartedUp==true</c>.
		/// </summary>
		public void SendStartedUpEvent() {
			if (StartedUpEvent != null)
				StartedUpEvent(this, null);
		}
	}
} // namespace NMY

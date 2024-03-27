

using System;
using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace NMY.Tests {

	[TestFixture]
    // public class StartupBehaviour_Test<T> : TestCase where T : StartupBehaviour
	public class StartupBehaviour_Test 
    {
        protected UnityEngine.GameObject behaviourGO;
        protected TestStartupBehaviour behaviour;     // StartupBehviour instance for testing
		// private bool startedUpEventReceived;
		protected int nrOfReceivedStartedUpEvents;
	
		// NOTE: Unity executes a frame after the SetUp method. Therefore the Start() function
		//       will be called before any of the test functions is executed and we CANNOT
		//       create the GO and behaviour using the [SetUp] and [TearDown] attributes.
		// 		 Rather we call these functions from the test functions manually.
        public void SetUp()
        {						
            behaviourGO = new UnityEngine.GameObject("StartupBehaviour");
			behaviour = behaviourGO.AddComponent<TestStartupBehaviour>();			
			nrOfReceivedStartedUpEvents = 0;
        }

        public void TearDown()
        {
            UnityEngine.GameObject.DestroyImmediate(behaviourGO);
			behaviourGO = null;
			behaviour = null;
        }

        [Test]
        public void AutoStart_DefaultParam_IsTrue()
        {   			
			SetUp();
			// StartupBehaviour must have autoStartup==true by default
			Assert.True( behaviour.autoStartup );
			TearDown();
		}

		[Test]
        public void HasStartedUp_DefaultParam_IsFalse()
        {
			SetUp();
			Assert.False( behaviour.hasStartedUp );
			TearDown();
		}	
	

        [Test]
        public void Startup_Unstarted_HasStartedUpAfterStartup()
        {
			SetUp();

			// Make sure behaviour has not been started up yet
			Assert.True( !behaviour.hasStartedUp, "StartupBehaviour returns " +
				"hasStartedUp==true before Startup() of that behaviour was called.");
			
			behaviour.Startup();
			
			Assert.True( behaviour.hasStartedUp, 
				"A behaviour derived from StartupBehaviour must return " +
				"hasStartedUp==true after Startup() has been called.\nThis " +
				"can happen if StartupExit() is overloaded but the the override " +
				"implementation does not set _isStartedUp to true.\nYou must " +
				"set _isStartedUp=true manually or call the baseclass " +
				"implementation of the StartupExit() function.");

			TearDown();
		}

		
		[Test]
		public void Startup_Unstarted_SendsStartedUpEventAfterStartup() {
			SetUp();

			// Make sure the event wasn't sent already
			Assert.True( nrOfReceivedStartedUpEvents==0 );
			Assert.True( !behaviour.hasStartedUp );
			
			behaviour.StartedUpEvent += OnStartedUpEvent;
			behaviour.Startup();
			
			Assert.True( behaviour.hasStartedUp );
			Assert.True( nrOfReceivedStartedUpEvents>0, "The behaviour does not send " +
				"the StartedUpEvent. Every StartupBehaviour must send the " +
				"StartedUpEvent when the startup has finished." );
			
			behaviour.StartedUpEvent -= OnStartedUpEvent;

			TearDown();
		}
		
		/// <summary>
		/// Helper function for the Startup_UnstartedBehaviour_SendsStartedUpEventAfterStartup test.
		/// </summary>
		protected void OnStartedUpEvent(object sender, EventArgs args) {
			nrOfReceivedStartedUpEvents++;
		}
		

		[Test]
		public void Startup_Started_DoesNotSendEventWhenStartedUpAgain() {
			SetUp();

			// Make sure the event wasn't sent already
			Assert.True( nrOfReceivedStartedUpEvents==0 );
			Assert.True( !behaviour.hasStartedUp );
			
			behaviour.StartedUpEvent += OnStartedUpEvent;
			behaviour.Startup();
			
			Assert.True( behaviour.hasStartedUp );
			Assert.True( nrOfReceivedStartedUpEvents>0, "The behaviour does not send " +
				"the StartedUpEvent. Every StartupBehaviour must send the " +
				"StartedUpEvent when the startup has finished." );
			
			nrOfReceivedStartedUpEvents = 0;
			// Try to start it up again
			behaviour.Startup();
			// This time the startedUpEventReceived should not be modified again
			// because the behaviour has already started up.
			Assert.True( nrOfReceivedStartedUpEvents==0, "The behaviour does send the " + 
				"StartedUpEvent more than once (in this case twice).");
		
			behaviour.StartedUpEvent -= OnStartedUpEvent;	

			TearDown();
		}

		
		[Test]
		public void Startup_Unstarted_DoesSendStartedUpEventExactlyOnce() {
			SetUp();

			// Make sure the event wasn't sent already
			Assert.True( nrOfReceivedStartedUpEvents==0 );
			Assert.True( !behaviour.hasStartedUp );
			
			behaviour.StartedUpEvent += OnStartedUpEvent;
			behaviour.Startup();
			behaviour.StartedUpEvent -= OnStartedUpEvent;	
			
			Assert.True( behaviour.hasStartedUp );
			Assert.True( nrOfReceivedStartedUpEvents==1, "The behaviour does send " +
				"the StartedUpEvent more than once (in this case " + 
				nrOfReceivedStartedUpEvents + " times) in the Activate() method." );

			TearDown();
		}		
	
	}
	
} // namespace NMY.Tests

using System;
using NUnit.Framework;

namespace NMY.Tests {
	
	[TestFixture]
	public class ActivatableStartupBehaviour_Test 
    // public abstract class ActivatableStartupBehaviour_Test<T> : StartupBehaviour_Test<T> where T : ActivatableStartupBehaviour
    {
		protected UnityEngine.GameObject behaviourGO;
        protected TestActivatableStartupBehaviour behaviour; 

		protected int nrOfSentActivateEvents;
		protected int nrOfSentDeactivateEvents;
		
        public void SetUp()
        {
		 	behaviourGO = new UnityEngine.GameObject("ActivatableStartupBehaviour");
			behaviour = behaviourGO.AddComponent<TestActivatableStartupBehaviour>();	
			
			nrOfSentActivateEvents = 0;
			nrOfSentDeactivateEvents = 0;
        }

        public void TearDown()
        {
			UnityEngine.GameObject.DestroyImmediate(behaviourGO);
			behaviourGO = null;
			behaviour = null;
        }
		
		// The following tests test the IActivatable interface and should be 
		// somehow refactored out of this test into something reusable. 
		
		[Test]
		public void IsActivated_DefaultValue_IsFalse() {
			SetUp();
			Assert.False( behaviour.isActivated, "Expected false, got true. A " +
				"component implementing the IActivatable interface must have " +
				"isActivated==false before being activated.");
			TearDown();
		}
		
		[Test]
		public void IsActivated_IsInitiallyActivatedTrue_IsTrue() {
			SetUp();

			behaviour.isInitiallyActivated = true;
			behaviour.Startup();
			Assert.True( behaviour.isActivated, "A component implementing the " +
				"IActivatable interface must have isActivate==true after being " + 
				"started up with isInitiallyActive=true.");

			TearDown();
		}
		
		[Test]
		public void IsActivated_IsInitiallyActivatedFalse_IsFalse() {
			SetUp();

			behaviour.isInitiallyActivated = false;
			behaviour.Startup();
			Assert.False( behaviour.isActivated, "A component implementing the " +
				"IActivatable interface must have isActivate==false after being " + 
				"started up with isInitiallyActive=false.");

			TearDown();
		}	
		
		[Test]
		public void Activate_IsActivatedAfterActivate_IsTrue() {
			SetUp();

			behaviour.isInitiallyActivated = false;
			behaviour.Startup();
			behaviour.Activate();
			Assert.True( behaviour.isActivated, "A component implementing the " +
				"IActivatable interface must have isActivate==true after being " + 
				"activated using Activate().");
			
			TearDown();
		}	
		
		[Test]
		public void Deactivate_IsActivatedAfterDeactivate_IsFalse() {
			SetUp();

			behaviour.isInitiallyActivated = true;
			behaviour.Startup();
			Assert.True( behaviour.isActivated );
			behaviour.Deactivate();
			
			Assert.False( behaviour.isActivated, "A component implementing the " +
				"IActivatable interface must have isActivate==false after being " + 
				"deactivated using Deactivate().");

			TearDown();
		}	

		[Test]
		public void DeactivateImmedietaly_IsActivatedAfterDeactivate_IsFalse() {
			SetUp();

			behaviour.isInitiallyActivated = true;
			behaviour.Startup();
			Assert.True( behaviour.isActivated );
			behaviour.DeactivateImmediately();
			
			Assert.False( behaviour.isActivated, "A component implementing the " +
				"IActivatable interface must have isActivate==false after being " + 
				"deactivated using DeactivateImmediately().");

			TearDown();
		}	
		
		[Test]
		public void Activate_SendsActivateEvent_IsTrue() {
			SetUp();

			behaviour.isInitiallyActivated = false;
			behaviour.Startup();
			behaviour.ActivateEvent += OnBehaviourActivate;
			behaviour.Activate();
			behaviour.ActivateEvent -= OnBehaviourActivate;
			
			Assert.True( nrOfSentActivateEvents>0, "A component implementing the " +
				"IActivatable interface must sent the ActivateEvent " + 
				"during the execution of Activate().");			

			TearDown();
		}

		[Test]
		public void Activate_SendsExactlyOneActivateEvent_IsTrue() {
			SetUp();

			behaviour.isInitiallyActivated = false;
			behaviour.Startup();
			behaviour.ActivateEvent += OnBehaviourActivate;
			behaviour.Activate();
			behaviour.ActivateEvent -= OnBehaviourActivate;
			
			Assert.True( nrOfSentActivateEvents==1, "A component implementing the " +
				"IActivatable interface must sent exactly one ActivateEvent. Sent " +
				nrOfSentActivateEvents + " times.");			

			TearDown();
		}		
		
		[Test]
		public void Activate_SecondCallDoesntSendsActivatedEvent_IsTrue() {
			SetUp();

			behaviour.isInitiallyActivated = false;
			behaviour.Startup();
			behaviour.Activate();
			behaviour.ActivateEvent += OnBehaviourActivate;
			behaviour.Activate();
			behaviour.ActivateEvent -= OnBehaviourActivate;
			
			Assert.True( nrOfSentActivateEvents==0, "A component implementing the " +
				"IActivatable interface must sent the ActivateEvent " + 
				"only once when being activated. Already activated components must not "+
				"send the event again when Activate() is called again.");			

			TearDown();
		}		
		
		[Test]
		public void Deactivate_SendsDeactivateEvent_IsTrue() {
			SetUp();

			behaviour.isInitiallyActivated = true;
			behaviour.Startup();
			behaviour.DeactivateEvent += OnBehaviourDeactivate;
			behaviour.Deactivate();
			behaviour.DeactivateEvent -= OnBehaviourDeactivate;
			
			Assert.True( nrOfSentDeactivateEvents>0, "A component implementing the " +
				"IActivatable interface must sent the DeactivateEvent " + 
				"during the execution of Deactivate().");			

			TearDown();
		}
		
		[Test]
		public void Deactivate_SendsExactlyOneDeactivateEvent_IsTrue() {
			SetUp();

			behaviour.isInitiallyActivated = true;
			behaviour.Startup();
			behaviour.DeactivateEvent += OnBehaviourDeactivate;
			behaviour.Deactivate();
			behaviour.DeactivateEvent -= OnBehaviourDeactivate;
			
			Assert.True( nrOfSentDeactivateEvents==1, "A component implementing the " +
				"IActivatable interface must sent exactly one DeactivateEvent during a " +
				"call to Deactivate(). Sent " + nrOfSentDeactivateEvents + " times.");			
			
			TearDown();
		}	
		
		[Test]
		public void Deactivate_SecondCallDoesntSendDeactivateEvent_IsTrue() {
			SetUp();

			behaviour.isInitiallyActivated = true;
			behaviour.Startup();
			behaviour.Deactivate();
			behaviour.DeactivateEvent += OnBehaviourDeactivate;
			behaviour.Deactivate();
			behaviour.DeactivateEvent -= OnBehaviourDeactivate;
			
			Assert.True( nrOfSentDeactivateEvents==0, "A component implementing the " +
				"IActivatable interface must sent the DeactivateEvent " + 
				"only once when being deactivated. Already deactivated components must not "+
				"send the event again when Deactivate() is called again.");			

			TearDown();
		}
		
		[Test]
		public void DeactivateImmedietaly_SendsDeactivateEvent_IsTrue() {
			SetUp();

			behaviour.isInitiallyActivated = true;
			behaviour.Startup();
			behaviour.DeactivateEvent += OnBehaviourDeactivate;
			behaviour.DeactivateImmediately();
			behaviour.DeactivateEvent -= OnBehaviourDeactivate;
			
			Assert.True( nrOfSentDeactivateEvents>0, "A component implementing the " +
				"IActivatable interface must sent the DeactivateEvent " + 
				"during the execution of DeactivateImmediately().");			

			TearDown();
		}
		
		[Test]
		public void DeactivateImmedietaly_SendsExactlyOneDeactivateEvent_IsTrue() {
			SetUp();

			behaviour.isInitiallyActivated = true;
			behaviour.Startup();
			behaviour.DeactivateEvent += OnBehaviourDeactivate;
			behaviour.DeactivateImmediately();
			behaviour.DeactivateEvent -= OnBehaviourDeactivate;
			
			Assert.True( nrOfSentDeactivateEvents==1, "A component " +
				"implementing the IActivatable interface must sent exactly " +
				"one DeactivateEvent during a call to " +
				"DeactivateImmediately().\nSent " + nrOfSentDeactivateEvents + 
				" times.");			

			TearDown();
		}	
		
		
		[Test]
		public void DeactivateImmedietaly_SecondCallDoesntSendDeactivatedEvent_IsTrue() {
			SetUp();

			behaviour.isInitiallyActivated = true;
			behaviour.Startup();
			behaviour.DeactivateImmediately();
			behaviour.DeactivateEvent += OnBehaviourDeactivate;
			behaviour.DeactivateImmediately();
			behaviour.DeactivateEvent -= OnBehaviourDeactivate;
			
			Assert.True( nrOfSentDeactivateEvents==0, "A component implementing the " +
				"IActivatable interface must sent the DeactivateEvent " + 
				"only once when being deactivated. Already deactivated components must not "+
				"send the event again when DeactivateImmediately() is called again.");

			TearDown();
		}			
		
		protected void OnBehaviourActivate(object sender, EventArgs args) {
			nrOfSentActivateEvents++;
		}
		
		protected void OnBehaviourDeactivate(object sender, EventArgs args) {
			nrOfSentDeactivateEvents++;
		}
		
	}

}
using UnityEngine;
using System.Collections;

namespace NMY.Tests {
	
	public class TestActivatableStartupBehaviour : ActivatableStartupBehaviour {
	
		override protected void StartupEnter() {
		}
		
//		override protected void StartupExit() {
//			if (isInitiallyActivated) {
//				_isActivated = false;
//				Activate();
//			}
//			else {
//				_isActivated = true;
//				DeactivateImmediately();
//			}
//			SendStartedUpEvent();
//		}
		
		override protected void ActivateEnter() {
		}
		
		override protected void DeactivateEnter() {
		}
		
		override protected void ActivateImmediatelyEnter() {
		}
		
		override protected void DeactivateImmediatelyEnter() {
		}
		
	}

}

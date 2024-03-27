
using UnityEngine;
using System.Collections;
using NMY;

namespace NMY.Tests {
	
	public class TestStartupBehaviour : StartupBehaviour {
	
		override protected void StartupEnter() {}
		
		override protected void StartupExit() {
			SendStartedUpEvent();
		}
		
	}

}

using UnityEngine;
using System.Collections;

namespace NMY {

public class StartupController : MonoBehaviour {
	
	public StartupBehaviour[] startupBehaviour;
	//public float delay = 0f;
	
	void Start () {
		Startup();
	}
	
	// virtual public IEnumerator StartupCo() {
	//	yield return new WaitForSeconds(delay);
	
	virtual public void Startup() {
		//StartCoroutine(StartupCo());
		
		if (startupBehaviour.Length>0) {
			foreach (StartupBehaviour obj in startupBehaviour)
				if (obj && (obj.gameObject.activeSelf && obj.enabled)) {
					obj.Startup();
				}
		}
		else {
			Debug.LogError("Startup has no object to startup. Failed!");
		}		
	}
	
}

} // namespace
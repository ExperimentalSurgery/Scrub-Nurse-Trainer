using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// The DelegateCoroutineManager can be used to start and stop coroutines based on C# delegates
/// (anonymous functions).
/// 
/// Unity3Ds coroutines are not perfect as they cannot be stopped once they have been started 
/// using the non-string C# version of StartCoroutine().
/// 
/// In order to overcome this restriction the DelegateCoroutineManager and some extensions
/// methods can be used. The DelegateCoroutineManager can but is not intented to be accessed 
/// directly, normally it is used via the MonoBehaviour extension method StartDelegateCoroutine(),
/// which returns a DelegateCoroutine instance. This instance can be stopped at any given time
/// using the DelegateCoroutine.Stop() method.
/// 
/// Here's an example which starts a coroutine on the component s and immediately stops it
/// again. Note that stopping the coroutine just after starting it does not make much sense
/// but it's only here to show API usage:
/// <code>
/// // MyScript is a subclass of MonoBehaviour
/// MyScript s = go.GetComponent<MyScript>();
/// // start a managed delegate coroutine which prints "foo" after one second. The 
/// // coroutine is started via the extension methods which hides all the details.
/// DelegateCoroutine dc = s.StartDelegateCoroutine( 1f, () => { print("foo"); } );
/// // Instantly stop the coroutine (does not make much sense here, but shows how to do it)
/// dc.Stop();
/// </code>
/// </summary>
namespace NMY.DelegateCoroutines {

	public class DelegateCoroutineManager : MonoBehaviour {
				
		#region Singleton
		// NOTE(PE): Singleton code is implemented directly here instead of using a SingletonStartupBehaviour 
		// in order to get rid of the dependency to the future com.nmy.core package (which provides the 
		// SingletonStartupBehaviour). As some other scripts in Startup make use of DelegateCoroutines 
		// this would introduce a package dependency cycle which cannot be resolved.
		private static volatile DelegateCoroutineManager _instance = null;
		public static DelegateCoroutineManager instance {
			get{
				if(!_instance){
					_instance = FindObjectOfType(typeof(DelegateCoroutineManager)) as DelegateCoroutineManager;
					if(!_instance){
						GameObject go = new GameObject((typeof(DelegateCoroutineManager)).ToString()+" [Singleton]");
						_instance = go.AddComponent<DelegateCoroutineManager>();
						string className = _instance.ToString();
						go.name = className;
						Debug.Log("Added new "+className+" object to the scene since none existed.");
					}
					// if(_instance.autoStartup)
					// 	_instance.Startup(); // only startup if we're allowed to
				}
				return _instance;
			}
		}

		void Start(){
			#region Singleton
			if(_instance == null){
				_instance = this as DelegateCoroutineManager;
			}
			else if(_instance != this){
				Debug.Log("Destroying duplicate "+this.ToString()+" component from GameObject "+this.gameObject.name);
				Destroy(this);
				return;	// prevent further code execution
			}
			#endregion
		}

		public static bool Exists(){
			return _instance != null;
		}

		void OnDestroy(){
			_instance = null;
		}
		#endregion

		/// <summary>
		/// The list containing the running coroutines.
		/// Only public for debugging, should be private to prevent unwanted list modifications.
		/// </summary>
		public List<DelegateCoroutine> runningCoroutines = new List<DelegateCoroutine>(); 
	
		public DelegateCoroutine DoStart(MonoBehaviour b, float delay, bool realtime, System.Action a) {
			// Create a new game object and a DelegateCoroutine component which will be used to run the
			// coroutine in (Unity coroutines are run per component/mono behaviour).
			DelegateCoroutine e = DelegateCoroutine.Create();
			e.transform.SetParent(transform);
			e.sourceBehaviour = b;
			e.delay = delay;
			e.actionDelegate = a;
		
			runningCoroutines.Add(e);
			e.StartCoroutine(e.WaitAndExecuteCo(delay, realtime, a));
			return e;
		}
		
		public DelegateCoroutine DoStart(MonoBehaviour b, System.Action a) {
			// Create a new game object and a DelegateCoroutine component which will be used to run the
			// coroutine in (Unity coroutines are run per component/mono behaviour).
			DelegateCoroutine e = DelegateCoroutine.Create();
			e.transform.SetParent(transform);
			e.sourceBehaviour = b;
			e.actionDelegate = a;
		
			runningCoroutines.Add(e);
			e.StartCoroutine(e.WaitForEndOfFrameAndExecuteCo(a));
			return e;
		}
	
		/// <summary>
		/// Stop the specified delegate coroutine <paramref name="e"/>.
		/// </summary>
		/// <param name='e'>
		/// The delegate coroutine to stop. If the given coroutine is not running, nothing is done.
		/// </param>
		public void Stop(DelegateCoroutine e) {
			if (runningCoroutines.Contains(e))
				e.StopAllCoroutines();
		}
	
		/// <summary>
		/// Stops all DelegateCoroutines associated with a MonoBehaviour
		/// </summary>
		public void StopAll(MonoBehaviour b) {
			foreach(DelegateCoroutine dc in runningCoroutines)
				if(dc.sourceBehaviour==b)
					dc.StopAllCoroutines();
		}
	
		/// <summary>
		/// Removes the delegate coroutine <paramref name="e"/> from the list of running coroutines
		/// and destroys the game object containing the DelegateCoroutine behaviour.
		/// </summary>
		/// <param name='e'>
		/// The delegate coroutine to remove from the list of running coroutines.
		/// </param>
		public void RemoveEntry(DelegateCoroutine e) {
			if (runningCoroutines.Contains(e)) {
				runningCoroutines.Remove(e);
				Destroy(e.gameObject);
			}
		}
	
		/// <summary>
		/// Removes all DelegateCoroutines associated with a MonoBehaviour
		/// </summary>
		public void RemoveAll(MonoBehaviour b) {
			for(int i=runningCoroutines.Count-1;i>=0;i--)
				if(runningCoroutines[i].sourceBehaviour==b){
					Destroy(runningCoroutines[i].gameObject);
					runningCoroutines.RemoveAt(i);
				}
		}
	}
}

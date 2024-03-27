using UnityEngine;
using System.Collections;

/// <summary>
/// The DelegateCoroutine is an advanced version of Unity3D coroutines which can be stopped 
/// individually and which uses C# delegates (anonymous methods) instead of IEnumerator based 
/// methods.
/// 
/// A DelegateCoroutine is not intended to be instantiated directly but rather via the 
/// MonoBehaviour StartDelegateCoroutine() extension method. It can be accessed by all 
/// MonoBehaviour subclasses like this:
/// 
/// <code>
/// // MyScript is a baseclass of MonoBehaviour
/// MyScript s = go.GetComponent<MyScript>();
/// // start a managed delegate coroutine which prints "foo" after one second
/// DelegateCoroutine dc = s.StartDelegateCoroutine( 1f, () => { print("foo"); } );
/// </code>
/// A reference to the started DelegateCoroutine instance is now stored in the <c>dc</c> variable.
/// To stop the coroutine from the example above use the Stop() method of the returned
/// DelegateCoroutine instance:
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

	public class DelegateCoroutine : MonoBehaviour {
		/// <summary>
		/// The delay in seconds after which the <paramref name="actionDelegate"/> is exexuted.
		/// </summary>
		public float delay = 0f;
	
		/// <summary>
		/// The delegate to execute after waiting for <paramref name="delay"/> seconds.
		/// </summary>
		public System.Action actionDelegate;
	
		/// <summary>
		/// Reference to the MonoBehaviour instance which called the StartDelegateCoroutine()
		/// extension method.
		/// </summary>
		public MonoBehaviour sourceBehaviour;
	
		/// <summary>
		/// The actual coroutine method which is started by the DelegateRoutineManager.
		/// </summary>
		/// <param name='delay'>
		/// The delay in seconds after which the <paramref name="actionDelegate"/> is exexuted.
		/// </param>
		/// <param name='actionDelegate'>
		/// The delegate to execute after waiting for <paramref name="delay"/> seconds.
		/// </param>
		public IEnumerator WaitAndExecuteCo(float delay, bool realtime, System.Action actionDelegate) {
			if(realtime)
				yield return new WaitForSecondsRealtime(delay);
			else
				yield return new WaitForSeconds(delay);
			actionDelegate();
		
			// Remove the entry from the manager
			DelegateCoroutineManager.instance.RemoveEntry(this);
		}

		public IEnumerator WaitForEndOfFrameAndExecuteCo(System.Action actionDelegate) {
			yield return new WaitForEndOfFrame();
			actionDelegate();
		
			// Remove the entry from the manager
			DelegateCoroutineManager.instance.RemoveEntry(this);
		}

		/// <summary>
		/// Stops this delegate coroutine instance.
		/// </summary>
		public void Stop() {
			// Remove the entry from the manager
			DelegateCoroutineManager.instance.Stop(this);
			DelegateCoroutineManager.instance.RemoveEntry(this);
		}

		/// <summary>
		/// Stops all DelegateCoroutines associated with a MonoBehaviour
		/// </summary>
		/// <param name="b">The MonoBehaviour on which to stop the coroutines.</param>
		static public void StopAll(MonoBehaviour b){
			DelegateCoroutineManager.instance.StopAll(b);
			DelegateCoroutineManager.instance.RemoveAll(b);
		}

		static public DelegateCoroutine Create() {
			GameObject go = new GameObject("DelegateCoroutine");
			return go.AddComponent<DelegateCoroutine>();
		}	
	}

	public static class DelegateCoroutineExtensions {
	
		/// <summary>
		/// Extension method of the MonoBehaviour class to start a delegate coroutine.
		/// </summary>
		/// <returns>
		/// The started delegate coroutine.
		/// </returns>
		/// <param name='b'>
		/// The extension methods owner.
		/// </param>
		/// <param name='delay'>
		/// The delay in seonds after which to execute the action <paramref name="a"/>.
		/// </param>
		/// <param name='a'>
		/// The action to start after <paramref name="delay"/> seconds.
		/// </param>
		static public DelegateCoroutine StartDelegateCoroutine(this MonoBehaviour b, float delay, bool realtime, System.Action a) {
			return DelegateCoroutineManager.instance.DoStart(b, delay, realtime, a);
		}
	
		static public DelegateCoroutine StartDelegateCoroutine(this MonoBehaviour b, System.Action a) {
			return DelegateCoroutineManager.instance.DoStart(b, a);
		}

	}
}


/// <summary>
/// The WaitAndExecute extension methods are members of the NMY namespace (and not NMY.DelegateCoroutines) on 
/// purpose. Extension methods are namespace related, i.e. to be able to use an extension methods one must
/// be "using" the namespace it is contained in. 
/// </summary>
namespace NMY {

	using NMY.DelegateCoroutines;

	public static class DelegateCoroutineExtensions {
	
		// default behaviour: scaled game time
		static public DelegateCoroutine WaitAndExecute(this MonoBehaviour b, float delay, System.Action actionDelegate) {
			if(delay<=0){
				actionDelegate();
				return null;
			}
			return b.StartDelegateCoroutine(delay, false, actionDelegate);
		}
		
		static public DelegateCoroutine WaitForEndOfFrameAndExecute(this MonoBehaviour b, System.Action actionDelegate) {
			return b.StartDelegateCoroutine(actionDelegate);
		}

		// optional behaviour: unscaled time
		static public DelegateCoroutine WaitAndExecuteRealtime(this MonoBehaviour b, float delay, System.Action actionDelegate) {
			if(delay<=0){
				actionDelegate();
				return null;
			}
			return b.StartDelegateCoroutine(delay, true, actionDelegate);
		}

		// NOTE: PE 20191204 The following generic versions of WaitAndExecute have been commented out in NMY.cs before to the refactor.
		//        			 Moved them here to evaluate their usefullnes and possible reactivation sometime later.
		// 
		//		static public void WaitAndExecute<T>(this MonoBehaviour b, float delay, System.Action<T> a) {
		//			b.StartCoroutine(Extensions.WaitAndExecuteCo<T>(delay, a));
		//		}
		//		static private IEnumerator WaitAndExecuteCo<T>(float delay, System.Action<T> actionDelegate) {
		//			yield return new WaitForSeconds(delay);
		//			actionDelegate();
		//		}
	}

}
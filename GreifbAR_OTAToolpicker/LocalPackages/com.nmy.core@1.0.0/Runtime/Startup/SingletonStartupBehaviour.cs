using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace NMY {

	/// <summary>
	/// Baseclass for singletons which use the <see cref="StartupBehaviour" /> interface.
	/// </summary>
	/// <typeparam name="T">The class type for the singleton.</typeparam>
	/// <remarks>
	/// Derive from this class like so:
	/// <code>
	/// public class MySingleton : NMY.SingletonStartupBehaviour{MySingleton} {}
	/// </code>
	/// You can than access the singleton instance like so:
	/// <code>
	/// MySingleton ms = MySingleton.instance;
	/// </code>
	/// When you furthermore derive from <c>MySingleton</c>, then accessing the instance 
	/// property requires a cast. For example see this specialized singleton:
	/// <code>
	/// public class MySpecialSingleton : MySingleton {}
	/// </code>
	/// When you access the <c>MySpecialSingleton.instance</c> property it will still 
	/// return a reference to the base class <c>MySingleton</c>. Therefore an additional 
	/// cast is required:
	/// <code>
	/// // compiler error, will NOT work!
	/// MySpecialSingleton mss = MySpecialSingleton.instance;  
	/// // use cast, will work!
	/// MySpecialSingleton mss = MySpecialSingleton.instance as MySpecialSingleton;  
	/// </code>
	/// <para>
	/// Note that the cast might fail if there's a <c>MySingleton</c> or NO singleton 
	/// used in the scene. If no singleton is in the scene, accessing 
	/// <c>MySpecialSingleton.instance</c> will add a <c>MySingleton</c> instance to the 
	/// scene automatically, i.e. an instance of the baseclass and NOT a 
	/// <c>MySpecialSingleton</c> instance as it might be expected (this is due to 
	/// implemention details and cannot be avoided).
	/// </para>
	/// <para>
	/// It's recommended to always add the singleton to the scene manually to
	/// avoid automatic creation (until you really know what you're doing).
	/// </para>
	/// </remarks>
	abstract public class SingletonStartupBehaviour<T> : StartupBehaviour where T : SingletonStartupBehaviour<T>{
		#region Singleton
		private static volatile T _instance = null;
		public static T instance{
			get{
				if(!_instance){
					_instance = FindObjectOfType(typeof(T)) as T;
					if(!_instance){
						GameObject go = new GameObject((typeof(T)).ToString()+" [Singleton]");
						_instance = go.AddComponent<T>();
						string className = _instance.ToString();
						go.name = className;
						Debug.Log("Added new "+className+" object to the scene since none existed.");
					}
					if(_instance.autoStartup)
						_instance.Startup(); // only startup if we're allowed to
				}
				return _instance;
			}
		}
		#endregion

		protected override void StartupEnter(){
			#region Singleton
			if(_instance == null){
				_instance = this as T;
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
	}

} // namespace

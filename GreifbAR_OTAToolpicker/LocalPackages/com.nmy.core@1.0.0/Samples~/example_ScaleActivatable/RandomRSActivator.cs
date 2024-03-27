using UnityEngine;


namespace NMY.Tests {

	/// <summary>
	/// Randomly toggle activation of any RecursiveScaleActivator in this subtree.
	/// </summary>
	public class RandomRSActivator : RandomActivator {

		override protected void StartupEnter(){
			// find only RecursiveScaleActivators instead of all
			// how can this be done via Reflection convenience methods, instead of deriving from the whole class?
			activatables=GetComponentsInChildren<RecursiveScaleActivator>();
		}
	}

} // namespace

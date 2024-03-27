using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace NMY {

/// <summary>
/// An <see cref="ActivatableStartupBehaviour"/> with RadioButton behaviour, making sure only one element at a time is visible.
/// </summary>
/// <remarks>
/// - will automatically attach <see cref="SimpleFadeableBehaviour"/> or <see cref="SimpleOnOffActivatable"/> to all direct children without an ASB.
/// - changing <paramref name="currentItem"/> will cross-fade between previous and new entry, unless we are deactivated
/// - setting <paramref name="currentItem"/> to -1 will disable all children (while keeping ourself activated)
///   TODO: actually that's not fully implemented at the moment
/// - initially setting <paramref name="currentItem"/> to >=0 will activate that item on startup
/// </remarks>
public class RadioBehaviour : ActivatableStartupBehaviour {
	
	public List<ActivatableStartupBehaviour> radioChildren=new List<ActivatableStartupBehaviour>(); // NOTE: do not initialize in inspector! auto-filled
	[SerializeField] private int _currentItem=0;
	public int currentItem {
		get { return _currentItem; }
		set {
			_currentItem=value;
			if(_isActivated)
				ActivateCurrent();
		}
	}
	public string currentNamedItem {
		//get { return _currentNamedItem; }
		set {
			if(namedIndexDic.ContainsKey(value))
				currentItem=namedIndexDic[value];
			else
				Debug.LogError("Key \""+value+"\" not found!");
		}
	}
	public bool isFadeable=true; // only relevant for children with no ASB attached. true: add SimpleFadeableBehaviour. false: add OnOffActivatable.
	public float fadeDuration=1; // 0: immediately

	protected Dictionary<string,int> namedIndexDic=new Dictionary<string, int>();

	override protected void StartupEnter(){
//		gameObject.SetActiveRecursively(true); ///< enable all, so we can find inactive children later on
		radioChildren=new List<ActivatableStartupBehaviour>();

		foreach(Transform child in transform)
			AddChild(child.gameObject);
	}
	
	public void AddChild(ActivatableStartupBehaviour radioChild){
		radioChildren.Add(radioChild);
		namedIndexDic[radioChild.name]=radioChildren.Count-1;
	}
	
	public void AddChild(GameObject go){
		ActivatableStartupBehaviour radioChild=go.GetComponent<ActivatableStartupBehaviour>(); // somebody attached one manually? use it!
		if(!radioChild) {
			if(isFadeable){
				radioChild=go.AddComponent<SimpleFadeableBehaviour>();
				(radioChild as SimpleFadeableBehaviour).fadeDuration=fadeDuration;
			}else{
				radioChild=go.AddComponent<SimpleOnOffActivatable>();
			}
		}
		radioChild.autoStartup=false;
		radioChild.isInitiallyActivated=(_currentItem==radioChildren.Count);
		radioChild.Startup();
		AddChild(radioChild);
	}
	
	public void SubChild(ActivatableStartupBehaviour radioChild){
		if(!radioChildren.Contains(radioChild)){
			Debug.LogError($"RadioBehaviour {gameObject.name} does not contain child {radioChild.name}!");
			return;
		}

		if(radioChild.isActivated)
			currentItem=-1; // this is fine. in the resulting RadioBehaviour, no child will be active. also, this is a setter, and will call ActivateCurrent() immediately.
		
		radioChildren.Remove(radioChild);

		// need to rebuild index
		namedIndexDic.Clear();
		for(int i=0;i<radioChildren.Count;i++)
				namedIndexDic[radioChildren[i].name]=i;
	}
	
	#region IActivatable
	override protected void ActivateEnter() {
		ActivateCurrent();
	}
	
	override protected void DeactivateEnter() {
		foreach(ActivatableStartupBehaviour asb in radioChildren)
			asb.Deactivate();
	}
	
	override protected void ActivateImmediatelyEnter() {
		ActivateCurrent(true);
	}
	
	override protected void DeactivateImmediatelyEnter() {
		foreach(ActivatableStartupBehaviour asb in radioChildren)
			asb.DeactivateImmediately();
	}

	private void ActivateCurrent(bool immediately=false){
		immediately=immediately || (fadeDuration==0);
		foreach(ActivatableStartupBehaviour asb in radioChildren){
			bool activate=
					_currentItem>=0 && _currentItem<radioChildren.Count &&
					asb==radioChildren[_currentItem];
			if(immediately)
				asb.ActivateImmediately(activate);
			else
				asb.Activate(activate);
		}
	}
	#endregion	
}

}
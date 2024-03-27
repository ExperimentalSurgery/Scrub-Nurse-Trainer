using UnityEngine;
using UnityEngine.UI;
using NMY;
using NMY.DelegateCoroutines;

/// <summary>
/// Simple demo/test for the DelegateCoroutine and DelegateCoroutineManager classes.
/// </summary>
public class TestDelegateCoroutine : MonoBehaviour {

	// Use this for initialization
	void Start () {
		Example1Init();

		Time.timeScale=0.5f;

		// Start two delegate coroutines and stop one immediately.
		// Both coroutines print to the console after three seconds, but
		// the output of dc2 will never be printed because it is stopped
		// before the coroutine is executed.
		this.StartDelegateCoroutine(3f, false, () => {
			print (Time.realtimeSinceStartup+" I am DelegateCoroutine instance1.");
		} );
		
		DelegateCoroutine dc2 = this.StartDelegateCoroutine(3f, false, () => {
			print (Time.realtimeSinceStartup+" I am DelegateCoroutine instance2.");
		} );
		
		// Stop second coroutine, output will never be printed
		dc2.Stop();

		// do the same for unscaled game time
		this.StartDelegateCoroutine(3f, true, () => {
			print (Time.realtimeSinceStartup+" I am unscaled DelegateCoroutine instance1.");
		} );
		
		dc2 = this.StartDelegateCoroutine(3f, true, () => {
			print (Time.realtimeSinceStartup+" I am unscaled DelegateCoroutine instance2.");
		} );
		
		// Stop second coroutine, output will never be printed
		dc2.Stop();
	}

	[Header("Example 1 - StartDelegateCoroutine")]
	// public float example1WaitTime = 3f;
	public Button example1StartBtn;
	public Button example1StopBtn;
	public Text example1Output;
	public InputField example1WaitTimeInput;
	public DelegateCoroutine example1DelegateCoroutine;
	protected void Example1Init() {

	}
	public void StartExample1() {
		float t1 = Time.time;
		example1Output.text = "StartDelegateCoroutine called at time " + t1;
		float example1WaitTime = float.Parse(example1WaitTimeInput.text);
		example1StartBtn.interactable = false;
		example1StopBtn.interactable = true;

		example1DelegateCoroutine = this.StartDelegateCoroutine(example1WaitTime, true, () => {			
			string text = example1Output.text;
			text += "\nOutput of example 1 at time " + Time.time;
			example1Output.text = text;
			example1StartBtn.interactable = true;			
			example1StopBtn.interactable = false;
		});
	}

	public void StopExample1() {
		if (example1DelegateCoroutine!=null) {
			example1DelegateCoroutine.Stop();
			example1StartBtn.interactable = true;			
			example1StopBtn.interactable = false;
			string text = example1Output.text;
			text += "\nExample 1 stopped at time " + Time.time;
			example1Output.text = text;
		}
	}

	[Header("Example 2 - WaitAndExecute")]
	// public float example1WaitTime = 3f;
	public Button example2StartBtn;
	public Button example2StopBtn;
	public Text example2Output;
	public InputField example2WaitTimeInput;
	public DelegateCoroutine example2DelegateCoroutine;

	public void StartExample2() {
		float t1 = Time.time;
		example2Output.text = "WaitAndExecute called at time " + t1;
		float example2WaitTime = float.Parse(example2WaitTimeInput.text);
		example2StartBtn.interactable = false;
		example2StopBtn.interactable = true;

		example2DelegateCoroutine = this.WaitAndExecute(example2WaitTime, () => {			
			string text = example2Output.text;
			text += "\nOutput of example 2 at time " + Time.time;
			example2Output.text = text;
			example2StartBtn.interactable = true;			
			example2StopBtn.interactable = false;
		});
	}

	public void StopExample2() {
		if (example2DelegateCoroutine!=null) {
			example2DelegateCoroutine.Stop();
			example2StartBtn.interactable = true;			
			example2StopBtn.interactable = false;
			string text = example2Output.text;
			text += "\nExample 2 stopped at time " + Time.time;
			example2Output.text = text;
		}
	}
	
}

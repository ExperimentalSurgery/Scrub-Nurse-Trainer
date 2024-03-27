# NMY Delegate Coroutines

## Summary
Unity3Ds coroutines are not perfect as they cannot be stopped once they have 
been started using the non-string C# version of `StartCoroutine()`.

The `DelegateCoroutineManager` can be used to start and stop coroutines based on 
C# delegates (anonymous functions).

## Details
In order to overcome the restrictions of Unitys coroutines the 
`DelegateCoroutineManager` and some extensions methods can be used. The 
`DelegateCoroutineManager` is not intented to be accessed directly, 
normally it is used via the `MonoBehaviour` extension method 
`StartDelegateCoroutine()`, which returns a `DelegateCoroutine` instance. This 
instance can be stopped at any given time using the `DelegateCoroutine.Stop()` 
method.
There's another set of extension methods in the NMY namespace called 
`WaitAndExecute()` which is functionally equivalent to the 
`StartDelegateCoroutine()` method.

## Example: StartDelegateCoroutine
Here's an example which starts a coroutine on the component s and immediately 
stops it again. Note that stopping the coroutine just after starting it does 
not make much sense but it's only here to show API usage:
```C#
// MyScript is a subclass of MonoBehaviour
MyScript s = go.GetComponent<MyScript>();
// start a managed delegate coroutine which prints "foo" after one second. 
// The coroutine is started via the extension methods which hides all the 
// details.
DelegateCoroutine dc = s.StartDelegateCoroutine( 1f, () => { print("foo"); } );
// Instantly stop the coroutine (does not make much sense here, but shows 
// how to do it)
dc.Stop();
```

## Example: WaitAndExecute
This method waits for the given amount of time and then executes the given
anonymous function. It **does** respect `Time.timeScale`.
```C#
// MyScript is a subclass of MonoBehaviour
MyScript s = go.GetComponent<MyScript>();
// start a managed delegate coroutine which prints "foo" after one second. 
// The coroutine is started via the extension methods which hides all the 
// details.
DelegateCoroutine dc = s.WaitAndExecute( 1f, () => { print("foo"); } );
// Instantly stop the coroutine (does not make much sense here, but shows 
// how to do it)
dc.Stop();
```

## Example: WaitAndExecuteRealtime
This method waits for the given amount of time and then executes the given
anonymous function. It **does not** respect `Time.timeScale`.
```C#
// MyScript is a subclass of MonoBehaviour
MyScript s = go.GetComponent<MyScript>();
// start a managed delegate coroutine which prints "foo" after one second. 
// The coroutine is started via the extension methods which hides all the 
// details.
DelegateCoroutine dc = s.WaitAndExecuteRealtime( 1f, () => { print("foo"); } );
// Instantly stop the coroutine (does not make much sense here, but shows 
// how to do it)
dc.Stop();
```

## Example: WaitForEndOfFrameAndExecute
```C#
// MyScript is a subclass of MonoBehaviour
MyScript s = go.GetComponent<MyScript>();
// start a managed delegate coroutine which prints "foo" after the end of frame. 
// Note that we are not using the DelegateCoroutine returned by the method (we
// could if we wanted to). 
s.WaitForEndOfFrameAndExecute( () => { 
    print("foo"); 
});
```
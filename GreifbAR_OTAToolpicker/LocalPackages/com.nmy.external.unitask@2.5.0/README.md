# External: UniTask

Provides an efficient allocation free async/await integration for Unity.

- Struct based UniTask<T> and custom AsyncMethodBuilder to achieve zero allocation
- Makes all Unity AsyncOperations and Coroutines awaitable
- PlayerLoop based task(UniTask.Yield, UniTask.Delay, UniTask.DelayFrame, etc..) that enable replacing all coroutine operations
- MonoBehaviour Message Events and uGUI Events as awaitable/async-enumerable
- Runs completely on Unity's PlayerLoop so doesn't use threads and runs on WebGL, wasm, etc.
- Asynchronous LINQ, with Channel and AsyncReactiveProperty
- TaskTracker window to prevent memory leaks
- Highly compatible behaviour with Task/ValueTask/IValueTaskSource

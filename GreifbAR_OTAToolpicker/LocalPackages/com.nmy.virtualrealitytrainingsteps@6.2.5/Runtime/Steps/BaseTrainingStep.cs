using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using Unity.Netcode;
using UnityEditor;
using UnityEngine;
using UnityEngine.Serialization;

namespace NMY.VirtualRealityTraining.Steps
{
    /// <summary>
    /// An abstract class for defining and managing synchronized training steps in a networked environment.
    /// This class is a <see cref="NetworkBehaviour"/> and is used to define a step that is comprised of
    /// multiple activatable game objects, persistant activatable game objects that are not deactivated by the step,
    /// and a set of other training steps that should be activated after this step is finished.
    /// The class also has properties for defining timeout periods for various step-related events and has events
    /// that can be subscribed to for handling step-related events such as when the step starts, finishes,
    /// and completes, as well as when the activatable game objects are activated and deactivated.
    /// 
    /// This class should be subclassed and the `StepAction` methods should be overridden to define the actions
    /// and behaviors specific to each training step.
    /// </summary>
    [RequireComponent(typeof(NetworkObject))]
    [DefaultExecutionOrder(-50)]
    public abstract class BaseTrainingStep : NetworkBehaviour
    {
        [Tooltip("Use this field to express what this step listener is doing. This field will not be used otherwise.")]
        [SerializeField] [TextArea(3, 6)] private string _developerDescription;

        /// <summary>
        /// A list of GameObjects that will be activated when this step has started and deactivated when it has finished.
        /// </summary>
        [SerializeField] private List<GameObject> _stepActivatables = new();

        /// <summary>
        /// A list of GameObjects that will be activated when this step has started and remain activated after it has
        /// finished until they get deactivated by another step in <see cref="DeactivatePersistantStepsAsync"/>.
        /// </summary>
        [SerializeField] private List<GameObject> _persistantActivatables = new();

        /// <summary>
        /// A list of steps whose persistant activatables will be deactivated when this step is finished.
        /// </summary>
        [SerializeField] private List<BaseTrainingStep> _persistantActivatablesDeactivationSteps = new();

        /// <summary>
        /// A list of steps that come after this step in the training sequence.
        /// </summary>
        [SerializeField] private List<BaseTrainingStep> _nextSteps = new();

#region Timeouts
        
        /// <summary>
        /// The amount of time to wait before starting this step.
        /// </summary>
        [SerializeField] private float _timeoutBeforeStepStart;

        /// <summary>
        /// The amount of time to wait before finishing this step.
        /// </summary>
        [SerializeField] private float _timeoutBeforeStepFinish;

        /// <summary>
        /// The amount of time to wait before completing this step.
        /// </summary>
        [FormerlySerializedAs("_timeoutBeforeCompleting")]
        [SerializeField] private float _timeoutBeforeStepComplete;

        /// <summary>
        /// The amount of time to wait before activating the activatable GameObjects.
        /// </summary>
        [FormerlySerializedAs("_timeoutBeforeActivatablesActivating")]
        [FormerlySerializedAs("_timeoutBeforeActivating")]
        [SerializeField] private float _timeoutActivatablesActivating;
        
        /// <summary>
        /// The amount of time to wait after activating the activatable GameObjects.
        /// </summary>
        [SerializeField] private float _timeoutActivatablesActivated;

        /// <summary>
        /// The amount of time to wait before deactivating the activatable GameObjects.
        /// </summary>
        [FormerlySerializedAs("_timeoutBeforeActivatablesDeactivating")]
        [FormerlySerializedAs("_timeoutBeforeDeactivating")]
        [SerializeField] private float _timeoutActivatablesDeactivating;

        /// <summary>
        /// The amount of time to wait after deactivating the activatable GameObjects.
        /// </summary>
        [SerializeField] private float _timeoutActivatablesDeactivated;

        /// <summary>
        /// The amount of time to wait before activating the persistant activatable GameObjects.
        /// </summary>
        [SerializeField] private float _timeoutPersistantActivatablesActivating;
        
        /// <summary>
        /// The amount of time to wait after activating the persistant activatable GameObjects.
        /// </summary>
        [SerializeField] private float _timeoutPersistantActivatablesActivated;

        /// <summary>
        /// The amount of time to wait before deactivating the persistant activatable GameObjects.
        /// </summary>
        [SerializeField] private float _timeoutPersistantActivatablesDeactivating;

        /// <summary>
        /// The amount of time to wait after deactivating the persistant activatable GameObjects.
        /// </summary>
        [SerializeField] private float _timeoutPersistantActivatablesDeactivated;
#endregion

        /// <summary>
        /// Whether to wait for all clients to finish this step before moving on to the next step.
        /// </summary>
        [SerializeField] protected bool _waitForAllClientsToFinish = true;

        /// <summary>
        /// A set of all clients who have completed this step.
        /// </summary>
        protected HashSet<ulong> finishedClientsSet = new();

        /// <summary>
        /// The number of clients who have finished this step.
        /// </summary>
        protected NetworkVariable<int> finishedClients = new();

        /// <summary>
        /// The total number of clients in the network.
        /// </summary>
        protected NetworkVariable<int> totalClients = new();

        /// <summary>
        /// A CancellationTokenSource used to cancel any ongoing step actions.
        /// </summary>
        private CancellationTokenSource _stepActionTokenSource = new();

        /// <summary>
        /// An enumeration representing the state of the step.
        /// </summary>
        [Flags]
        public enum StepState
        {
            StepWaiting,
            StepStarted,
            StepFinished,
            StepCompleted,
            StepStopped
        }

        /// <summary>
        /// The current state of the step.
        /// </summary>
        protected NetworkVariable<StepState> _stepState = new();

        /// <summary>
        /// Gets the current state of the step.
        /// </summary>
        public StepState stepState => _stepState.Value;
        
        /// <summary>
        /// A list of steps that come after this step in the training sequence.
        /// </summary>
        protected List<BaseTrainingStep> nextSteps => _nextSteps;

#region Events

        /// <summary>
        /// An event that is raised when the step state changes.
        /// </summary>
        public static event EventHandler<BaseTrainingStepEventArgs> OnStepChanged;

        /// <summary>
        /// An event that is raised when the step is started.
        /// </summary>
        public event EventHandler<BaseTrainingStepEventArgs> OnStepStarted;

        /// <summary>
        /// An event that is raised when the step is finished.
        /// </summary>
        public event EventHandler<BaseTrainingStepEventArgs> OnStepFinished;

        /// <summary>
        /// An event that is raised when the step is completed.
        /// </summary>
        public event EventHandler<BaseTrainingStepEventArgs> OnStepCompleted;

        /// <summary>
        /// An event that is raised when a step action is started.
        /// </summary>
        public event EventHandler<BaseTrainingStepEventArgs> OnStepActionStarted;

        /// <summary>
        /// An event that is raised when a step action is finished.
        /// </summary>
        public event EventHandler<BaseTrainingStepEventArgs> OnStepActionFinished;
        
        /// <summary>
        /// An event that is raised right before the activatable GameObjects are activated.
        /// </summary>
        public event EventHandler<BaseTrainingStepEventArgs> OnActivatablesActivating;

        /// <summary>
        /// An event that is raised when the activatable GameObjects are activated.
        /// </summary>
        public event EventHandler<BaseTrainingStepEventArgs> OnActivatablesActivated;
        
        /// <summary>
        /// An event that is raised right before the activatable GameObjects are deactivated.
        /// </summary>
        public event EventHandler<BaseTrainingStepEventArgs> OnActivatablesDeactivating;

        /// <summary>
        /// An event that is raised when the activatable GameObjects are deactivated.
        /// </summary>
        public event EventHandler<BaseTrainingStepEventArgs> OnActivatablesDeactivated;

        /// <summary>
        /// An event that is raised right before the persistant activatable GameObjects are activated.
        /// </summary>
        public event EventHandler<BaseTrainingStepEventArgs> OnPersistantActivatablesActivating;
        
        /// <summary>
        /// An event that is raised when the persistant activatable GameObjects are activated.
        /// </summary>
        public event EventHandler<BaseTrainingStepEventArgs> OnPersistantActivatablesActivated;
        
        /// <summary>
        /// An event that is raised right before the persistant activatable GameObjects are deactivated.
        /// </summary>
        public event EventHandler<BaseTrainingStepEventArgs> OnPersistantActivatablesDeactivating;

        /// <summary>
        /// An event that is raised when the persistant activatable GameObjects are deactivated.
        /// </summary>
        public event EventHandler<BaseTrainingStepEventArgs> OnPersistantActivatablesDeactivated;

#endregion


#if UNITY_EDITOR
        /// <summary>
        /// Replace the _nextSteps with a new List.
        /// </summary>
        public void OverrideNextSteps(List<BaseTrainingStep> newSteps){
            if(Application.isPlaying) // only allowed in Edit mode
                return;
            Undo.RecordObject(this, "Override Next Steps"); // setup Unity Undo BEFORE the modification
            _nextSteps = newSteps;
            PrefabUtility.RecordPrefabInstancePropertyModifications(this); // mark any Prefab overrides AFTER the modification
        }
#endif

        protected virtual void Awake()
        {
        }

        protected virtual async void Start()
        {
            await UniTask.WhenAll(
                DeactivateActivatablesAsync(this.GetCancellationTokenOnDestroy(), false),
                DeactivatePersistantActivatablesAsync(this.GetCancellationTokenOnDestroy(), false));
        }

        /// <summary>
        /// This method is called when the networked component is spawned.
        /// </summary>
        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            if (IsServer)
            {
                totalClients.Value    = NetworkManager.ConnectedClients.Count;
                finishedClients.Value = 0;

                NetworkManager.OnClientConnectedCallback  += UpdateTotalClients;
                NetworkManager.OnClientDisconnectCallback += UpdateTotalClients;
            }
        }

        /// <summary>
        /// This method is called when the networked component is destroyed.
        /// </summary>
        public override void OnNetworkDespawn()
        {
            base.OnNetworkDespawn();
            if (IsServer)
            {
                NetworkManager.OnClientConnectedCallback  -= UpdateTotalClients;
                NetworkManager.OnClientDisconnectCallback -= UpdateTotalClients;
            }
        }

        /// <summary>
        /// updates the total number of clients currently connected to the network.
        /// This method is called on the server whenever a new client connects or an existing client disconnects from the network.
        /// It is used to ensure that the server knows how many clients are connected to the network at any given time,
        /// which is necessary for ensuring that the synchronized training steps are completed by all clients before
        /// moving on to the next step.
        /// </summary>
        /// <param name="clientID"></param>
        private async void UpdateTotalClients(ulong clientID)
        {
            await UniTask.NextFrame();
            totalClients.Value = NetworkManager.ConnectedClients.Count;
        }


#region Network Callbacks

        /// <summary>
        /// Called when a client is disconnected from the server. It removes the disconnected client from the set of finished clients
        /// and checks for the step completeness.
        /// </summary>
        /// <param name="clientID">The ID of the disconnected client.</param>
        /// <seealso cref="CheckStepCompletenessServer"/>
        private async void OnClientDisconnected(ulong clientID)
        {
            if (!IsServer) return;

            // Remove the disconnected clients from the set if present.
            if (finishedClientsSet.Contains(clientID))
            {
                finishedClientsSet.Remove(clientID);
                finishedClients.Value = finishedClientsSet.Count;
            }

            await UniTask.NextFrame();
            CheckStepCompletenessServer();
            //
            //
            // // NetworkManager.Singleton.ConnectedClients.Count is not changed at this point, we have to wait until the end of the frame
            // await new WaitForEndOfFrame();
            // if (_stepState.Value.HasFlag(StepState.StepStarted | StepState.StepFinished)) 
            //     CheckStepCompletenessServer();
        }

        /// <summary>
        /// Checks if all connected clients have finished their step, and if so it sets the step state to finished.
        /// If <see cref="_waitForAllClientsToFinish"/> is false and at least one client has finished their step,
        /// the method cancels the step action of all other clients.
        /// </summary>
        /// <returns>Returns whether or not all connected clients have finished the step.</returns>
        private bool CheckStepCompletenessServer()
        {
            if (!IsServer) return false;

            // If _waitForAllClientsToFinish is false, the first client that finishes this step raises the cancel event
            // for all other clients and their stepActionTask. The server waits until each client received the cancel event
            // and the StepCompleted event of each client.
            if (!_waitForAllClientsToFinish && finishedClientsSet.Count >= 1)
            {
                // StepAction of the Server must also be canceled
                if (!IsHost) _stepActionTokenSource.Cancel();
                
                CancelStepByServer_ClientRpc();
                // return true;
            }

            // Wait until all connected clients finishes their step to proceed further
            if (finishedClientsSet.SetEquals(NetworkManager.ConnectedClients.Keys))
            {
                _stepState.Value = StepState.StepFinished;
                return true;
            }

            return false;
        }

        /// <summary>
        /// <b>Client Rpc</b><br/>
        /// Cancels the step action of this client from the server. Called by <see cref="CheckStepCompletenessServer"/>.
        /// </summary>
        [ClientRpc(Delivery = RpcDelivery.Reliable)]
        private void CancelStepByServer_ClientRpc()
        {
            _stepActionTokenSource.Cancel();
        }

#endregion

        /// <summary>
        /// <para>
        /// This method is called when a training step is started.
        /// In the base implementation, StartStep activates the <see cref="_stepActivatables"/> and
        /// <see cref="_persistantActivatables"/> after the specified timeout.
        /// It calls the StepAction methods <see cref="PreStepActionAsync"/>, <see cref="ClientStepActionAsync"/>,
        /// <see cref="ServerStepActionAsync"/>, and <see cref="PostStepActionAsync"/> for step specific actions.
        /// It then waits for the specified timeout before finishing the step,
        /// and then waits for the specified timeout before completing the step.
        /// </para>
        /// <para>
        /// If <see cref="_waitForAllClientsToFinish"/> is true, StartStep will wait for all clients to finish the step
        /// before finishing it; otherwise, StartStep will finish the step as soon as the first client finishes it.
        /// </para>
        /// <para>
        /// Once the step is finished, StartStep will deactivate <see cref="_stepActivatables"/> and <see cref="_persistantActivatables"/>,
        /// and then call recursively this method to start the next steps.
        /// </para>
        /// </summary>
        /// <param name="stepToken">Instance of <see cref="CancellationToken"/> to cancel this step if necessary.</param>
        /// <returns>Instance of <see cref="UniTask"/> to be awaitable until finished.</returns>
        public virtual async UniTask StartStep(CancellationToken stepToken)
        {
            // if the ancestor of this step was stopped from the outside, deactivate persistant steps and raise some events
            // if (stepState is StepState.StepStopped)
            // {
            //     Debug.LogInfo("Step was stopped, skip start step", this);
            //     OnStepActionFinished?.Invoke(this, new BaseTrainingStepEventArgs(this));
            //     OnStepFinished?.Invoke(this, new BaseTrainingStepEventArgs(this));
            //     DeactivatePersistantStepsAsync();
            //     OnStepCompleted?.Invoke(this, new BaseTrainingStepEventArgs(this));
            //     return;
            // }

            // Reset the state of this step as it was brand new
            ResetStepState();
            InvokeOnStepChanged(new BaseTrainingStepEventArgs(this));

            NetworkManager.OnClientDisconnectCallback += OnClientDisconnected;

            // Use a linked cancellation token source for both cases:
            // - step action was cancelled (e.g. ForceContinue for this step)
            // - overall step system was cancelled
            var linkedSource = CancellationTokenSource.CreateLinkedTokenSource(stepToken, _stepActionTokenSource.Token);
            var linkedToken  = linkedSource.Token;

            // This convenience method is used by each UniTask that uses the linkedToken.
            // If the user force continue a step, an OperationCanceledException is thrown.
            // This method catches this exception for the linkedToken and calls catch and finally actions if needed.
            async UniTask RunStepActionTask(UniTask tryBlock, Action catchBlock = null, Action finallyBlock = null)
            {
                try
                {
                    await tryBlock;
                }
                catch (OperationCanceledException)
                {
                    if (Application.isPlaying) catchBlock?.Invoke();
                    if (stepToken.IsCancellationRequested) throw;
                }
                finally
                {
                    if (!stepToken.IsCancellationRequested && Application.isPlaying)
                    {
                        finallyBlock?.Invoke();
                    }
                }
            }

            // Step Started
            await RunStepActionTask(TimeoutDelay(_timeoutBeforeStepStart, linkedToken));
            if (IsServer) _stepState.Value = StepState.StepStarted;
            InvokeOnStepStarted(new BaseTrainingStepEventArgs(this));
            
            // Run the activation of activatables and persistant activatables in parallel;
            // raises corresponding events and waits for corresponding timeouts
            await RunStepActionTask(UniTask.WhenAll(
                                        ActivateActivatablesAsync(linkedToken),
                                        ActivatePersistantActivatablesAsync(linkedToken)));

            await RunStepActionTask(PreStepActionAsync(linkedToken));

            InvokeOnStepActionStarted(new BaseTrainingStepEventArgs(this));

            var stepActionTask = new UniTask();
            if (IsHost)
                stepActionTask = UniTask.WhenAll(ClientStepActionAsync(linkedToken), ServerStepActionAsync(linkedToken));
            else if (IsClient) stepActionTask = ClientStepActionAsync(linkedToken);
            else if (IsServer) stepActionTask = ServerStepActionAsync(linkedToken);

            await RunStepActionTask(stepActionTask, () =>
            {
                if (_stepActionTokenSource.IsCancellationRequested)
                {
                    // Most likely called by Server at ServerStepActionAsync;
                    // ClientStepActionAsync catches this exception to raise ClientStepFinished
                    Debug.LogWarning($"{GetType()}: Fallback: Step action was cancelled for step {name}", this);
                }
            });

            InvokeOnStepActionFinished(new BaseTrainingStepEventArgs(this));

            await RunStepActionTask(PostStepActionAsync(linkedToken));

            // stepState is set by the server in CheckStepCompletenessServer.
            // Clients and server will wait until this state is set to finished
            if (!stepToken.IsCancellationRequested)
                await RunStepActionTask(UniTask.WaitUntil(() => _stepState.Value == StepState.StepFinished, cancellationToken: stepToken));

            NetworkManager.OnClientDisconnectCallback -= OnClientDisconnected;

            if (!stepToken.IsCancellationRequested)
                await RunStepActionTask(TimeoutDelay(_timeoutBeforeStepFinish, stepToken));
            OnStepFinished?.Invoke(this, new BaseTrainingStepEventArgs(this));

            // Run the deactivation of the activatables and persistant activatables in parallel
            await RunStepActionTask(UniTask.WhenAll(
                                        DeactivateActivatablesAsync(stepToken), 
                                        DeactivatePersistantStepsAsync(stepToken)));

            for (var i = 0; i < nextSteps.Count; i++)
            {
                if (nextSteps[i] == null) continue;
                await nextSteps[i].StartStep(stepToken);
            }

            // After all children steps are processed (if there are any), this step is now the active one
            if (nextSteps.Count > 0) InvokeOnStepChanged(new BaseTrainingStepEventArgs(this));

            if (!stepToken.IsCancellationRequested)
                await RunStepActionTask(TimeoutDelay(_timeoutBeforeStepComplete, stepToken));

            if (IsServer) _stepState.Value = StepState.StepCompleted;
            StepCompletedAction();
            InvokeOnStepCompleted(new BaseTrainingStepEventArgs(this));
        }

        private static UniTask TimeoutDelay(float delay, CancellationToken linkedToken)
        {
            return UniTask.Delay(TimeSpan.FromSeconds(delay), cancellationToken: linkedToken);
        }

        /// <summary>
        /// Actions to perform when the step completed its execution. It is called by <see cref="StartStep"/>.
        /// </summary>
        protected virtual void StepCompletedAction()
        {
        }

        /// <summary>
        /// Resets the state of this training step at <see cref="StartStep"/>.
        /// </summary>
        protected virtual void ResetStepState()
        {
            if (IsServer)
            {
                _stepState.Value      = StepState.StepStarted;
                finishedClients.Value = 0;
                finishedClientsSet.Clear();
            }

            _stepActionTokenSource = new CancellationTokenSource();
        }

        /// <summary>
        /// Resets this step to its initial state. Can also stops and resets its children steps recursively.
        /// </summary>
        /// <param name="stopNextSteps">Stops the steps recursively in <see cref="nextSteps"/> iff true.</param>
        /// <param name="resetNextSteps">Resets the steps recursively in <see cref="nextSteps"/> iff true</param>
        /// <param name="useTimeouts">Optional: Use the timeouts.</param>
        public virtual async UniTask ResetStep(bool stopNextSteps, bool resetNextSteps, bool useTimeouts = false)
        {
            await StopStepAction(stopNextSteps, useTimeouts);

            if (resetNextSteps)
            {
                for (var i = 0; i < nextSteps.Count; i++)
                {
                    if (nextSteps[i] == null || nextSteps[i] == this) continue;
                    await nextSteps[i].ResetStep(stopNextSteps, true, useTimeouts);
                }
            }
            
            if (IsServer)
            {
                _stepState.Value      = StepState.StepWaiting;
                finishedClients.Value = 0;
                finishedClientsSet.Clear();
            }
        }

        /// <summary>
        /// Stops the current step and optionally all next steps.
        /// </summary>
        /// <param name="stopNextSteps">Stops all steps in <see cref="nextSteps"/> if true; otherwise, stops only this step.</param>
        /// <param name="useTimeouts">Optional: Use the timeouts</param>
        public virtual async UniTask StopStepAction(bool stopNextSteps, bool useTimeouts = false)
        {
            // Save guard for repeating steps - sometimes this method is called more than once. 
            // If the token source was already cancelled, we don't need to stop again. 
            if (_stepActionTokenSource.IsCancellationRequested) return;
            _stepActionTokenSource.Cancel();

            await DeactivateActivatablesAsync(this.GetCancellationTokenOnDestroy(), useTimeouts);
            await DeactivatePersistantActivatablesAsync(this.GetCancellationTokenOnDestroy(), useTimeouts);

            if (stopNextSteps)
            {
                for (var i = 0; i < nextSteps.Count; i++)
                {
                    if (nextSteps[i] == null || nextSteps[i] == this) continue;
                    await nextSteps[i].StopStepAction(true, useTimeouts);
                }
            }

            if (IsServer) _stepState.Value = StepState.StepStopped;
        }

        /// <summary>
        /// This method is called on the server and all clients before the step actions are started.
        /// It should be overridden by subclasses to perform actions that need to be performed before the step starts.
        /// The PreStepActionAsync method takes a CancellationToken as a parameter,
        /// which can be used to cancel the asynchronous operation.
        /// The method returns a Task representing the asynchronous operation.
        /// Use this method to initialize what should be used in the StepActions methods (e.g. add listener to events).
        /// </summary>
        /// <param name="ct">Instance of <see cref="CancellationToken"/> to cancel this step action if necessary.</param>
        /// <returns>Instance of <see cref="UniTask"/> as step action to be awaitable until finished.</returns>
        /// <seealso cref="ClientStepActionAsync"/>
        /// <seealso cref="ServerStepActionAsync"/>
        /// <seealso cref="PostStepActionAsync"/>
        protected virtual async UniTask PreStepActionAsync(CancellationToken ct)
        {
            await UniTask.CompletedTask;
        }

        /// <summary>
        /// This method is called on the server and all clients after the step actions have finished.
        /// It should be overridden by subclasses to perform actions that need to be performed after the step has completed.
        /// The PostStepActionAsync method takes a CancellationToken as a parameter,
        /// which can be used to cancel the asynchronous operation.
        /// The method returns a Task representing the asynchronous operation.
        /// Use this method to terminate what was initialized in <see cref="PreStepActionAsync"/> (e.g. remove listener to events).
        /// </summary>
        /// <param name="ct">Instance of <see cref="CancellationToken"/> to cancel this step action if necessary.</param>
        /// <returns>Instance of <see cref="UniTask"/> as step action to be awaitable until finished.</returns>
        /// <seealso cref="ClientStepActionAsync"/>
        /// <seealso cref="ServerStepActionAsync"/>
        /// <seealso cref="PreStepActionAsync"/>
        protected virtual async UniTask PostStepActionAsync(CancellationToken ct)
        {
            await UniTask.CompletedTask;
        }

        /// <summary>
        /// Activates all <see cref="_stepActivatables"/> and invokes
        /// <see cref="OnActivatablesActivating"/> and <see cref="OnActivatablesDeactivated"/>.
        /// </summary>
        /// <param name="ct">The cancellation token for the async operation.</param>
        /// <param name="useTimeouts">Optional: The deactivation timeouts are used if the value is <c>true</c>.</param>
        protected async UniTask ActivateActivatablesAsync(CancellationToken ct, bool useTimeouts = true)
        {
            try
            {
                if (useTimeouts) await TimeoutDelay(_timeoutActivatablesActivating, ct);
            }catch (OperationCanceledException) { }
            
            InvokeOnActivatablesActivating(new BaseTrainingStepEventArgs(this));
            ActivateActivatables();
            InvokeOnActivatablesActivated(new BaseTrainingStepEventArgs(this));

            try
            {
                if (useTimeouts) await TimeoutDelay(_timeoutActivatablesActivated, ct);
            }catch (OperationCanceledException) { }
        }

        /// <summary>
        /// Activates all <see cref="_stepActivatables"/>.
        /// </summary>
        private void ActivateActivatables()
        {
            for (var i = 0; i < _stepActivatables.Count; i++)
                _stepActivatables[i].SetActive(true);
        }

        /// <summary>
        /// Deactivates all <see cref="_stepActivatables"/> and invokes <see cref="OnActivatablesDeactivating"/> and
        /// <see cref="OnActivatablesDeactivated"/>.
        /// </summary>
        /// <param name="ct">The cancellation token for the async operation.</param>
        /// <param name="useTimeouts">Optional: The deactivation timeouts are used if the value is <c>true</c>.</param>
        protected async UniTask DeactivateActivatablesAsync(CancellationToken ct, bool useTimeouts = true)
        {
            InvokeOnActivatablesDeactivating(new BaseTrainingStepEventArgs(this));
            try
            {
                if (useTimeouts) await TimeoutDelay(_timeoutActivatablesDeactivating, ct);
                DeactivateActivatables();
                if (useTimeouts) await TimeoutDelay(_timeoutActivatablesDeactivated, ct);
            }
            catch (OperationCanceledException)
            {
                if (Application.isPlaying) DeactivateActivatables();
            }
            InvokeOnActivatablesDeactivated(new BaseTrainingStepEventArgs(this));
        }

        /// <summary>
        /// Deactivates all <see cref="_stepActivatables"/>.
        /// </summary>
        private void DeactivateActivatables()
        {
            for (var i = 0; i < _stepActivatables.Count; i++)
                _stepActivatables[i].SetActive(false);
        }

        /// <summary>
        /// Activates all <see cref="_persistantActivatables"/> and invokes
        /// <see cref="OnPersistantActivatablesActivating"/> and<see cref="OnPersistantActivatablesActivated"/>.
        /// </summary>
        /// <param name="ct">The cancellation token for the async operation.</param>
        /// <param name="useTimeouts">Optional: The deactivation timeouts are used if the value is <c>true</c>.</param>
        protected async UniTask ActivatePersistantActivatablesAsync(CancellationToken ct, bool useTimeouts = true)
        {
            try
            {
                if (useTimeouts) await TimeoutDelay(_timeoutPersistantActivatablesActivating, ct);
            }catch (OperationCanceledException) { }
            
            InvokeOnPersistantActivatablesActivating(new BaseTrainingStepEventArgs(this));
            ActivatePersistantActivatables();
            InvokeOnPersistantActivatablesActivated(new BaseTrainingStepEventArgs(this));
            
            try
            {
                if (useTimeouts) await TimeoutDelay(_timeoutPersistantActivatablesActivated, ct);
            }catch (OperationCanceledException) { }
        }

        /// <summary>
        /// Activates all <see cref="_persistantActivatables"/>.
        /// </summary>
        private void ActivatePersistantActivatables()
        {
            for (var i = 0; i < _persistantActivatables.Count; i++)
                _persistantActivatables[i].SetActive(true);
        }

        /// <summary>
        /// Deactivates <see cref="_persistantActivatables"/> from this step and invokes
        /// <see cref="OnPersistantActivatablesDeactivating"/> and <see cref="OnPersistantActivatablesDeactivated"/>.
        /// </summary>
        /// <param name="ct">The cancellation token for the async operation.</param>
        /// <param name="useTimeouts">Optional: The deactivation timeouts are used if the value is <c>true</c>.</param>
        private async UniTask DeactivatePersistantActivatablesAsync(CancellationToken ct, bool useTimeouts = true)
        {
            InvokeOnPersistantActivatablesDeactivating(new BaseTrainingStepEventArgs(this));
            try
            {
                if (useTimeouts) await TimeoutDelay(_timeoutPersistantActivatablesDeactivating, ct);
                DeactivatePersistantActivatables();
                if (useTimeouts) await TimeoutDelay(_timeoutPersistantActivatablesDeactivated, ct);
            }
            catch (OperationCanceledException)
            {
                if (Application.isPlaying) DeactivatePersistantActivatables();
            }
            InvokeOnPersistantActivatablesDeactivated(new BaseTrainingStepEventArgs(this));
        }

        /// <summary>
        /// Deactivates all <see cref="_persistantActivatables"/>.
        /// </summary>
        private void DeactivatePersistantActivatables()
        {
            for (var i = 0; i < _persistantActivatables.Count; i++)
                _persistantActivatables[i].SetActive(false);
        }

        /// <summary>
        /// Deactivates all <see cref="_persistantActivatables"/> from <see cref="_persistantActivatablesDeactivationSteps"/>.
        /// Calls <see cref="DeactivatePersistantActivatablesAsync"/>.
        /// </summary>
        /// <param name="ct">The cancellation token for the async operation.</param>
        /// <param name="useTimeouts">Optional: The deactivation timeouts are used if the value is <c>true</c>.</param>
        protected async UniTask DeactivatePersistantStepsAsync(CancellationToken ct,  bool useTimeouts = true)
        {
            for (var i = 0; i < _persistantActivatablesDeactivationSteps.Count; i++)
            {
                if (_persistantActivatablesDeactivationSteps[i] == null)
                    Debug.LogError($"{GetType()}: Unassigned reference to DeactivationPersistant in {name}!", this);
                else
                    await _persistantActivatablesDeactivationSteps[i].DeactivatePersistantActivatablesAsync(ct, useTimeouts);
            }
        }

        /// <summary>
        /// Abstract method that derived classes of <see cref="BaseTrainingStep"/> can override to specify the actions
        /// that should be taken by a client during this step of the training.
        /// </summary>
        /// <remarks>
        /// This method is called asynchronously in <see cref="StartStep"/> and can be cancelled by the server
        /// if the step is completed or stopped before the client finishes this step.
        /// Each client must call <see cref="RaiseClientStepFinished"/> in this method.
        /// </remarks>
        /// <param name="ct">Instance of <see cref="CancellationToken"/> to cancel this step if necessary.</param>
        /// <returns>Instance of <see cref="UniTask"/> as step action to be awaitable until finished.</returns>
        protected abstract UniTask ClientStepActionAsync(CancellationToken ct);

        /// <summary>
        /// This method is an abstract method that defines the actions that should be performed by the server when a step starts.
        /// It is called on the server when a step starts, and is executed asynchronously.
        /// </summary>
        /// <param name="ct">Instance of <see cref="CancellationToken"/> to cancel this step if necessary.</param>
        /// <returns>Instance of <see cref="UniTask"/> as step action to be awaitable until finished.</returns>
        protected virtual async UniTask ServerStepActionAsync(CancellationToken ct)
        {
            await UniTask.CompletedTask;
        }

        /// <summary>
        /// Resets the GameObject to its initial state.
        /// Adds GameObjects as children for (persistant) activatables and next steps if needed.
        /// Also adds <see cref="BaseTrainingStepDebug"/> and <see cref="BaseTrainingStepUnityEvents"/> if enabled
        /// in Preferences.
        /// </summary>
        protected virtual void Reset()
        {
            const string activatablesName           = "[Activatables]";
            const string persistantActivatablesName = "[Persistant Activatables]";
            const string nextStepsName              = "[Next Steps]";

            var childs = transform.Cast<Transform>().ToList();

            if (!childs.Exists(t => t.name.Equals(activatablesName)))
            {
                var go = new GameObject(activatablesName);
                go.transform.SetParent(transform);
                _stepActivatables.Add(go);
            }

            if (!childs.Exists(t => t.name.Equals(persistantActivatablesName)))
            {
                var go = new GameObject(persistantActivatablesName);
                go.transform.SetParent(transform);
                _persistantActivatables.Add(go);
            }

            if (!childs.Exists(t => t.name.Equals(nextStepsName)))
            {
                var go = new GameObject(nextStepsName);
                go.transform.SetParent(transform);
            }

            if (GameObjectPrefixName() == "" || name.StartsWith(GameObjectPrefixName())) return;
            if (name.StartsWith("[Step]")) name = name.Remove(0, 6);
            name = $"{GameObjectPrefixName()} {name.Trim(' ')}";

            // Add components based on the Preference Settings of the project.
#if UNITY_EDITOR
            if (EditorPrefs.GetBool("NMY.AddBaseTrainingStepUnityEvents") && !TryGetComponent(out BaseTrainingStepUnityEvents _))
            {
                gameObject.AddComponent<BaseTrainingStepUnityEvents>();
            }
            
            if (EditorPrefs.GetBool("NMY.AddBaseTrainingStepDebug") && !TryGetComponent(out BaseTrainingStepDebug _))
            {
                gameObject.AddComponent<BaseTrainingStepDebug>();
            }
#endif
        }
        
        /// <summary>
        /// Gets the prefix string to be used when naming game objects in the scene.
        /// </summary>
        /// <returns>The prefix string to be used when naming game objects in the scene.</returns>
        /// <remarks>
        /// Override this method if another GameObject prefix name is desired.
        /// </remarks>
        protected virtual string GameObjectPrefixName() => "[Step]";

        /// <summary>
        /// Attempts to recursively traverse the step hierarchy to find and start the <see cref="startTrainingStep"/>.
        /// All steps along the way to find '<paramref name="startTrainingStep"/>' are executed in <see cref="ExecuteMoveToStepAction"/>.
        /// </summary>
        /// <param name="startTrainingStep">Instance of <see cref="BaseTrainingStep"/> that should be started.</param>
        /// <param name="stepToken">Instance of <see cref="CancellationToken"/> to cancel this UniTask externally if necessary.</param>
        /// <returns>Returns true iff <see cref="startTrainingStep"/> was found and started; false otherwise.</returns>
        /// <seealso cref="ExecuteMoveToStepAction"/>
        public virtual async UniTask<bool> TryMoveToStep(BaseTrainingStep startTrainingStep,
                                                         CancellationToken stepToken)
        {
            var foundStep   = false;
            var indexOfStep = -1;

            // This step is not the training step, we have to go deeper in the hierarchy
            if (this != startTrainingStep)
            {
                // Try to mimic the state like we would begin from the start
                Debug.Log($"{GetType()}: Skip {name}", this);
                
                InvokeOnStepChanged(new BaseTrainingStepEventArgs(this));
                InvokeOnStepStarted(new BaseTrainingStepEventArgs(this));

                // these are step-local actions, but since we're effectively skipping this step, we don't use the timeouts
                // await ActivateActivatablesAsync(stepToken, false); 
                InvokeOnActivatablesActivating(new BaseTrainingStepEventArgs(this));
                InvokeOnActivatablesActivated(new BaseTrainingStepEventArgs(this));
                
                await ActivatePersistantActivatablesAsync(stepToken, false);

                InvokeOnStepActionStarted(new BaseTrainingStepEventArgs(this));

                await ExecuteMoveToStepAction(stepToken);

                InvokeOnStepActionFinished(new BaseTrainingStepEventArgs(this));
                InvokeOnStepFinished(new BaseTrainingStepEventArgs(this));

                if (IsServer) _stepState.Value = StepState.StepFinished;

                // these are step-local actions, but since we're effectively skipping this step, we don't use the timeouts
                // await DeactivateActivatablesAsync(stepToken, false);
                InvokeOnActivatablesDeactivating(new BaseTrainingStepEventArgs(this));
                InvokeOnActivatablesDeactivated(new BaseTrainingStepEventArgs(this));
                
                await DeactivatePersistantStepsAsync(stepToken, false);

                for (var i = 0; i < nextSteps.Count; i++)
                {
                    if (nextSteps[i] == null) continue;

                    foundStep   = await nextSteps[i].TryMoveToStep(startTrainingStep, stepToken);
                    indexOfStep = i;
                    if (foundStep) break;
                }
            }
            // Found the right training step, start it normally
            else
            {
                await StartStep(stepToken);
                stepToken.ThrowIfCancellationRequested();

                // we executed a regular StartStep(), which also takes care of all of its _nextSteps, and all events,
                // so we simply return here
                return true;
            }

            if (!foundStep)
            {
                // cleanly end the skipped step, without _timeoutBeforeCompleting
                InvokeOnStepCompleted(new BaseTrainingStepEventArgs(this));
                if (IsServer) _stepState.Value = StepState.StepCompleted;

                return false;
            }

            // Start all other steps that comes after the start step
            for (var i = indexOfStep + 1; i < nextSteps.Count; i++)
            {
                if (nextSteps[i] == null) continue;
                await nextSteps[i].StartStep(stepToken);
                stepToken.ThrowIfCancellationRequested();
            }

            await UniTask.Delay(TimeSpan.FromSeconds(_timeoutBeforeStepComplete), cancellationToken: stepToken);

            stepToken.ThrowIfCancellationRequested();

            InvokeOnStepCompleted(new BaseTrainingStepEventArgs(this));
            if (IsServer) _stepState.Value = StepState.StepCompleted;

            return true;
        }

        /// <summary>
        /// Called by <see cref="TryMoveToStep"/>. It gives all derived classes the ability to add their specific
        /// behaviours while moving to a specific step. This is often called the "end state" of a step.
        /// </summary>
        /// <param name="ct"></param>
        /// <seealso cref="TryMoveToStep"/>
        protected virtual async UniTask ExecuteMoveToStepAction(CancellationToken ct = default) => await UniTask.CompletedTask;

        /// <summary>
        /// Clients tell the server that they finished this step.
        /// </summary>
        [ContextMenu("RaiseStepFinished")]
        public void RaiseClientStepFinished()
        {
            if (IsClient) RaiseClientStepFinished_ServerRpc(NetworkManager.LocalClientId);
        }

        /// <summary>
        /// <b>Server Rpc Rpc</b><br/>
        /// Client with ID '<paramref name="clientID"/>' tells the server that it has finished this step.
        /// The server checks the step completeness afterwards (<see cref="CheckStepCompletenessServer"/>).
        /// </summary>
        /// <param name="clientID">The client ID that belongs to the client that finished this step.</param>
        /// <seealso cref="RaiseClientStepFinished"/>
        /// <seealso cref="CheckStepCompletenessServer"/>
        [ServerRpc(Delivery = RpcDelivery.Reliable, RequireOwnership = false)]
        private void RaiseClientStepFinished_ServerRpc(ulong clientID)
        {
            finishedClientsSet.Add(clientID);
            finishedClients.Value = finishedClientsSet.Count;
            CheckStepCompletenessServer();
        }

        /// <summary>
        /// Adds <paramref name="newStep"/> to <see cref="nextSteps"/>.
        /// </summary>
        /// <param name="newStep">Instance of <see cref="BaseTrainingStep"/> which should be added to <see cref="nextSteps"/>.</param>
        protected void AddNextStep(BaseTrainingStep newStep)
        {
            nextSteps.Add(newStep);
        }

        /// <summary>
        /// Removes <paramref name="oldStep"/> from <see cref="nextSteps"/> if present.
        /// </summary>
        /// <param name="oldStep">Instance of <see cref="BaseTrainingStep"/> which should be removed from <see cref="nextSteps"/>.</param>
        protected void RemoveNextStep(BaseTrainingStep oldStep)
        {
            if (nextSteps.Contains(oldStep)) nextSteps.Remove(oldStep);
        }

        protected void RemoveAllNextSteps()
        {
            nextSteps.Clear();
        }

        /// <summary>
        /// Recursively checks whether the specified <paramref name="step"/> is part of the step hierarchy or not.
        /// </summary>
        /// <remarks>This method can be used externally if, for example, two or more step systems are running
        /// simultaneously in the scene and <see cref="OnStepChanged"/> has been called and we want to know which system
        /// the step change belongs.
        /// </remarks>
        /// <param name="step">Instance of <see cref="BaseTrainingStep"/> to check if it is part of this hierarchy.</param>
        /// <param name="visited">List of <see cref="BaseTrainingStep"/>s that have already been visited.</param>
        /// <returns>Returns true iff <paramref name="step"/> was found in the hierarchy; false otherwise.</returns>
        public virtual bool IsStepInHierarchy(BaseTrainingStep step, HashSet<BaseTrainingStep> visited = default)
        {
            // Initialize the visited HashSet if it's null
            visited ??= new HashSet<BaseTrainingStep>();
            
            // If the step is already visited, it means we are in a cycle
            if (visited.Contains(step)) return false;
            
            // Base case: if this step is the one we're looking for, return true
            if (step == this) return true;
            
            // Add the current step to the visited set to prevent revisiting
            visited.Add(this);

            if (IsStepInNextSteps(step)) return true;
            
            // Check if the step is in any of the next steps
            foreach (var otherStep in nextSteps)
            {
                // If step is found in tuples, it's part of the hierarchy
                if (otherStep == step) return true;
                // Recursively check the hierarchy without revisiting steps
                if (!visited.Contains(otherStep) && otherStep.IsStepInHierarchy(step, visited)) return true;
            }

            return false;
        }

        protected bool IsStepInNextSteps(BaseTrainingStep step)
        {
            return nextSteps.Contains(step);
        }

        /// <summary>
        /// Forcefully cancel all step actions of this step and continues with the next step in the hierarchy.
        /// </summary>
        /// <remarks>This method can only be called by the server or the host, clients cannot continue the steps
        /// by themselves. They get notified by the server to cancel their steps if necessary.
        /// </remarks>
        /// <seealso cref="ForceContinue_ClientRpc"/>
        [ContextMenu("Force Continue")]
        public void ForceContinue()
        {
            if (!IsServer && !IsHost)
            {
                Debug.LogWarning($"{GetType()}: Only Server and Host are allowed to forcefully continue this step.",
                                 this);
                return;
            }

            if (IsServer) _stepActionTokenSource.Cancel();
            ForceContinue_ClientRpc();
        }

        /// <summary>
        /// <b>Client Rpc</b><br/>
        /// Server forcefully cancels all step actions of this step and continues with the next step
        /// in the hierarchy.
        /// </summary>
        /// <seealso cref="ForceContinue"/>
        [ClientRpc(Delivery = RpcDelivery.Reliable)]
        private void ForceContinue_ClientRpc()
        {
            _stepActionTokenSource.Cancel();
        }

#region Event Invokers

                protected virtual void InvokeOnStepChanged(BaseTrainingStepEventArgs args) => 
            OnStepChanged?.Invoke(this, args);

        protected virtual void InvokeOnStepStarted(BaseTrainingStepEventArgs args) => 
            OnStepStarted?.Invoke(this, args);
        
        protected virtual void InvokeOnStepFinished(BaseTrainingStepEventArgs args) => 
            OnStepFinished?.Invoke(this, args);
        
        protected virtual void InvokeOnStepCompleted(BaseTrainingStepEventArgs args) => 
            OnStepCompleted?.Invoke(this, args);
        
        protected virtual void InvokeOnStepActionStarted(BaseTrainingStepEventArgs args) => 
            OnStepActionStarted?.Invoke(this, args);        
        
        protected virtual void InvokeOnStepActionFinished(BaseTrainingStepEventArgs args) => 
            OnStepActionFinished?.Invoke(this, args);
        
        protected virtual void InvokeOnActivatablesActivating(BaseTrainingStepEventArgs args) => 
            OnActivatablesActivating?.Invoke(this, args);
        
        protected virtual void InvokeOnActivatablesActivated(BaseTrainingStepEventArgs args) => 
            OnActivatablesActivated?.Invoke(this, args);
        
        protected virtual void InvokeOnActivatablesDeactivating(BaseTrainingStepEventArgs args) => 
            OnActivatablesDeactivating?.Invoke(this, args);
        
        protected virtual void InvokeOnActivatablesDeactivated(BaseTrainingStepEventArgs args) => 
            OnActivatablesDeactivated?.Invoke(this, args);
        
        protected virtual void InvokeOnPersistantActivatablesActivating(BaseTrainingStepEventArgs args) => 
            OnPersistantActivatablesActivating?.Invoke(this, args);
        
        protected virtual void InvokeOnPersistantActivatablesActivated(BaseTrainingStepEventArgs args) => 
            OnPersistantActivatablesActivated?.Invoke(this, args);
        
        protected virtual void InvokeOnPersistantActivatablesDeactivating(BaseTrainingStepEventArgs args) => 
            OnPersistantActivatablesDeactivating?.Invoke(this, args);
        
        protected virtual void InvokeOnPersistantActivatablesDeactivated(BaseTrainingStepEventArgs args) => 
            OnPersistantActivatablesDeactivated?.Invoke(this, args);

#endregion

#region Timeout Properties

        /// <summary>
        /// The amount of time to wait before starting this step.
        /// </summary>
        internal float timeoutBeforeStepStart
        {
            get => _timeoutBeforeStepStart;
            set => _timeoutBeforeStepStart = value;
        }

        /// <summary>
        /// The amount of time to wait before finishing this step.
        /// </summary>
        internal float timeoutBeforeStepFinish
        {
            get => _timeoutBeforeStepFinish;
            set => _timeoutBeforeStepFinish = value;
        }

        /// <summary>
        /// The amount of time to wait before completing this step.
        /// </summary>
        internal float timeoutBeforeStepComplete
        {
            get => _timeoutBeforeStepComplete;
            set => _timeoutBeforeStepComplete = value;
        }

        /// <summary>
        /// The amount of time to wait before activating the activatable GameObjects.
        /// </summary>
        internal float timeoutActivatablesActivating
        {
            get => _timeoutActivatablesActivating;
            set => _timeoutActivatablesActivating = value;
        }

        /// <summary>
        /// The amount of time to wait after activating the activatable GameObjects.
        /// </summary>
        internal float timeoutActivatablesActivated
        {
            get => _timeoutActivatablesActivated;
            set => _timeoutActivatablesActivated = value;
        }

        /// <summary>
        /// The amount of time to wait before deactivating the activatable GameObjects.
        /// </summary>
        internal float timeoutActivatablesDeactivating
        {
            get => _timeoutActivatablesDeactivating;
            set => _timeoutActivatablesDeactivating = value;
        }

        /// <summary>
        /// The amount of time to wait after deactivating the activatable GameObjects.
        /// </summary>
        internal float timeoutActivatablesDeactivated
        {
            get => _timeoutActivatablesDeactivated;
            set => _timeoutActivatablesDeactivated = value;
        }

        /// <summary>
        /// The amount of time to wait before activating the persistant activatable GameObjects.
        /// </summary>
        internal float timeoutPersistantActivatablesActivating
        {
            get => _timeoutPersistantActivatablesActivating;
            set => _timeoutPersistantActivatablesActivating = value;
        }

        /// <summary>
        /// The amount of time to wait after activating the persistant activatable GameObjects.
        /// </summary>
        internal float timeoutPersistantActivatablesActivated
        {
            get => _timeoutPersistantActivatablesActivated;
            set => _timeoutPersistantActivatablesActivated = value;
        }

        /// <summary>
        /// The amount of time to wait before deactivating the persistant activatable GameObjects.
        /// </summary>
        internal float timeoutPersistantActivatablesDeactivating
        {
            get => _timeoutPersistantActivatablesDeactivating;
            set => _timeoutPersistantActivatablesDeactivating = value;
        }

        /// <summary>
        /// The amount of time to wait after deactivating the persistant activatable GameObjects.
        /// </summary>
        internal float timeoutPersistantActivatablesDeactivated
        {
            get => _timeoutPersistantActivatablesDeactivated;
            set => _timeoutPersistantActivatablesDeactivated = value;
        }
#endregion
    }
}
using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using NMY.VirtualRealityTraining.Steps;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

#if NMY_ENABLE_XR_INTERACTION_TOOLKIT
using UnityEngine.XR.Interaction.Toolkit.Inputs;
#endif

namespace NMY.VirtualRealityTraining
{
    /// <summary>
    /// <para>
    /// Concrete implementation of a step controller (<see cref="BaseStepController"/>).
    /// </para>
    /// <para>
    /// <b>Server</b><br/>
    /// When this component spawns, the server waits for the wait condition to be met (<see cref="WaitForStartConditionTask"/>)
    /// until it starts with the start step.
    /// </para>
    /// <para>
    /// <b>Client</b><br/>
    /// When this component spawns, the clients ask the server which step is the current active (<see cref="StepHandShake_ServerRpc"/>).
    /// After receiving the information handshake, they start the corresponding step. 
    /// </para>
    /// <para>
    /// This class allows to continue the current step with a button click either in PlayMode or as well in Builds.
    /// You can also stop the whole step system.
    /// </para>
    /// </summary>
    [RequireComponent(typeof(NetworkObject))]
    public class TrainingStepController : BaseStepController
    {
        /// <summary>
        /// Instance of <see cref="InputActionProperty"/> to forcefully continue the current step.
        /// </summary>
        [Tooltip("InputActionProperty to forcefully continue the current step")]
        [SerializeField] private InputActionProperty _continueNextStepAction;

        /// <summary>
        /// Should the action <see cref="_continueNextStepAction"/> be activated in a build?
        /// </summary>
        [Tooltip("Should the user be able to forcefully continue the current step in a build?")]
        [SerializeField] private bool _allowContinueNextStepInBuild;

        /// <summary>
        /// Minimum number of connected clients to start the start step. 
        /// </summary>
        [Tooltip("Minimum number of connected clients to start the start step.")]
        [SerializeField] private int _minConnectedClientsToStart = 1;

        /// <summary>
        /// Networked variable containing a reference to the previous step.
        /// </summary>
        private NetworkVariable<NetworkBehaviourReference> _previousStep = new();
        
        /// <summary>
        /// Networked variable containing a reference to the current step.
        /// </summary>
        private NetworkVariable<NetworkBehaviourReference> _currentStep  = new();

        private CancellationTokenSource _tokenSource = new();
        private bool                    _conditionCompleted;

        /// <summary>
        /// Property to get and set the previous step.
        /// </summary>
        public BaseTrainingStep previousStep
        {
            get
            {
                _previousStep.Value.TryGet(out BaseTrainingStep step);
                return step;
            }
            private set => _previousStep.Value = value;
        }

        /// <summary>
        /// Property to get and set the current step.
        /// </summary>
        public BaseTrainingStep currentStep
        {
            get
            {
                _currentStep.Value.TryGet(out BaseTrainingStep step);
                return step;
            }
            private set
            {
                if (rootTrainingStep.IsStepInHierarchy(value))
                    _currentStep.Value = value;
            }
        }


        private void OnEnable()
        {
            if (_continueNextStepAction.action == null) return;
            if (!_allowContinueNextStepInBuild)
            {
#if UNITY_EDITOR
#if NMY_ENABLE_XR_INTERACTION_TOOLKIT
                _continueNextStepAction.EnableDirectAction();
#endif
                _continueNextStepAction.action.performed += OnContinueNextStepAction;
#endif // UNITY_EDITOR
            }
            else
            {
#if NMY_ENABLE_XR_INTERACTION_TOOLKIT
                _continueNextStepAction.EnableDirectAction();
#endif
                _continueNextStepAction.action.performed += OnContinueNextStepAction;
            }
        }

        private void OnDisable()
        {
            if (_continueNextStepAction.action == null) return;

#if NMY_ENABLE_XR_INTERACTION_TOOLKIT
            _continueNextStepAction.DisableDirectAction();
#endif
            _continueNextStepAction.action.performed -= OnContinueNextStepAction;
        }

        /// <summary>
        /// Callback for the <see cref="_continueNextStepAction"/> action; it forcefully continue the current step.
        /// </summary>
        /// <param name="context">Information provided to action callbacks about what triggered an action.</param>
        private void OnContinueNextStepAction(InputAction.CallbackContext context)
        {
            currentStep.ForceContinue();
        }


        /// <summary>
        /// Overrides the <see cref="NetworkBehaviour.OnNetworkSpawn"/> method of the <see cref="NetworkBehaviour"/> class to
        /// start the logic for starting the steps for server and clients.
        /// </summary>
        public override async void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            if (IsServer) BaseTrainingStep.OnStepChanged += OnStepChanged;
            _previousStep.OnValueChanged += OnPreviousStepChanged;
            _currentStep.OnValueChanged  += OnCurrentStepChanged;

            if (IsServer)
            {
                // Server waits until the start condition is met
                await WaitForStartConditionTask();

                _conditionCompleted = true;

                // Debug: If the start step differs from the root step (we want to test faster),
                // let all clients move to the training step. This also starts the training step
                if (rootTrainingStep != startTrainingStep)
                {
                    TryMoveToStep(rootTrainingStep, startTrainingStep).Forget();
                }
                // Otherwise, start the start step for all clients through the RPC except the host. He starts his own
                else
                {
                    StartStep(startTrainingStep).Forget();
                }
            }

            // for late joining clients
            else if (IsClient)
            {
                StepHandShake_ServerRpc(NetworkManager.LocalClientId);
            }
        }

        /// <summary>
        /// <b>Server Rpc</b><br/>
        /// Client with ID '<paramref name="localClientId"/>' requests a handshake with the server to
        /// get the currently active step.
        /// </summary>
        /// <param name="localClientId">The client ID that requests the handshake.</param>
        [ServerRpc(Delivery = RpcDelivery.Reliable, RequireOwnership = false)]
        private void StepHandShake_ServerRpc(ulong localClientId)
        {
            StepHandShakeWaitForCondition(localClientId);
        }

        /// <summary>
        /// The server asynchronously waits until the wait condition is met and tells the requesting client with
        /// ID '<paramref name="localClientId"/>' which step should be started.
        /// </summary>
        /// <param name="localClientId">The client ID that requests the handshake</param>
        private async void StepHandShakeWaitForCondition(ulong localClientId)
        {
            await new WaitUntil(() => _conditionCompleted);
            StepHandShake_ClientRpc(currentStep, localClientId);
        }

        /// <summary>
        /// <b>Client Rpc</b>: The server tells the originated client
        /// (<code>NetworkManager.LocalClientId == localClientId</code>) which step should be started to be in sync
        /// with the server.
        /// </summary>
        /// <param name="currentStepReference">An instance of <see cref="NetworkBehaviourReference"/> holding the
        /// information about the currently active step from the server.</param>
        /// <param name="localClientId">The client ID from which the handshake originated.</param>
        [ClientRpc]
        private void StepHandShake_ClientRpc(NetworkBehaviourReference currentStepReference, ulong localClientId)
        {
            if (localClientId != NetworkManager.LocalClientId) return;

            if (currentStepReference.TryGet(out BaseTrainingStep step))
            {
                if (step == rootTrainingStep)
                {
                    StartStep(step).Forget();
                }
                else TryMoveToStep(rootTrainingStep, step).Forget();
            }
        }

        // [ClientRpc(Delivery = RpcDelivery.Reliable)]
        // private void TryMoveToStepsClientRpc(NetworkBehaviourReference startTrainingStep)
        // {
        //     if (startTrainingStep.TryGet(out BaseTrainingStep step))
        //     {
        //         TryMoveToStep(_rootTrainingStep, step);
        //     }
        // }

        /// <summary>
        /// In case a clients connected after the first step was finished (= late joining clients), we try to move
        /// from <paramref name="fromStep"/> to <paramref name="toStep"/>.
        /// </summary>
        /// <param name="fromStep">Instance of <see cref="BaseTrainingStep"/> to start moving forward.</param>
        /// <param name="toStep">Instance of <see cref="BaseTrainingStep"/> to which we want to move</param>
        private async UniTaskVoid TryMoveToStep(BaseTrainingStep fromStep, BaseTrainingStep toStep)
        {
            await fromStep.TryMoveToStep(toStep, _tokenSource.Token);
        }

        /// <summary>
        /// The start condition for this controller. If this condition is not met, the server does not start with the steps.
        /// </summary>
        private async UniTask WaitForStartConditionTask() =>
            await UniTask.WaitUntil(() => NetworkManager.ConnectedClients.Count >= _minConnectedClientsToStart);

        public override void OnNetworkDespawn()
        {
            base.OnNetworkDespawn();
            BaseTrainingStep.OnStepChanged -= OnStepChanged;
            _previousStep.OnValueChanged   -= OnPreviousStepChanged;
            _currentStep.OnValueChanged    -= OnCurrentStepChanged;
        }

        private void OnPreviousStepChanged(NetworkBehaviourReference previousValue, NetworkBehaviourReference newValue)
        {
            // if (newValue.TryGet(out BaseTrainingStep prev))
            // {
            //     Debug.Log($"{GetType()}: Previous: {prev.name}", this);
            // }
        }

        private void OnCurrentStepChanged(NetworkBehaviourReference previousValue, NetworkBehaviourReference newValue)
        {
            // if (newValue.TryGet(out BaseTrainingStep prev))
            // {
            //     Debug.Log($"{GetType()}: Current: {prev.name}", this);
            // }
        }

        /// <summary>
        /// Callback method when the currently active step changes.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnStepChanged(object sender, BaseTrainingStepEventArgs e)
        {
            if (!IsServer) return;

            if (currentStep != null) previousStep = currentStep;
            currentStep = e.step;
        }


        [ClientRpc]
        private void StartStepClientRpc()
        {
            if (!IsHost) StartStep(startTrainingStep).Forget();
        }

        /// <summary>
        /// Starts the straining step.
        /// </summary>
        /// <param name="step">An instance of <see cref="BaseTrainingStep"/> to be started.</param>
        private async UniTaskVoid StartStep(BaseTrainingStep step)
        {
            try
            {
                await step.StartStep(_tokenSource.Token);
            }
            catch (OperationCanceledException)
            {
                Debug.Log($"{GetType()}: Step was canceled. Reset now all steps.", this);
                await step.ResetStep(true, true, true);
            }
        }

        /// <summary>
        /// Try to change the start step from server side before the steps have initialized.
        /// </summary>
        /// <param name="newStartStep">The new Start Step for this controller.</param>
        /// <returns>Returns true if the change was successful; false otherwise.</returns>
        public bool TryChangeStartStep(BaseTrainingStep newStartStep)
        {
            // can we access Network info?
            // NOTE: we may not access the currentStep property if NetworkManager itself hasn't initialized yet!
            if (NetworkManager.Singleton != null)
            {
                // only the server may modify the startTrainingStep
                if (!IsServer) return false;

                // only allowed if we haven't initialized yet!
                if (currentStep != null) return false;
            }

            if (newStartStep == null)
                return false;

            startTrainingStep = newStartStep;
            return true;
        }

        /// <summary>
        /// Stops the entire step system.
        /// </summary>
        [ContextMenu("Stop Step System")]
        public void StopStepSystem()
        {
            _tokenSource.Cancel();
        }
    }
}
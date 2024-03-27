using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

namespace NMY.VirtualRealityTraining.Steps
{
    /// <summary>
    /// A special training step that waits for the user input to start the other step from
    /// a previous <see cref="ButtonDecisionStep"/> if desired.
    /// </summary>
    public class RepeatOtherUiButtonDecisionStepStep : BaseTrainingStep
    {
        /// <summary>
        /// A reference to the previous decision step from which the <see cref="DecisionStep{T}.otherStep"/>
        /// should be repeated.
        /// </summary>
        [SerializeField] private ButtonDecisionStep _decisionStep;

        /// <summary>
        /// A reference to the button that must be clicked to start the other step.
        /// </summary>
        [SerializeField] private Button _yesButton;

        /// <summary>
        /// A reference to the button that must be clicked to not start the other step.
        /// </summary>
        [SerializeField] private Button _noButton;

        /// <summary>
        /// A networked flag indicating whether the user has clicked the "Yes" button.
        /// The value is synchronized by the server to all clients.
        /// </summary>
        private NetworkVariable<bool> _yesButtonClicked = new();

        /// <summary>
        /// A networked flag indicating whether the user has clicked the "no" button.
        /// The value is synchronized by the server to all clients.
        /// </summary>
        private NetworkVariable<bool> _noButtonClicked = new();

        /// <summary>
        /// A networked reference to the step that 
        /// </summary>
        private NetworkVariable<NetworkBehaviourReference> _repeatedStep = new();

        /// <summary>
        /// A flag indicating whether the other step was added to <see cref="BaseTrainingStep._nextSteps"/>.
        /// </summary>
        private bool _addedToNextSteps;

        public BaseTrainingStep repeatedStep => _repeatedStep.Value.TryGet(out BaseTrainingStep step) ? step : null;

        
        /// <summary>
        /// Overrides the <see cref="BaseTrainingStep.PreStepActionAsync"/> method of the <see cref="BaseTrainingStep"/> class to
        /// register event handlers for the <see cref="Button.onClick"/> events of the <see cref="Button"/> class,
        /// which are used to detect when the client clicked <see cref="_yesButton"/> and <see cref="_noButton"/>.
        /// </summary>
        /// <param name="ct">The cancellation token for the async operation.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        protected override async UniTask PreStepActionAsync(CancellationToken ct)
        {
            await base.PreStepActionAsync(ct);

            _yesButton.onClick.AddListener(OnYesButtonClicked);
            _yesButton.interactable = true;

            _noButton.onClick.AddListener(OnNoButtonClicked);
            _noButton.interactable = true;

            _repeatedStep.OnValueChanged += OnRepeatedStepChanged;
        }

        /// <summary>
        /// Overrides the <see cref="BaseTrainingStep.PostStepActionAsync"/> method of the <see cref="BaseTrainingStep"/> class to
        /// unregister event handlers for the <see cref="Button.onClick"/> events of the <see cref="Button"/> class
        /// that were registered in the <see cref="PreStepActionAsync"/> method.
        /// </summary>
        /// <param name="ct">The cancellation token for the async operation.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        protected override async UniTask PostStepActionAsync(CancellationToken ct)
        {
            await base.PostStepActionAsync(ct);

            _yesButton.onClick.RemoveListener(OnYesButtonClicked);
            _yesButton.interactable = false;

            _noButton.onClick.RemoveListener(OnNoButtonClicked);
            _noButton.interactable = false;

            _repeatedStep.OnValueChanged -= OnRepeatedStepChanged;
        }

        /// <summary>
        /// The event handler for the <see cref="NetworkVariable{T}.OnValueChanged"/> event of the
        /// <see cref="_repeatedStep"/> network variable. It adds the reference step to
        /// <see cref="BaseTrainingStep._nextSteps"/> if possible.
        /// </summary>
        /// <param name="previousValue">The previous value of the <see cref="_repeatedStep"/> network variable.</param>
        /// <param name="newValue">The new value of the <see cref="_repeatedStep"/> network variable.</param>
        private void OnRepeatedStepChanged(NetworkBehaviourReference previousValue, NetworkBehaviourReference newValue)
        {
            if (stepState is not StepState.StepStarted) return;

            if (newValue.TryGet(out BaseTrainingStep step))
            {
                AddNextStep(step);
                _addedToNextSteps = true;
            }
        }
        
        /// <summary>
        /// The event handler for the <see cref="Button.onClick"/> event of the <see cref="_yesButton"/> <see cref="Button"/>.
        /// Tells the server that the "Yes" button was clicked and sends the desired <see cref="DecisionStep{T}.otherStep"/>.
        /// </summary>
        private void OnYesButtonClicked()
        {
            YesButtonClicked_ServerRpc();
            SetOtherStep_ServerRpc(_decisionStep.otherStep);
        }

        /// <summary>
        /// The event handler for the <see cref="Button.onClick"/> event of the <see cref="_noButton"/> <see cref="Button"/>.
        /// Tells the server that the "No" button was clicked.
        /// </summary>
        private void OnNoButtonClicked()
        {
            NoButtonClicked_ServerRpc();
        }

        /// <summary>
        /// <b>Server Rpc</b><br/>
        /// A client decided to click the "Yes" button. Sets <see cref="_yesButtonClicked"/> to true.
        /// </summary>
        [ServerRpc(RequireOwnership = false, Delivery = RpcDelivery.Reliable)]
        private void YesButtonClicked_ServerRpc() => _yesButtonClicked.Value = true;

        /// <summary>
        /// <b>Server Rpc</b><br/>
        /// A client decided to click the "No" button. Sets <see cref="_noButtonClicked"/> to true.
        /// </summary>
        [ServerRpc(RequireOwnership = false, Delivery = RpcDelivery.Reliable)]
        private void NoButtonClicked_ServerRpc() => _noButtonClicked.Value = true;

        /// <summary>
        /// <b>Server Rpc</b><br/>
        /// Sets <see cref="_repeatedStep"/> either to null or to the referenced <see cref="NetworkBehaviour"/>
        /// from <paramref name="otherStepReference"/>.
        /// </summary>
        /// <param name="otherStepReference">Instance of <see cref="NetworkBehaviourReference"/> that contains a
        /// <see cref="BaseTrainingStep"/>.</param>
        [ServerRpc(RequireOwnership = false, Delivery = RpcDelivery.Reliable)]
        private void SetOtherStep_ServerRpc(NetworkBehaviourReference otherStepReference)
        {
            _repeatedStep.Value = otherStepReference.TryGet(out BaseTrainingStep otherStep)
                ? otherStep
                : otherStepReference;
        }
        
        /// <summary>
        /// Overrides the <see cref="BaseTrainingStep.ClientStepActionAsync"/> method of the <see cref="BaseTrainingStep"/> class to
        /// asynchronously wait until the user clicked either the "Yes" or "No" button or another client
        /// already clicked it and the new step was added before signaling that the step is finished for the local client.
        /// </summary>
        /// <remarks>
        /// This method calls <see cref="WaitUntilYesButtonClicked"/> and <see cref="WaitUntilNoButtonClicked"/>
        /// to wait until the user clicked either the "Yes" or "No" button.
        /// Additionally, it calls <see cref="WaitUntilStepAddedToNextSteps"/> to check whether another client clicked already one of the buttons.
        /// Afterwards, it calls <see cref="BaseTrainingStep.RaiseClientStepFinished"/> to signal that the step is finished.
        /// If the cancellation token is cancelled while waiting, <see cref="BaseTrainingStep.RaiseClientStepFinished"/> is still called.
        /// </remarks>
        /// <param name="ct">The cancellation token for the async operation.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        protected override async UniTask ClientStepActionAsync(CancellationToken ct)
        {
            try
            {
                await UniTask.WhenAny(WaitUntilYesButtonClicked(ct), WaitUntilNoButtonClicked(ct),
                                      WaitUntilStepAddedToNextSteps(ct));

                // When "no button" is clicked, we dont need to add a step to nextSteps; otherwise,
                // wait until all clients added the other step to the next steps
                if (_yesButtonClicked.Value)
                {
                    await WaitUntilStepAddedToNextSteps(ct);
                }

                RaiseClientStepFinished();
            }
            catch (OperationCanceledException)
            {
                RaiseClientStepFinished();
            }
        }

        /// <summary>
        /// Overrides the <see cref="BaseTrainingStep.ServerStepActionAsync"/> method of the <see cref="BaseTrainingStep"/> class to
        /// asynchronously wait for the user input (either "Yes" or "No" button was clicked).
        /// It waits until the step was added by all clients to continue with the next step. 
        /// </summary>
        /// <param name="ct">The cancellation token to use for cancelling the operation.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        protected override async UniTask ServerStepActionAsync(CancellationToken ct)
        {
            try
            {
                await UniTask.WhenAny(WaitUntilYesButtonClicked(ct), WaitUntilNoButtonClicked(ct),
                                      WaitUntilStepAddedToNextSteps(ct));

                // When "no button" is clicked, we dont need to add a step to nextSteps; otherwise,
                // wait until all clients added the other step to the next steps
                if (_yesButtonClicked.Value)
                {
                    await WaitUntilStepAddedToNextSteps(ct);
                }
            }
            catch (OperationCanceledException)
            {
            }
        }

        /// <summary>
        /// Asynchronously waits until the "Yes" button was clicked.
        /// </summary>
        /// <param name="ct">The cancellation token to use for cancelling the wait operation.</param>
        /// <returns>A task that represents the asynchronous wait operation.</returns>
        private UniTask WaitUntilYesButtonClicked(CancellationToken ct) =>
            UniTask.WaitUntil(() => _yesButtonClicked.Value, cancellationToken: ct);

        /// <summary>
        /// Asynchronously waits until the "No" button was clicked.
        /// </summary>
        /// <param name="ct">The cancellation token to use for cancelling the wait operation.</param>
        /// <returns>A task that represents the asynchronous wait operation.</returns>
        private UniTask WaitUntilNoButtonClicked(CancellationToken ct) =>
            UniTask.WaitUntil(() => _noButtonClicked.Value, cancellationToken: ct);

        /// <summary>
        /// Asynchronously waits until a step was added to <see cref="BaseTrainingStep._nextSteps"/>.
        /// </summary>
        /// <param name="ct">The cancellation token to use for cancelling the wait operation.</param>
        /// <returns>A task that represents the asynchronous wait operation.</returns>
        private UniTask WaitUntilStepAddedToNextSteps(CancellationToken ct) =>
            UniTask.WaitUntil(() => _addedToNextSteps, cancellationToken: ct);

        /// <summary>
        /// Overrides the method <see cref="BaseTrainingStep.ResetStepState"/> of the <see cref="BaseTrainingStep"/> class to
        /// remove for all clients and server the <see cref="repeatedStep"/> from <see cref="BaseTrainingStep._nextSteps"/>.
        /// It also resets the networked variables <see cref="_repeatedStep"/>, <see cref="_yesButton"/>,
        /// and <see cref="_noButton"/> by the server. 
        /// </summary>
        protected override void ResetStepState()
        {
            _addedToNextSteps = false;

            RemoveNextStep(repeatedStep);

            if (IsServer)
            {
                _repeatedStep.Value     = new NetworkBehaviourReference();
                _yesButtonClicked.Value = false;
                _noButtonClicked.Value  = false;
            }

            base.ResetStepState();
        }

        /// <summary>
        /// Overrides the method <see cref="BaseTrainingStep.StepCompletedAction"/> of the <see cref="BaseTrainingStep"/> class to
        /// remove <see cref="repeatedStep"/> from <see cref="BaseTrainingStep._nextSteps"/> if this step is finished.
        /// </summary>
        protected override void StepCompletedAction()
        {
            base.StepCompletedAction();
            RemoveNextStep(repeatedStep);
        }
    }
}
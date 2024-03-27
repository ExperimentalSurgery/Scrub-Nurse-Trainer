using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

namespace NMY.VirtualRealityTraining.Steps
{
    /// <summary>
    /// A training step that repeats this step until the required steps from <see cref="_requiredSteps"/> are traversed.
    /// </summary>
    /// <remarks>
    /// <para>
    /// First, this step adds the <see cref="DecisionStep{T}._selectedStep"/> to
    /// <see cref="BaseTrainingStep._nextSteps"/>. It waits until it was added. Afterwards, it checks whether we need
    /// to repeat this step. If not all <see cref="_requiredSteps"/> were traversed so far, add this step to the
    /// <see cref="BaseTrainingStep._nextSteps"/> as well.
    /// </para>
    /// <para>
    /// The order in which the steps are traversed is not important.
    /// <see cref="_requiredSteps"/> must contain steps that are also set in <see cref="DecisionStep{T}._stepTuples"/>.
    /// </para>
    /// </remarks>
    public class RepeatButtonDecisionStep : ButtonDecisionStep
    {
        /// <summary>
        /// A boolean value indicating whether to add all steps from the step tuples as required steps at <see cref="Start"/>.
        /// </summary>
        [SerializeField] private bool _automaticallyFillRequiredSteps;

        [SerializeField] private bool _deactivateTraversedStepButtons;
        

        /// <summary>
        /// <para>
        /// A list of required steps that must be traversed in this loop. These steps must also be set in
        /// <see cref="DecisionStep{T}._stepTuples"/>.
        /// </para>
        /// <para>
        /// If not set, it will be automatically filled with the steps from <see cref="DecisionStep{T}._stepTuples"/>.
        /// </para>
        /// </summary>
        [SerializeField] private List<BaseTrainingStep> _requiredSteps = new();

        /// <summary>
        /// A <see cref="NetworkList{T}"/> containing all steps that were traversed in this loop so far.
        /// </summary>
        private NetworkList<NetworkBehaviourReference> _traversedSteps;

        /// <summary>
        /// A flag that indicates whether the traversed step has been added to the traversed list.
        /// </summary>
        private bool _addedToTraversedSteps;

        protected override void Awake()
        {
            base.Awake();
            _traversedSteps = new NetworkList<NetworkBehaviourReference>();
        }

        /// <summary>
        /// Overrides the <see cref="BaseTrainingStep.Start"/> method of the <see cref="BaseTrainingStep"/> class to
        /// set <see cref="_requiredSteps"/> to all steps that are present in <see cref="DecisionStep{T}._stepTuples"/>.
        /// </summary>
        protected override void Start()
        {
            base.Start();
            
            if (_requiredSteps.Count != 0) return;
            if (!_automaticallyFillRequiredSteps) return;
            foreach (var item in _stepTuples)
            {
                _requiredSteps.Add(item.step);
            }
        }

        /// <summary>
        /// Overrides the <see cref="ButtonDecisionStep.PreStepActionAsync"/> method of the <see cref="ButtonDecisionStep"/> class to
        /// register event handlers for the <see cref="NetworkList{T}.OnListChanged"/> events of the <see cref="NetworkList{T}"/> class,
        /// which are used to detect when the selected step was added to the traversed steps.
        /// </summary>
        /// <param name="ct">The cancellation token for the async operation.</param>
        /// <returns>A task representing the asynchronous operation.</returns>

        protected override async UniTask PreStepActionAsync(CancellationToken ct)
        {
            await base.PreStepActionAsync(ct);
            _traversedSteps.OnListChanged -= OnTraversedStepsChanged;
            _traversedSteps.OnListChanged += OnTraversedStepsChanged;

            // Check traversed steps to deactivate
            if (!_deactivateTraversedStepButtons) return;
            foreach (var stepsReference in _traversedSteps)
            {
                if (!stepsReference.TryGet(out BaseTrainingStep step)) continue;
                
                foreach (var stepDecisionTuple in _stepTuples)
                {
                    if (stepDecisionTuple.step == step)
                    {
                        stepDecisionTuple.comparable.interactable = false;
                    }
                }
            }
        }

        /// <summary>
        /// Overrides the <see cref="ButtonDecisionStep.PreStepActionAsync"/> method of the <see cref="ButtonDecisionStep"/> class to
        /// unregister event handlers for the <see cref="NetworkList{T}.OnListChanged"/> events of the <see cref="NetworkList{T}"/> class
        /// that were registered in the <see cref="PreStepActionAsync"/> method.
        /// </summary>
        /// <param name="ct">The cancellation token for the async operation.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        protected override async UniTask PostStepActionAsync(CancellationToken ct)
        {
            _traversedSteps.OnListChanged -= OnTraversedStepsChanged;
            await base.PostStepActionAsync(ct);
        }


        /// <summary>
        /// The event handler for the <see cref="NetworkList{T}.OnListChanged"/> event of the <see cref="_traversedSteps"/> network list.
        /// Sets <see cref="_addedToTraversedSteps"/> to `true`.
        /// </summary>
        /// <param name="changeEvent"> The change event information from <see cref="NetworkListEvent{T}"/>.</param>
        private void OnTraversedStepsChanged(NetworkListEvent<NetworkBehaviourReference> changeEvent)
        {
            _addedToTraversedSteps = true;
        }

        /// <summary>
        /// Overrides the <see cref="DecisionStep{T}.ClientStepActionAsync"/> method of the <see cref="DecisionStep{T}"/> class to
        /// wait until the we know if we need to repeat this step again or not before calling the base implementation.
        /// </summary>
        /// <param name="ct">The cancellation token to use for cancelling the operation.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        protected override async UniTask ClientStepActionAsync(CancellationToken ct)
        {
            await RepeatThisStepIfNecessaryAsync(ct);
            // call this last because RaiseClientStepFinished is called there.
            await base.ClientStepActionAsync(ct);
        }

        /// <summary>
        /// Overrides the <see cref="DecisionStep{T}.ServerStepActionAsync"/> method of the <see cref="DecisionStep{T}"/> class to
        /// wait for <see cref="RepeatThisStepIfNecessaryAsync"/> before it continues with the base implementation.
        /// </summary>
        /// <param name="ct">The cancellation token for the async operation.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        protected override async UniTask ServerStepActionAsync(CancellationToken ct)
        {
            await RepeatThisStepIfNecessaryAsync(ct);
            await base.ServerStepActionAsync(ct);
        }

        /// <summary>
        /// Checks whether this step must be repeated until <see cref="IsRequiredStepsComplete"/> returns true.
        /// </summary>
        /// <remarks>
        /// It asynchronously waits for <see cref="DecisionStep{T}._addedSelectedStepToNextSteps"/> and
        /// <see cref="_addedToTraversedSteps"/> to be set in order to determine if we need to add this
        /// step again to <see cref="BaseTrainingStep._nextSteps"/>.
        /// </remarks>
        /// <param name="ct">The cancellation token to use for cancelling the wait operation.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        private async UniTask RepeatThisStepIfNecessaryAsync(CancellationToken ct)
        {
            if (IsRequiredStepsComplete()) await UniTask.CompletedTask;

            // Wait for all variables to be set before checking if we need to repeat this step again.
            await UniTask.WhenAll(WaitUntilTraversedStepsUpdated(ct), WaitUntilStepAddedToNextSteps(ct));

            // Add this step to the next steps if we need to repeat it after selected step is finished.
            if (!IsRequiredStepsComplete() && !IsStepInNextSteps(this)) AddNextStep(this);
        }

        /// <summary>
        /// Asynchronously waits until the traversed step was added to <see cref="_traversedSteps"/>.
        /// </summary>
        /// <param name="ct">The cancellation token to use for cancelling the wait operation.</param>
        /// <returns>A task that represents the asynchronous wait operation.</returns>
        private UniTask WaitUntilTraversedStepsUpdated(CancellationToken ct) =>
            UniTask.WaitUntil(() => _addedToTraversedSteps, cancellationToken: ct);

        /// <summary>
        /// Determines whether the all required steps from <see cref="_requiredSteps"/> were traversed at least once.
        /// </summary>
        /// <returns>`true` if all required steps from <see cref="_requiredSteps"/> are present in the
        /// <see cref="_traversedSteps"/>, `false` otherwise.</returns>
        private bool IsRequiredStepsComplete()
        {
            foreach (var requiredStep in _requiredSteps)
            {
                if (!_traversedSteps.Contains(requiredStep)) return false;
            }

            return true;
        }

        /// <summary>
        /// Overrides the <see cref="ButtonDecisionStep.SetSelectedStep"/> of the <see cref="ButtonDecisionStep"/> class to
        /// send the corresponding step from this client to the server.
        /// It calls <see cref="SetTraversedStep_ServerRpc"/>.
        /// </summary>
        /// <param name="decisionTuple">The <see cref="DecisionStep{T}.StepDecisionTuple{TT}"/> that was selected.</param>
        protected override void SetSelectedStep(StepDecisionTuple<Button> decisionTuple)
        {
            SetTraversedStep_ServerRpc(decisionTuple.step);
            base.SetSelectedStep(decisionTuple);
        }

        /// <summary>
        /// <b>Server Rpc</b><br/>
        /// Called by <see cref="SetSelectedStep"/>; it adds <see cref="stepReference"/> to the
        /// <see cref="_traversedSteps"/> list.
        /// </summary>
        /// <param name="stepReference">An instance of <see cref="NetworkBehaviourReference"/> that holds a <see cref="BaseTrainingStep"/> reference.</param>
        [ServerRpc(RequireOwnership = false, Delivery = RpcDelivery.Reliable)]
        private void SetTraversedStep_ServerRpc(NetworkBehaviourReference stepReference)
        {
            if (stepReference.TryGet(out BaseTrainingStep step))
            {
                if (_traversedSteps.Contains(stepReference))
                {
                    _traversedSteps.Remove(stepReference);
                }

                _traversedSteps.Add(stepReference);
            }
        }

        public override async UniTask<bool> TryMoveToStep(BaseTrainingStep startTrainingStep,
                                                          CancellationToken stepToken)
        {
            // the step is not found in the hierarchy, we can't move to it
            if (!IsStepInHierarchy(startTrainingStep))
                return await base.TryMoveToStep(startTrainingStep, stepToken);

            // The RepeatButtonDecisionStep is the one we want to move to. 
            if (this == startTrainingStep)
            {
                await StartStep(stepToken);
                stepToken.ThrowIfCancellationRequested();
                return true;
            }
            
            // This RepeatButtonDecisionStep contains the desired step deeper in the hierarchy.
            
            // Try to mimic the state like we would begin from the start
            Debug.Log($"{GetType()}: Skip {name}", this);

            InvokeOnStepStarted(new BaseTrainingStepEventArgs(this));

            InvokeOnStepActionStarted(new BaseTrainingStepEventArgs(this));
            
            InvokeOnActivatablesActivating(new BaseTrainingStepEventArgs(this));
            InvokeOnActivatablesActivated(new BaseTrainingStepEventArgs(this));
            
            await ActivatePersistantActivatablesAsync(stepToken, false);
            await PreStepActionAsync(stepToken);

            await selectedStep.TryMoveToStep(startTrainingStep, stepToken);
            // Add this step to the next steps if we need to repeat it after selected step is finished.
            if (!IsRequiredStepsComplete() && !IsStepInNextSteps(this)) AddNextStep(this);

            InvokeOnStepActionFinished(new BaseTrainingStepEventArgs(this));
            await PostStepActionAsync(stepToken);
            
            InvokeOnStepFinished(new BaseTrainingStepEventArgs(this));

            if (IsServer) _stepState.Value = StepState.StepFinished;
            
            InvokeOnActivatablesDeactivating(new BaseTrainingStepEventArgs(this));
            InvokeOnActivatablesDeactivated(new BaseTrainingStepEventArgs(this));
            await DeactivatePersistantStepsAsync(stepToken, false);
            
            // Start all other steps that comes after the start step
            for (var i = 0; i < nextSteps.Count; i++)
            {
                if (nextSteps[i] == null) continue;
                await nextSteps[i].StartStep(stepToken);
                stepToken.ThrowIfCancellationRequested();
            }
            
            await UniTask.Delay(TimeSpan.FromSeconds(timeoutBeforeStepComplete), cancellationToken: stepToken);

            stepToken.ThrowIfCancellationRequested();

            InvokeOnStepCompleted(new BaseTrainingStepEventArgs(this));
            if (IsServer) _stepState.Value = StepState.StepCompleted;
            
            return true;
        }

        /// <summary>
        /// Overrides the method <see cref="DecisionStep{T}.ResetStepState"/> of the <see cref="DecisionStep{T}"/> class to
        /// remove this step and to reset the flag <see cref="_addedToTraversedSteps"/>.
        /// </summary>
        protected override void ResetStepState()
        {
            RemoveNextStep(this);
            _addedToTraversedSteps = false;
            base.ResetStepState();
        }

        /// <summary>
        /// Overrides the method <see cref="DecisionStep{T}.StepCompletedAction"/> of the <see cref="DecisionStep{T}"/> class to
        /// clear <see cref="_traversedSteps"/> from if this step is finished.
        /// </summary>
        protected override void StepCompletedAction()
        {
            base.StepCompletedAction();

            if (!IsServer) return;
            _traversedSteps.Clear();
        }

        protected override string GameObjectPrefixName() => "[Repeat Decision Step]";
    }
}
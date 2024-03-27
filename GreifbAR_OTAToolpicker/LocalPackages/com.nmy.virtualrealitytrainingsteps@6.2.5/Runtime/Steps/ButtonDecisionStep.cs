using System.Threading;
using Cysharp.Threading.Tasks;
using Unity.Netcode;
using UnityEngine.UI;

namespace NMY.VirtualRealityTraining.Steps
{
    /// <summary>
    /// A training step that waits for a UI <see cref="Button"/> <see cref="Button.onClick"/> event to add the
    /// corresponding step into the step sequence.
    /// </summary>
    public class ButtonDecisionStep : DecisionStep<Button>
    {
        /// <summary>
        /// Overrides the <see cref="DecisionStep{T}.PreStepActionAsync"/> method of the <see cref="DecisionStep{T}"/> class to
        /// register event handlers for the <see cref="Button.onClick"/> events of the <see cref="Button"/> class,
        /// which are used to detect when the player clicked a button to make the step decision.
        /// </summary>
        /// <param name="ct">The cancellation token for the async operation.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        protected override UniTask PreStepActionAsync(CancellationToken ct)
        {
            for (var index = 0; index < _stepTuples.Count; index++)
            {
                var stepTuple = _stepTuples[index];
                stepTuple.comparable.onClick.AddListener(() => SetSelectedStep(stepTuple));
                stepTuple.comparable.interactable = true;
            }

            return base.PreStepActionAsync(ct);
        }

        /// <summary>
        /// Overrides the <see cref="DecisionStep{T}.PreStepActionAsync"/> method of the <see cref="DecisionStep{T}"/> class to
        /// unregister event handlers for the <see cref="Button.onClick"/> events of the <see cref="Button"/> class
        /// that were registered in the <see cref="PreStepActionAsync"/> method.
        /// which are used to detect when the player clicked a button to make the step decision.
        /// </summary>
        /// <param name="ct">The cancellation token for the async operation.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        protected override UniTask PostStepActionAsync(CancellationToken ct)
        {
            for (var index = 0; index < _stepTuples.Count; index++)
            {
                var stepTuple = _stepTuples[index];
                stepTuple.comparable.onClick.RemoveListener(() => SetSelectedStep(stepTuple));
                stepTuple.comparable.interactable = false;
            }

            return base.PostStepActionAsync(ct);
        }

        /// <summary>
        /// Overrides the <see cref="DecisionStep{T}.SetSelectedStep"/> of the <see cref="DecisionStep{T}"/> class to
        /// send the corresponding step from this client to the server.
        /// </summary>
        /// <param name="decisionTuple">The <see cref="DecisionStep{T}.StepDecisionTuple{TT}"/> that was selected.</param>
        protected override void SetSelectedStep(StepDecisionTuple<Button> decisionTuple)
        {
            SetSelectedStep_ServerRpc(NetworkManager.LocalClientId, decisionTuple.step);
        }

        /// <summary>
        /// <b>Server Rpc</b><br/>
        /// Called by <see cref="SetSelectedStep"/>; it sets <see cref="DecisionStep{T}._selectedStep"/>.
        /// </summary>
        /// <param name="networkManagerLocalClientId">The ID of the calling client.</param>
        /// <param name="stepReference">An instance of <see cref="NetworkBehaviourReference"/> that holds a <see cref="BaseTrainingStep"/> reference.</param>
        [ServerRpc(RequireOwnership = false, Delivery = RpcDelivery.Reliable)]
        private void SetSelectedStep_ServerRpc(ulong networkManagerLocalClientId,
                                               NetworkBehaviourReference stepReference)
        {
            if (stepReference.TryGet(out BaseTrainingStep step))
            {
                _selectedStep.Value = step;
            }
        }
    }
}
#if NMY_ENABLE_UNITY_ATOMS
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityAtoms.BaseAtoms;
using Unity.Netcode;
using UnityEngine;

namespace NMY.VirtualRealityTraining.Steps
{
    /// <summary>
    /// A class for defining a training step that checks a boolean variable against a target value and then selects a step to continue with.
    /// This class extends the <see cref="DecisionStep{T}"/> class and is used to define a step that checks a boolean variable
    /// against a target value before deciding which step to continue with. The boolean variable and the target value can be specified using the
    /// <see cref="_boolVariable"/> field. The steps to continue with can be specified using the <see cref="DecisionStep{T}._stepTuples"/> field.
    /// </summary>
    /// <remarks>
    /// Note that this step requires exactly <b>TWO</b> _stepTuples, with their comparables set to "true" and "false".
    /// Otherwise it will wait indefinitely unless the BoolVariable already has the correct value when the step starts.
    /// Specifically, this step will NOT wait+listen for a state change of the BoolVariable when it is already running -
    /// use the regular <see cref="BoolVariableStep"/> for that.
    /// </remarks>
    public class BoolDecisionStep : DecisionStep<bool>
    {
        /// <summary>
        /// The boolean variable that this step will check.
        /// </summary>
        [SerializeField] private BoolVariable _boolVariable;

        /// <summary>
        /// Overrides the <see cref="BaseTrainingStep.ClientStepActionAsync"/> method of the <see cref="DecisionStep{T}"/> class to
        /// check the value of <see cref="_boolVariable"/> and set the appropriate step.
        /// </summary>
        /// <remarks>
        /// It <b>does not</b> wait for a value change!
        /// It sets the first step whose value is equals to <see cref="_boolVariable"/> as <see cref="DecisionStep{T}._selectedStep"/>.
        /// </remarks>
        /// <param name="ct">A cancellation token that can be used to cancel the asynchronous operation.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        protected override async UniTask ClientStepActionAsync(CancellationToken ct)
        {
            foreach (var stepTuple in _stepTuples)
            {
                if (_boolVariable.Value != stepTuple.comparable) continue;

                SetSelectedStep(stepTuple);
                await base.ClientStepActionAsync(ct);
                break;
            }
        }

        /// <summary>
        /// Overrides the <see cref="DecisionStep{T}.SetSelectedStep"/> of the <see cref="DecisionStep{T}"/> class to
        /// send the corresponding step from this client to the server.
        /// </summary>
        /// <param name="decisionTuple">The <see cref="DecisionStep{T}.StepDecisionTuple{TT}"/> that was selected.</param>
        protected override void SetSelectedStep(StepDecisionTuple<bool> decisionTuple)
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
#endif
#if NMY_ENABLE_UNITY_ATOMS

using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityAtoms.BaseAtoms;
using UnityEngine;

namespace NMY.VirtualRealityTraining.Steps
{
    /// <summary>
    /// A class for defining a training step that waits for a boolean variable to match a target value.
    /// This class extends the <see cref="BaseTrainingStep"/> class and is used to define a step that waits for a boolean
    /// variable to match a specified target value before completing the step.
    /// 
    /// The <see cref="_boolCondition"/> and <see cref="_targetValue"/> fields should be set to specify
    /// the boolean variable to monitor and the target value to match.
    /// </summary>
    /// <remarks>
    /// This component is only available if 'Unity Atoms' is included in the Package Manager.
    /// </remarks>
    public class BoolVariableStep : BaseTrainingStep
    {
        /// <summary>
        /// The boolean variable that this step will wait for.
        /// </summary>
        [SerializeField] private BoolVariable _boolCondition;

        /// <summary>
        /// The boolean value that represents the target value to match.
        /// </summary>
        [SerializeField] private bool _targetValue = true;

        /// <summary>
        /// Overrides the <see cref="BaseTrainingStep.ClientStepActionAsync"/> method of the <see cref="BaseTrainingStep"/> class to
        /// asynchronously wait until the bool condition matches the target value before signaling that the step is finished for the local client.
        /// </summary>
        /// <remarks>
        /// This method calls <see cref="WaitUntilBoolVariableMatches"/> to wait for the the condition and target values to match,
        /// and then calls <see cref="BaseTrainingStep.RaiseClientStepFinished"/> to signal that the step is finished.
        /// If the cancellation token is cancelled while waiting, <see cref="BaseTrainingStep.RaiseClientStepFinished"/> is still called.
        /// </remarks>
        /// <param name="ct">The cancellation token for the async operation.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        protected override async UniTask ClientStepActionAsync(CancellationToken ct)
        {
            try
            {
                await WaitUntilBoolVariableMatches(ct);
                RaiseClientStepFinished();
            }
            catch (OperationCanceledException)
            {
                RaiseClientStepFinished();
            }
        }

        /// <summary>
        /// Asynchronously waits until the the values of <see cref="_boolCondition"/> and <see cref="_targetValue"/> are equal.
        /// </summary>
        /// <remarks>
        /// It waits until this condition is met or until the cancellation token is cancelled.
        /// </remarks>
        /// <param name="ct">The cancellation token for the async operation.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        private async UniTask WaitUntilBoolVariableMatches(CancellationToken ct) =>
            await UniTask.WaitUntil(() => _boolCondition.Value == _targetValue, cancellationToken: ct);

        /// <summary>
        /// Overrides the <see cref="BaseTrainingStep.ExecuteMoveToStepAction"/> method of <see cref="BaseTrainingStep"/>
        /// to automatically set the <see cref="BoolVariable.Value"/> of <see cref="BoolVariable"/>
        /// to the value of <see cref="_targetValue"/>.
        /// </summary>
        /// <param name="ct"></param>
        /// <remarks>
        /// make sure that whoever is supposed to set this value in the first place is listening to its event and reacts
        /// accordingly to this external forced-override.
        /// </remarks>
        /// <returns>A task representing the asynchronous operation.</returns>
        protected override async UniTask ExecuteMoveToStepAction(CancellationToken ct = default)
        {
            _boolCondition.SetValue(_targetValue);
            await base.ExecuteMoveToStepAction(ct);
        }
    }
}

#endif
using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using NMY.VirtualRealityTraining.VirtualAssistant;
using UnityEngine;

namespace NMY.VirtualRealityTraining.Steps.VirtualAssistant
{
    /// <summary>
    /// A training step that involves the virtual assistant moving to a target position
    /// before signaling that the step is finished.
    /// The virtual assistant will move according to the movement data specified in the <see cref="_moveData"/> property.
    /// </summary>
    public class VirtualAssistantMoveStep : BaseVirtualAssistantTrainingStep
    {
        /// <summary>
        /// The <see cref="VirtualAssistantMoveData"/> object that contains the details of the movement action
        /// performed by the virtual assistant.
        /// </summary>
        [SerializeField] private VirtualAssistantMoveData _moveData;

        /// <summary>
        /// Overrides the <see cref="BaseTrainingStep.ClientStepActionAsync"/> method of the <see cref="BaseTrainingStep"/> class to asynchronously wait
        /// for the virtual assistant to move to the desired position before signaling that the step is finished for the local client.
        /// </summary>
        /// <remarks>
        /// If the cancellation token is cancelled while waiting, the virtual assistant is moved instantly to the desired
        /// position and <see cref="BaseTrainingStep.RaiseClientStepFinished"/> is still called.
        /// </remarks>
        /// <param name="ct">The cancellation token for the async operation.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        protected override async UniTask ClientStepActionAsync(CancellationToken ct)
        {
            try
            {
                // Move the virtual assistant to the specified location.
                await VirtualRealityTraining.VirtualAssistant.VirtualAssistant.instance.Move(_moveData, ct);
                RaiseClientStepFinished();
            }
            catch (OperationCanceledException)
            {
                // If the operation was canceled, move the virtual assistant to the specified location instantly.
                await VirtualRealityTraining.VirtualAssistant.VirtualAssistant.instance.MoveInstantly(_moveData);
                RaiseClientStepFinished();
            }
        }
        
        /// <summary>
        /// Overrides the <see cref="AbstractTimeBasedStep.ServerStepActionAsync"/> method of the <see cref="AbstractTimeBasedStep"/> class to
        /// asynchronously wait for the virtual assistant to move to the desired position on server side.
        /// </summary>
        protected override async UniTask ServerStepActionAsync(CancellationToken ct)
        {
            try
            {
                await base.ServerStepActionAsync(ct);
                // Move the virtual assistant to the specified location.
                await VirtualRealityTraining.VirtualAssistant.VirtualAssistant.instance.Move(_moveData, ct);
            }
            catch (OperationCanceledException)
            {
                // If the operation was canceled, move the virtual assistant to the specified location instantly.
                await VirtualRealityTraining.VirtualAssistant.VirtualAssistant.instance.MoveInstantly(_moveData);
            }
        }

        /// <summary>
        /// Overrides the <see cref="BaseTrainingStep.ExecuteMoveToStepAction"/> method of <see cref="BaseTrainingStep"/>
        /// to automatically move the virtual assistant instantly to the desired position when the step is skipped.
        /// </summary>
        /// <param name="ct"></param>
        /// <returns>A task representing the asynchronous operation.</returns>
        protected override async UniTask ExecuteMoveToStepAction(CancellationToken ct = default)
        {
            await VirtualRealityTraining.VirtualAssistant.VirtualAssistant.instance.MoveInstantly(_moveData);
            await base.ExecuteMoveToStepAction(ct);
        }
        
        protected override string GameObjectPrefixName() => "[VA Move Step]";
    }
}

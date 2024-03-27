#if NMY_ENABLE_AUTOHAND

using System;
using System.Threading;
using Autohand;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace NMY.VirtualRealityTraining.Steps
{
    /// <summary>
    /// A training step that waits for a grabbable object to be held.
    /// </summary>
    /// <remarks>
    /// This component is only available if 'AutoHand' is included in the Package Manager.
    /// </remarks>
    /// <seealso cref="ReleaseGrabStep"/>
    public class GrabStep : BaseTrainingStep
    {
        /// <summary>
        /// A reference to the <see cref="Grabbable"/> component that should be grabbed.
        /// </summary>
        [Header("Grab Settings")]
        [SerializeField] private Grabbable _grabbable;
        
        /// <summary>
        /// Overrides the <see cref="BaseTrainingStep.ClientStepActionAsync"/> method of the <see cref="BaseTrainingStep"/> class to
        /// asynchronously wait until the grabbable object is held before signaling that the step is finished for the local client.
        /// </summary>
        /// <remarks>
        /// This method calls <see cref="WaitForGrabbableHeld"/> to wait until the grabbable object is held,
        /// and then calls <see cref="BaseTrainingStep.RaiseClientStepFinished"/> to signal that the step is finished.
        /// If the cancellation token is cancelled while waiting, <see cref="BaseTrainingStep.RaiseClientStepFinished"/> is still called.
        /// </remarks>
        /// <param name="ct">The cancellation token for the async operation.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        protected override async UniTask ClientStepActionAsync(CancellationToken ct)
        {
            try
            {
                await WaitForGrabbableHeld(ct);
                RaiseClientStepFinished();
            }
            catch (OperationCanceledException)
            {
                RaiseClientStepFinished();
            }
        }

        /// <summary>
        /// Asynchronously wait until the grabbable object is held.
        /// </summary>
        /// <remarks>
        /// It waits until this condition is met or until the cancellation token is cancelled.
        /// </remarks>
        /// <param name="ct">A cancellation token that can be used to cancel the operation.</param>
        /// <returns>A task that completes when the grabbable object is held.</returns>
        private UniTask WaitForGrabbableHeld(CancellationToken ct) => 
            UniTask.WaitUntil(() => _grabbable.IsHeld(), cancellationToken: ct);
        
        protected override string GameObjectPrefixName() => "[Grab Step]";

    }
}

#endif

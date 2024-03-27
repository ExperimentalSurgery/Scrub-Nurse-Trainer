#if NMY_ENABLE_AUTOHAND

using System;
using System.Threading;
using Autohand;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace NMY.VirtualRealityTraining.Steps
{
    /// <summary>
    /// A training step that waits for a grabbable object to be released.
    /// Optionally, it waits until the object is resting. 
    /// </summary>
    /// <remarks>
    /// This component is only available if 'AutoHand' is included in the Package Manager.
    /// </remarks>
    /// <seealso cref="GrabStep"/>
    public class ReleaseGrabStep : BaseTrainingStep
    {
        /// <summary>
        /// The grabbable object to be released.
        /// </summary>
        [Header("Grab Settings")]
        [SerializeField] private Grabbable _grabbable;
        
        /// <summary>
        /// The <see cref="Rigidbody"/> of the grabbable to be released.
        /// </summary>
        [SerializeField] private Rigidbody _grabbableRigidbody;
        
        /// <summary>
        /// Whether to wait for the grabbable object to be resting before completing the step.
        /// </summary>
        [SerializeField] private bool _waitForResting = true;
        
        /// <summary>
        /// Whether the grabbable object was held by this client before the step started.
        /// </summary>
        private bool _wasLocallyHeld;

        protected override async UniTask ClientStepActionAsync(CancellationToken ct)
        {
            try
            {
                await WaitForGrabbableReleased(ct);
                if (_waitForResting) await WaitForResting(ct);
                RaiseClientStepFinished();
            }
            catch (OperationCanceledException)
            {
                RaiseClientStepFinished();
            }
        }

        /// <summary>
        /// Asynchronously waits until the grabbable object is released by any client or this client.
        /// </summary>
        /// <param name="ct">The cancellation token to use for cancelling the operation.</param>
        /// <returns>A task that completes when the grabbable object is released.</returns>
        private UniTask WaitForGrabbableReleased(CancellationToken ct) => 
            UniTask.WaitUntil(() => !_wasLocallyHeld || !_grabbable.IsHeld(), cancellationToken: ct);

        /// <summary>
        /// Asynchronously waits until the grabbable object is released and resting.
        /// </summary>
        /// <param name="ct">The cancellation token to use for cancelling the operation.</param>
        /// <returns>A task that completes when the grabbable object is released and resting.</returns>
        private UniTask WaitForResting(CancellationToken ct) => 
            UniTask.WaitUntil(() => !_grabbable.IsHeld() && _grabbableRigidbody.velocity.magnitude <= 0.001, cancellationToken: ct);


        /// <summary>
        /// Overrides the <see cref="BaseTrainingStep.ExecuteMoveToStepAction"/> method of <see cref="BaseTrainingStep"/>
        /// to automatically release the grabbable object.
        /// </summary>
        /// <param name="ct"></param>
        /// <returns>A task representing the asynchronous operation.</returns>
        protected override async UniTask ExecuteMoveToStepAction(CancellationToken ct = default)
        {
            _grabbable.HandsRelease();
            await base.ExecuteMoveToStepAction(ct);
        }

        /// <summary>
        /// Overrides the method <see cref="BaseTrainingStep.ResetStepState"/> of the <see cref="BaseTrainingStep"/> class to
        /// reset the value of <see cref="_wasLocallyHeld"/>.
        /// </summary>
        protected override void ResetStepState()
        {
            base.ResetStepState();
            _wasLocallyHeld = _grabbable.IsHeld();
        }
        
        protected override string GameObjectPrefixName() => "[Release Step]";
    }
}

#endif

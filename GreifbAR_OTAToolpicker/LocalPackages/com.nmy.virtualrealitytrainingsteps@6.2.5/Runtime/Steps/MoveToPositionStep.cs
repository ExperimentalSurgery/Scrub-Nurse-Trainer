using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
#if NMY_ENABLE_XR_INTERACTION_TOOLKIT
using UnityEngine.XR.Interaction.Toolkit;
#endif

namespace NMY.VirtualRealityTraining.Steps
{
    /// <summary>
    /// A training step that waits until the user moved to a specified position.
    /// </summary>
    public class MoveToPositionStep : BaseTrainingStep
    {
        
#region Serialized Fields

        /// <summary>
        /// A <see cref="Transform"/> that specifies the position that should be moved to the desired position during this step.
        /// This could be the position of the user's camera or some other GameObject.
        /// This property must be set in the Unity Editor in order for the step to function properly.
        /// </summary>
        [Header("Move Settings")]
        [Tooltip("The position that should be moved to the desired position.")]
        [SerializeField] private Transform _sourcePosition;

        /// <summary>
        /// A <see cref="Transform"/> that specifies the desired position that the source position should be moved to.
        /// This could be a GameObject representing a target location in the game world.
        /// </summary>
        [Tooltip("The desired position of the source.")]
        [SerializeField] private Transform _desiredPosition;

        /// <summary>
        /// A value that specifies the radius around the desired position within which the user must be to complete the training step.
        /// When the user moves within this radius, the ClientStepFinished event will be raised,
        /// indicating that the step is complete.
        /// </summary>
        [Tooltip("The radius around the desired position to trigger the step complete event.")]
        [SerializeField] private float _activationRadius = 1;

#endregion
        
        /// <summary>
        /// Overrides the <see cref="BaseTrainingStep.ClientStepActionAsync"/> method of the <see cref="BaseTrainingStep"/> class to
        /// asynchronously wait until the player is within the activation radius specified by <see cref="_activationRadius"/>
        /// before signaling that the step is finished for the local client.
        /// </summary>
        /// <remarks>
        /// This method calls <see cref="WaitForClientInRadius"/> to wait until the player is within the activation radius,
        /// and then calls <see cref="BaseTrainingStep.RaiseClientStepFinished"/> to signal that the step is finished.
        /// If the cancellation token is cancelled while waiting, <see cref="BaseTrainingStep.RaiseClientStepFinished"/> is still called.
        /// </remarks>
        /// <param name="ct">The cancellation token for the async operation.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        protected override async UniTask ClientStepActionAsync(CancellationToken ct)
        {
            try
            {
                await WaitForClientInRadius(ct);
                RaiseClientStepFinished();
            }
            catch (OperationCanceledException )
            {
                RaiseClientStepFinished();
            }
        }

        /// <summary>
        /// Asynchronously waits until the user moves within the radius specified by the <see cref="_activationRadius"/> property.
        /// </summary>
        /// <param name="ct">The cancellation token to use to cancel the wait operation if the step is cancelled or otherwise interrupted.</param>
        /// <returns>A <see cref="UniTask"/> object that can be awaited until the user is within the radius.</returns>
        private UniTask WaitForClientInRadius(CancellationToken ct) => 
            UniTask.WaitUntil(IsWithinRadius, cancellationToken: ct);

        /// <summary>
        /// Determines whether the user is within the radius specified by the <see cref="_activationRadius"/> property.
        /// </summary>
        /// <returns>A value indicating whether the user is within the radius.</returns>
        private bool IsWithinRadius()
        {
            var sourcePosition  = _sourcePosition.position;
            var desiredPosition = _desiredPosition.position;

            var vecDesired = new Vector3(desiredPosition.x, 0, desiredPosition.z);
            var vecSource  = new Vector3(sourcePosition.x, 0, sourcePosition.z);

            var currentDistance = Vector3.Distance(vecDesired, vecSource);
            return currentDistance <= _activationRadius;
        }

        /// <summary>
        /// Overrides the <see cref="BaseTrainingStep.ExecuteMoveToStepAction"/> method of <see cref="BaseTrainingStep"/>
        /// to automatically teleport the user to <see cref="_desiredPosition"/>.
        /// </summary>
        /// <param name="ct"></param>
        /// <remarks>
        /// This only works if the XR Interaction Toolkit is enabled in the project.
        /// </remarks>
        /// <returns>A task representing the asynchronous operation.</returns>
        protected override async UniTask ExecuteMoveToStepAction(CancellationToken ct = default)
        {
#if NMY_ENABLE_XR_INTERACTION_TOOLKIT
            var teleportationProvider = FindObjectOfType<TeleportationProvider>();

            if (teleportationProvider)
            {
                var teleportRequest = new TeleportRequest
                {
                    destinationPosition = _desiredPosition.position,
                    matchOrientation    = MatchOrientation.TargetUp
                };

                teleportationProvider.QueueTeleportRequest(teleportRequest);
            }
#endif
            await base.ExecuteMoveToStepAction(ct);
        }
        
        protected override string GameObjectPrefixName() => "[Move Step]";

    }
}

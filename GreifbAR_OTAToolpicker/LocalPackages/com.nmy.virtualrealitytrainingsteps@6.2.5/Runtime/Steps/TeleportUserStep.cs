#if NMY_ENABLE_XR_INTERACTION_TOOLKIT

using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

namespace NMY.VirtualRealityTraining.Steps
{
    /// <summary>
    /// A training step that teleports the user to a specified destination.
    /// </summary>
    /// <remarks>
    /// This component is only available if 'XR Interaction Toolkit' is included in the Package Manager.
    /// </remarks>
    public class TeleportUserStep : BaseTrainingStep
    {
        /// <summary>
        /// The destination to teleport the user to.
        /// </summary>
        [SerializeField] private Transform _destination;

        /// <summary>
        /// The teleportation provider component used to initiate the teleportation.
        /// </summary>
        [SerializeField] private TeleportationProvider _teleportationProvider;

        /// <summary>
        /// The orientation matching mode to use when teleporting the user.
        /// </summary>
        [SerializeField] private MatchOrientation _matchOrientation;

        /// <summary>
        /// The teleport request to be queued by the teleportation provider.
        /// </summary>
        private TeleportRequest _teleportRequest;

        /// <summary>
        /// Overrides the <see cref="BaseTrainingStep.Start"/> method of the <see cref="BaseTrainingStep"/> class
        /// to initialize find the <see cref="_teleportationProvider"/> if not set in the inspector.
        /// </summary>
        protected override void Start()
        {
            base.Start();
            if (_teleportationProvider is null && !(_teleportationProvider = FindObjectOfType<TeleportationProvider>()))
                Debug.LogError($"{GetType()}: Teleportation Provider could not be found!", this);
        }

        /// <summary>
        /// Overrides the <see cref="BaseTrainingStep.ClientStepActionAsync"/> method of the <see cref="BaseTrainingStep"/> class to
        /// synchronously teleport the user to the new destination before signaling that the step is finished for the local client.
        /// </summary>
        /// <param name="ct">The cancellation token for the async operation.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        protected override async UniTask ClientStepActionAsync(CancellationToken ct)
        {
            try
            {
                TryTeleportUser();

                await UniTask.CompletedTask;
                RaiseClientStepFinished();
            }
            catch (OperationCanceledException)
            {
                RaiseClientStepFinished();
            }
        }

        /// <summary>
        /// Attempts to teleport the user to the specified destination.
        /// </summary>
        /// <returns>
        /// True if the user was teleported successfully, false otherwise.
        /// </returns>
        private bool TryTeleportUser()
        {
            if (_teleportationProvider is null) return false;
            if (_destination is null) return false;

            _teleportRequest = new TeleportRequest
            {
                destinationPosition = _destination.position,
                destinationRotation = _destination.rotation,
                matchOrientation    = _matchOrientation
            };

            _teleportationProvider.QueueTeleportRequest(_teleportRequest);
            return true;
        }

        /// <summary>
        /// Overrides the <see cref="BaseTrainingStep.ExecuteMoveToStepAction"/> method of <see cref="BaseTrainingStep"/>
        /// to automatically initiate the teleportation of the user to the specified destination.
        /// </summary>
        /// <param name="ct"></param>
        /// <returns>A task representing the asynchronous operation.</returns>
        protected override async UniTask ExecuteMoveToStepAction(CancellationToken ct = default)
        {
            TryTeleportUser();
            await base.ExecuteMoveToStepAction(ct);
        }

        protected override string GameObjectPrefixName() => "[Teleport Step]";
    }
}

#endif
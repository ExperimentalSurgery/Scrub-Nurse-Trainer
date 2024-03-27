using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace NMY.VirtualRealityTraining.Steps
{
    /// <summary>
    /// A class for defining a training step that waits for the player to enter a trigger area before completing.
    /// This class extends the <see cref="BaseTrainingStep"/> class and is used to define a step that waits for the player to enter a trigger area
    /// defined by a <see cref="TriggerEvent"/> component.
    /// </summary>
    /// <remarks>
    /// The `ClientStepActionAsync` method waits for the player to enter the trigger area, as detected by the <see cref="TriggerEvent"/> events,
    /// and then calls <see cref="BaseTrainingStep.RaiseClientStepFinished"/> to mark the step as finished for the local client.
    /// </remarks>
    public class ActivationAreaStep : BaseTrainingStep
    {
        /// <summary>
        /// This field is used to store a reference to a <see cref="TriggerEvent"/> component.
        /// </summary>
        /// <remarks>
        /// The <see cref="TriggerEvent"/> component has events for when a collider enters or exits the trigger area defined by the component.
        /// The `ActivationAreaStep` class registers event handlers for these events in the
        /// <see cref="BaseTrainingStep.PreStepActionAsync"/> method, which is called when the step is about to start.
        /// The <see cref="BaseTrainingStep.PostStepActionAsync"/> method is called when the step is finished
        /// and it unregisters the event handlers for the <see cref="TriggerEvent"/> events.
        /// </remarks>
        [SerializeField] private TriggerEvent _triggerEvent;

        /// <summary>
        /// This field is used to track whether the player has entered the trigger area defined by the <see cref="TriggerEvent"/> component.
        /// </summary>
        /// <remarks>
        /// It is set to `true` in the `OnTriggerEntered` method when a collider enters the trigger area,
        /// and it is used in the `WaitForEntered` method to determine when to stop waiting for the player to enter the trigger area.
        /// </remarks>
        private bool _hasEntered;

#region Overrides

        /// <summary>
        /// Overrides the method <see cref="BaseTrainingStep.ResetStepState"/> of the <see cref="BaseTrainingStep"/> class to
        /// reset the <see cref="_hasEntered"/> to false.
        /// </summary>
        protected override void ResetStepState()
        {
            base.ResetStepState();
            _hasEntered = false;
        }

        /// <summary>
        /// Overrides the <see cref="BaseTrainingStep.PreStepActionAsync"/> method of the <see cref="BaseTrainingStep"/> class to
        /// register event handlers for the <see cref="TriggerEvent"/> events, which are used to detect when the player enters the trigger area.
        /// </summary>
        /// <param name="ct">The cancellation token for the async operation.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        protected override async UniTask PreStepActionAsync(CancellationToken ct)
        {
            await base.PreStepActionAsync(ct);
            _triggerEvent.onTriggerEnter.AddListener(OnTriggerEntered);
            _triggerEvent.onTriggerStay.AddListener(OnTriggerEntered);
        }

        /// <summary>
        /// Overrides the <see cref="BaseTrainingStep.PostStepActionAsync"/> method of the <see cref="BaseTrainingStep"/> class to
        /// unregister the event handlers for the <see cref="TriggerEvent"/> events that were registered in the <see cref="PreStepActionAsync"/> method.
        /// </summary>
        /// <param name="ct">The cancellation token for the async operation.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        protected override async UniTask PostStepActionAsync(CancellationToken ct)
        {
            await base.PostStepActionAsync(ct);
            _triggerEvent.onTriggerEnter.RemoveListener(OnTriggerEntered);
            _triggerEvent.onTriggerStay.RemoveListener(OnTriggerEntered);
        }

        /// <summary>
        /// Overrides the <see cref="BaseTrainingStep.ClientStepActionAsync"/> method of the <see cref="BaseTrainingStep"/> class to
        /// wait for the client to enter the trigger area, as detected by the <see cref="TriggerEvent"/> events.
        /// </summary>
        /// <remarks>
        /// This method calls <see cref="WaitForEntered"/> to wait for the client enter the trigger area,
        /// and then calls <see cref="BaseTrainingStep.RaiseClientStepFinished"/> to signal that the step is finished
        /// for the local client.
        /// If the cancellation token is cancelled while waiting, <see cref="BaseTrainingStep.RaiseClientStepFinished"/> is still called.
        /// </remarks>
        /// <param name="ct">The cancellation token for the async operation.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        protected override async UniTask ClientStepActionAsync(CancellationToken ct)
        {
            try
            {
                if (_triggerEvent != null) await WaitForEntered(ct);
                else Debug.LogWarning($"{GetType()}: Trigger Event could not be found, skip this step.", this);
                RaiseClientStepFinished();
            }
            catch (OperationCanceledException)
            {
                RaiseClientStepFinished();
            }
        }
        
        protected override string GameObjectPrefixName() => "[Activation Step]";

#endregion
        /// <summary>
        /// The event handler for the <see cref="TriggerEvent.onTriggerEnter"/> event of <see cref="TriggerEvent"/>.
        /// Called when a collider enters the trigger area.
        /// </summary>
        /// <param name="other">The collider that entered the trigger area.</param>
        private void OnTriggerEntered(Collider other)
        {
            _hasEntered = true;
        }
        
        /// <summary>
        /// Asynchronously waits until the collider has entered the trigger area.
        /// </summary>
        /// <remarks>
        /// It waits until this condition is met or until the cancellation token is cancelled.
        /// </remarks>
        /// <param name="ct">The cancellation token for the async operation.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        private UniTask WaitForEntered(CancellationToken ct) => 
            UniTask.WaitUntil(() => _hasEntered, cancellationToken: ct);
    }
}
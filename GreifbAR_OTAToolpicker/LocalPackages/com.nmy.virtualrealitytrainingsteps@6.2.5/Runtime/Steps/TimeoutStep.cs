using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace NMY.VirtualRealityTraining.Steps
{
    /// <summary>
    /// A training step that waits for a given amount of time to elapse before completing.
    /// <remarks>
    /// This class derives from the <see cref="AbstractTimeBasedStep"/> class and uses the concept of "delta time"
    /// to calculate the amount of time remaining before the timeout is reached.
    /// The delta time is the amount of time that has elapsed since the step was started on the server.
    /// </remarks>
    /// <seealso cref ="AbstractTimeBasedStep" />
    /// <seealso cref ="TimeoutStep.WaitForTimeout(CancellationToken)" />
    /// <seealso cref ="TimeoutStep.RemainingTime(double)" />
    /// <seealso cref ="TimeoutStep.CorrectedTime(double)" />
    /// </summary>
    public class TimeoutStep : AbstractTimeBasedStep
    {
        /// <summary>
        /// The amount of time in seconds the activity should take before timing out.
        /// </summary>
        [SerializeField] private float _activityTimeout;

        /// <summary>
        /// Overrides the <see cref="AbstractTimeBasedStep.ClientStepActionAsync"/> method of the <see cref="AbstractTimeBasedStep"/> class to
        /// asynchronously wait for the timeout to be reached before signaling that the step is finished for the local client.
        /// </summary>
        /// <remarks>
        /// This method calls <see cref="WaitForTimeout"/> to wait for the timeout to be reached,
        /// and then calls <see cref="BaseTrainingStep.RaiseClientStepFinished"/> to signal that the step is finished.
        /// If the cancellation token is cancelled while waiting, <see cref="BaseTrainingStep.RaiseClientStepFinished"/> is still called.
        /// </remarks>
        /// <param name="ct">The cancellation token for the async operation.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        protected override async UniTask ClientStepActionAsync(CancellationToken ct)
        {
            try
            {
                await base.ClientStepActionAsync(ct);
                await WaitForTimeout(ct);
                RaiseClientStepFinished();
            }
            catch (OperationCanceledException)
            {
                RaiseClientStepFinished();
            }
        }

        /// <summary>
        /// Overrides the <see cref="AbstractTimeBasedStep.ServerStepActionAsync"/> method of the <see cref="AbstractTimeBasedStep"/> class to
        /// asynchronously wait for the timeout to be reached on the server.
        /// </summary>
        protected override async UniTask ServerStepActionAsync(CancellationToken ct)
        {
            try
            {
                await base.ServerStepActionAsync(ct);
                await WaitForTimeout(ct);
            }
            catch (OperationCanceledException) { }
        }
        
        /// <summary>
        /// Asynchronously waits until the specified timeout has elapsed.
        /// </summary>
        /// <param name ="ct">The cancellation token for the async operation.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        private UniTask WaitForTimeout(CancellationToken ct) =>
            UniTask.Delay(TimeSpan.FromSeconds(RemainingTime(_activityTimeout)), cancellationToken: ct);
       
        protected override string GameObjectPrefixName() => "[Timeout Step]";
    }
}

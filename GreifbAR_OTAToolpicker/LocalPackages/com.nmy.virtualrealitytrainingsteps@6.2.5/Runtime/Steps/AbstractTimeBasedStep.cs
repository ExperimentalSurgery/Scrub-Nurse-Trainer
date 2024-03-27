using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Unity.Netcode;

namespace NMY.VirtualRealityTraining.Steps
{
    /// <summary>
    /// <para>
    /// An abstract class for defining and managing synchronized training steps that are based on time in a networked environment.
    /// </para>
    /// 
    /// <para>
    /// This class is a <see cref="NetworkBehaviour"/> and extends the <see cref="BaseTrainingStep"/> class to provide properties
    /// and methods for tracking time and calculating elapsed time. It has a <see cref="NetworkVariable{T}"/> called 
    /// <see cref="_stepStartedTime"/> that is used to track the time when the step was started on the server, and provides 
    /// properties and methods for calculating the elapsed time and remaining time from a given value.
    /// This class should be subclassed to define the specific actions and behaviors for a time-based training step.
    /// </para>
    /// 
    /// <para>
    /// Example:<br/>
    /// - Host A starts a tasks that waits for 10 minutes. After some time, Client B connects and should not wait the whole 10 minutes.<br/>
    /// - An AudioSource is playing vor Host A. Client B connects later and it's AudioSource should start where Host A currently is. 
    /// </para>
    /// </summary>
    public abstract class AbstractTimeBasedStep : BaseTrainingStep
    {
        /// <summary>
        /// A <see cref="NetworkVariable{T}"/> that is used to track the time when the step was started on the server.
        /// </summary>
        private readonly NetworkVariable<double> _stepStartedTime = new(-1);

#region Properties
        
        /// <summary>
        /// The time the step was started on the server.
        /// </summary>
        protected double stepStartedTime => Math.Max(0, _stepStartedTime.Value);

        /// <summary>
        /// The amount of time that has elapsed since the step was started on the server.
        /// </summary>
        protected double deltaTime
        {
            get
            {
                var value = NetworkManager.NetworkTimeSystem.ServerTime - stepStartedTime;
                return value < 1f ? 0 : value;
            }
        }

        /// <summary>
        /// Returns the amount of time remaining from the given value.
        /// </summary>
        /// <param name="value">The value to calculate the remaining time from.</param>
        /// <returns>The amount of time remaining from the given value.</returns>
        protected double RemainingTime(double value)    => Math.Max(0, value - deltaTime);
        
        /// <summary>
        /// Returns the amount of time remaining from the given value.
        /// </summary>
        /// <param name="maxValue">The maximum value to calculate the corrected time from.</param>
        /// <returns>The corrected amount of time remaining from the given value.</returns>
        protected double CorrectedTime(double maxValue) => Math.Min(deltaTime, maxValue);

#endregion

#region Overrides

        /// <summary>
        /// Overrides the <see cref="BaseTrainingStep.PreStepActionAsync"/> method of the <see cref="BaseTrainingStep"/> class to
        /// initialize <see cref="_stepStartedTime"/> server-side with the current server time.
        /// </summary>
        /// <param name="ct">The cancellation token for the async operation.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        protected override async UniTask PreStepActionAsync(CancellationToken ct)
        {
            await base.PreStepActionAsync(ct);

            if (IsServer)
            {
                _stepStartedTime.Value = NetworkManager.NetworkTimeSystem.ServerTime;
            }
        }

        /// <summary>
        /// Overrides the <see cref="BaseTrainingStep.ClientStepActionAsync"/> method of the <see cref="BaseTrainingStep"/> class to
        /// asynchronously wait until the start time for the step is set on the server.
        /// </summary>
        /// <remarks>
        /// This method calls <see cref="WaitUntilStartTimeSet"/> to wait until the start time is set. It does <b>NOT</b>
        /// <see cref="BaseTrainingStep.RaiseClientStepFinished"/> after waiting, this must be implemented in the
        /// derived classes.
        /// If the cancellation token is cancelled while waiting, <see cref="BaseTrainingStep.RaiseClientStepFinished"/> is still called.
        /// </remarks>
        /// <param name="ct">The cancellation token for the async operation.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        protected override async UniTask ClientStepActionAsync(CancellationToken ct)
        {
            try
            {
                await WaitUntilStartTimeSet(ct);
            }
            catch (OperationCanceledException)
            {
                RaiseClientStepFinished();
            }
        }

        /// <summary>
        /// Overrides the <see cref="BaseTrainingStep.ServerStepActionAsync"/> method of the <see cref="BaseTrainingStep"/> class to
        /// asynchronously wait until the start time for the step is set on the server.
        /// </summary>
        /// <param name="ct">The cancellation token for the async operation.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        protected override async UniTask ServerStepActionAsync(CancellationToken ct)
        {
            try
            {
                await base.ServerStepActionAsync(ct);
                await WaitUntilStartTimeSet(ct);
            }
            catch (OperationCanceledException)
            {
            }
        }
        
        protected override void ResetStepState()
        {
            base.ResetStepState();
            if (IsServer) _stepStartedTime.Value = -1;
        }

#endregion
        
        /// <summary>
        /// Waits until the start time for the step is set on the server.
        /// </summary>
        /// <param name="ct">The cancellation token for the async operation.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        private async UniTask WaitUntilStartTimeSet(CancellationToken ct) =>
            await UniTask.WaitUntil(() => _stepStartedTime.Value > -1, cancellationToken: ct);
    }
}
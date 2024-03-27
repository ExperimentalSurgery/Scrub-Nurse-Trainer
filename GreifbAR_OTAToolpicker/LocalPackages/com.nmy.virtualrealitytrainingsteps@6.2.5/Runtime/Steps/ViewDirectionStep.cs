using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace NMY.VirtualRealityTraining.Steps
{
    /// <summary>
    /// A class for defining a training step that requires the player to look in a specific direction for a certain amount of time.
    /// This class extends the <see cref="BaseTrainingStep"/> class and is used to define a step that requires the player to look in a specific direction,
    /// as defined by the <see cref="_desiredDirection"/> field, for the amount of time specified by the <see cref="_successDelay"/> field.
    /// 
    /// The angle of view that the player must be looking within can be set using the <see cref="_angle"/> field.
    /// The transform to be looked at can be set using the <see cref="_target"/> field. If this field is not set, the main camera will be used as the target.
    /// </summary>
    public class ViewDirectionStep : BaseTrainingStep
    {
        /// <summary>
        /// The transform representing the object to be looked at.
        /// This is the object that the player must be looking at in order for the step to be considered complete.
        /// If this field is not set, the main camera will be used as the target.
        /// </summary>
        [Header("View Direction Config")]
        [Tooltip("The target transform to should look in the desired direction. If not specified, it uses the Camera Main Transform.")]
        [SerializeField] private Transform _target;

        /// <summary>
        /// The desired direction that the player should be looking in.
        /// This is the direction that the player must be looking in for the required amount of time, as specified by the <see cref="_successDelay"/> field,
        /// in order for the step to be considered finished.
        /// </summary>
        [SerializeField] private Transform _desiredDirection;

        /// <summary>
        /// The amount of time, in seconds, that the player must be looking in the desired direction before the step is considered finished.
        /// This value is used to calculate the lerp value for each frame.
        /// </summary>
        [SerializeField] private float _successDelay = 3.0f;

        /// <summary>
        /// The maximum allowed angle, in degrees, between the target's forward direction and the desired direction.
        /// If the angle between these two directions is greater than this value, the player is not considered to be looking in the desired direction.
        /// </summary>
        [SerializeField] private float _angle = 50f;

        /// <summary>
        /// The lerp value for the current frame.
        /// This value is used to track the time that the player has been looking in the desired direction.
        /// When the value is less than or equal to zero, it indicates that the player has been looking in the desired direction for the required amount of time.
        /// </summary>
        private float _lerpValue;

        /// <summary>
        /// Overrides the <see cref="BaseTrainingStep.Start"/> method of the <see cref="BaseTrainingStep"/> class
        /// to initialize the target transform and the lerp value.
        /// </summary>
        /// <remarks>
        /// If <see cref="_target"/> is not set, it is set to the main camera's transform.
        /// <see cref="_lerpValue"/> is set to the value of <see cref="_successDelay"/>.
        /// </remarks>
        protected override void Start()
        {
            base.Start();
            if (!_target) _target = Camera.main.transform;
            _lerpValue = _successDelay;
        }

        /// <summary>
        /// Overrides the <see cref="BaseTrainingStep.ClientStepActionAsync"/> method of the <see cref="BaseTrainingStep"/> class to
        /// asynchronously wait for the player to look in the desired direction before signaling that the step is finished for the local client.
        /// </summary>
        /// <remarks>
        /// This method calls <see cref="WaitForClientLookAtTarget"/> to wait for the player to look in the desired direction,
        /// and then calls <see cref="BaseTrainingStep.RaiseClientStepFinished"/> to signal that the step is finished.
        /// If the cancellation token is cancelled while waiting, <see cref="BaseTrainingStep.RaiseClientStepFinished"/> is still called.
        /// </remarks>
        /// <param name="ct">The cancellation token for the async operation.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        protected override async UniTask ClientStepActionAsync(CancellationToken ct)
        {
            try
            {
                await WaitForClientLookAtTarget(ct);
                RaiseClientStepFinished();
            }
            catch (OperationCanceledException)
            {
                RaiseClientStepFinished();
            }
        }

        /// <summary>
        /// Asynchronously waits until the player is looking in the desired direction within the specified tolerance.
        /// </summary>
        /// <remarks>
        /// This method uses the <see cref="IsViewDirectionInRange"/> method to determine whether the player is looking
        /// in the desired direction, and waits until this condition is met or until the cancellation token is cancelled.
        /// </remarks>
        /// <param name="ct">The cancellation token for the async operation.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        private UniTask WaitForClientLookAtTarget(CancellationToken ct) =>
            UniTask.WaitUntil(() =>
            {
                _lerpValue = IsViewDirectionInRange() ? _lerpValue - Time.deltaTime : _successDelay;
                return _lerpValue <= 0;
            }, cancellationToken: ct);

        protected override void ResetStepState()
        {
            base.ResetStepState();
            _lerpValue = _successDelay;
        }

        /// <summary>
        /// Overrides the <see cref="BaseTrainingStep.ExecuteMoveToStepAction"/> method of <see cref="BaseTrainingStep"/>
        /// to automatically set the player's desired direction when the step is skipped.
        /// </summary>
        /// <param name="ct"></param>
        /// <returns>A task representing the asynchronous operation.</returns>
        protected override async UniTask ExecuteMoveToStepAction(CancellationToken ct = default)
        {
            _target.LookAt(_desiredDirection);
            await base.ExecuteMoveToStepAction(ct);
        }

        /// <summary>
        /// Determines whether the player is looking in the desired direction within the specified tolerance.
        /// This method calculates the angle between the player's forward direction and the desired direction,
        /// and returns true if the angle is less than or equal to the specified tolerance, as determined by the `_angle` field.
        /// </summary>
        /// <returns>True if the player is looking in the desired direction within the specified tolerance, false otherwise.</returns>
        private bool IsViewDirectionInRange()
        {
            var targetPos  = _target.position;
            var desiredPos = _desiredDirection.position;

            var horizontalRelation = new Vector3(desiredPos.x, targetPos.y, desiredPos.z);
            var targetDir          = horizontalRelation - targetPos;

            var forward     = _target.forward;
            var angleDeg    = Vector3.Angle(targetDir, forward);
            var angleDegAbs = Mathf.Abs(angleDeg);

            var viewDirectionInRange = !(angleDegAbs > _angle);

            return viewDirectionInRange;
        }
    }
}
using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace NMY.VirtualRealityTraining.Steps
{
    /// <summary>
    /// A training step that waits until the user looks at a specified target for a certain amount of time.
    /// </summary>
    public class LookAtTargetStep : BaseTrainingStep
    {
        /// <summary>
        /// The camera used to determine whether the user is looking at the target.
        /// </summary>
        [Header("View Direction Config")]
        [SerializeField] private Transform _camera;

        /// <summary>
        /// The target that the user should look at.
        /// </summary>
        [SerializeField] private Transform _target;
        
        /// <summary>
        /// The amount of time that the user should take to look at the target before the step is considered complete.
        /// </summary>
        [SerializeField] private float     _successDelay = 3.0f;

        /// <summary>
        /// A value in the range [-1, 1] that determines whether the user should stand in front or behind the target.
        /// A value of -1 indicates that the user should stand behind the target, a value of 0 indicates that the user
        /// should stand next to the target, and a value of 1 indicates that the user should stand in front of the target.
        /// </summary>
        [Tooltip("Whether the user should stand in front or behind the target.\n" +
                 "-1 = behind target, 0 = next to target, 1 = before target")]
        [SerializeField] [Range(-1f, 1f)] private float _positionThreshold = 0.2f;

        /// <summary>
        /// A value in the range [-1, 1] that determines whether the user should look at the target or in the same direction as the target.
        /// A value of -1 indicates that the user looks in the opposite direction, a value of 0 indicates that the user
        /// looks perpendicular, and a value of 1 indicates that the user looks in the same direction as the target.
        /// </summary>
        [Tooltip("Whether the user should look at the target or in the same direction as the target.\n" +
                 "-1 = opposite direction, 0 = perpendicular, 1 = same directions")]
        [SerializeField] [Range(-1f, 1f)] private float _lookThreshold = -0.8f;

        /// <summary>
        /// A flag indicating whether to print debug values of the calculation.
        /// </summary>
        [SerializeField] private bool _debugValues;
        
        /// <summary>
        /// The lerp value for the current frame.
        /// This value is used to track the time that the player has been looking at the desired target.
        /// When the value is less than or equal to zero, it indicates that the player has been looking at the desired
        /// target for the required amount of time.
        /// </summary>
        private float _lerpValue;

        /// <summary>
        /// Overrides the <see cref="BaseTrainingStep.Start"/> method of the <see cref="BaseTrainingStep"/> class to
        /// initialize <see cref="_camera"/> to the main camera if not set.
        /// </summary>
        protected override void Start()
        {
            base.Start();
            if (!_camera && Camera.main != null) _camera = Camera.main.transform;
            _lerpValue = _successDelay;
        }

        /// <summary>
        /// Overrides the <see cref="BaseTrainingStep.ClientStepActionAsync"/> method of the <see cref="BaseTrainingStep"/> class to
        /// asynchronously wait for the player to look at the desired target before signaling that the step is finished for the local client.
        /// </summary>
        /// <remarks>
        /// This method calls <see cref="WaitForClientLookAtTarget"/> to wait for the player to look at the desired target,
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
        /// Asynchronously waits for the client to look at the target specified by the <see cref="_target"/> property
        /// for the amount of time specified by the <see cref="_successDelay"/> property.
        /// </summary>
        /// <param name="ct">A cancellation token that can be used to cancel the wait operation.</param>
        /// <returns>A task that represents the asynchronous wait operation.</returns>
        private UniTask WaitForClientLookAtTarget(CancellationToken ct) =>
            UniTask.WaitUntil(() =>
            {
                _lerpValue = IsLookingAtTarget() ? _lerpValue - Time.deltaTime : _successDelay;
                return _lerpValue <= 0;
            }, cancellationToken: ct);

        /// <summary>
        /// Overrides the method <see cref="BaseTrainingStep.ResetStepState"/> of the <see cref="BaseTrainingStep"/> class to
        /// reset the value of <see cref="_lerpValue"/>.
        /// </summary>
        protected override void ResetStepState()
        {
            base.ResetStepState();
            _lerpValue = _successDelay;
        }

        /// <summary>
        /// Determines whether the client is looking at the target specified by the `_target` property.
        /// </summary>
        /// <returns>`true` if the client is looking at the target, `false` otherwise.</returns>
        private bool IsLookingAtTarget()
        {
            // Calculate the direction from the target to the camera and the dot product of the direction and the target's
            // forward vector. This determines whether the user is standing in front of or behind the target.
            
            // https://twitter.com/freyaholmer/status/1200807790580768768?lang=de
            var targetPosition = _target.position;
            var cameraPosition = _camera.position;
            
            var direction =  (cameraPosition - targetPosition).normalized;
            var frontBackDot = Vector3.Dot(direction, _target.forward);
            
            // Check whether the user is standing in the right position relative to the target.
            var inRightPosition = _positionThreshold < 0
                ? frontBackDot <= _positionThreshold
                : frontBackDot >= _positionThreshold;
            
            // Calculate the dot product of the direction and the camera's forward vector. This determines whether the
            // user is looking at the target or in the same direction as the target.
            var cameraDot   = Vector3.Dot(direction, _camera.forward);
            var isLookingAt = _lookThreshold < 0 
                ? cameraDot <= _lookThreshold 
                : cameraDot >= _lookThreshold;
            
            if (_debugValues)
                Debug.Log($"{GetType()}: Position Dot Value: {frontBackDot} Look Dot Value: {cameraDot} {inRightPosition} {isLookingAt}", this);

            // Return true if the user is standing in the right position and looking at the target.
            return inRightPosition && isLookingAt;
        }
#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            if (_target == null) return;
            const float radius = 1f;
            
            var angle = Mathf.Acos(_positionThreshold) * Mathf.Rad2Deg;

            if (_positionThreshold < 0) angle = 180 - angle;
            
            // remove additional rotations from the target to get a flat surface
            // depending on the sign of the threshold, invert the forward direction
            var forward        = Quaternion.AngleAxis(-_target.rotation.eulerAngles.x, _target.right) * (_positionThreshold >= 0 ? _target.forward : -_target.forward);
            var rotatedForward = Quaternion.Euler(0, -angle, 0) * forward;

            var targetPosition = _target.position;

            Handles.DrawLine(targetPosition, targetPosition + rotatedForward * 1);
            Handles.DrawLine(targetPosition, targetPosition + Quaternion.AngleAxis(angle * 2, Vector3.up)  * rotatedForward * radius);
            Handles.DrawWireArc(targetPosition, Vector3.up, rotatedForward, angle * 2, radius);
            
            Handles.color = new Color(0.8f, 0, 0, 0.4f);
            Handles.DrawSolidArc(targetPosition, Vector3.up, rotatedForward, angle * 2, radius);
        }
#endif
        
        protected override string GameObjectPrefixName() => "[Look At Step]";
    }
}
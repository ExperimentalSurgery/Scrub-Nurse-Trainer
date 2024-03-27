using System;
using System.Collections.Generic;
using NMY.VirtualRealityTraining.Steps;
using UnityEngine;
using UnityEngine.Serialization;

namespace NMY.VirtualRealityTraining
{
    /// <summary>
    /// This class is an extension of the <see cref="BaseTrainingStep"/> and is responsible for
    /// triggering animations right after the activatables of a step gets activated and right before
    /// the activatables are deactivated. 
    /// </summary>
    /// <remarks>
    /// <para>
    /// The highest timeout for the given trigger event will be used as timeout in the step.
    /// </para>
    /// This component automatically changes the following timeouts for the corresponding events:
    /// <list type="bullet">
    /// <item><see cref="BaseTrainingStep.OnActivatablesActivated"/> and <see cref="BaseTrainingStep.timeoutActivatablesActivated"/></item>
    /// <item><see cref="BaseTrainingStep.OnActivatablesDeactivating"/> and <see cref="BaseTrainingStep.timeoutActivatablesDeactivating"/></item>
    /// <item><see cref="BaseTrainingStep.OnPersistantActivatablesActivated"/> and <see cref="BaseTrainingStep.timeoutPersistantActivatablesActivated"/></item>
    /// <item><see cref="BaseTrainingStep.OnPersistantActivatablesDeactivating"/> and <see cref="BaseTrainingStep.timeoutPersistantActivatablesDeactivating"/></item>
    /// </list>
    /// </remarks>
    public class ActivatablesAnimation : MonoBehaviour
    {
        /// <summary>
        /// A reference to the step to from which the (persistant) activatables should be animated.
        /// </summary>
        /// <remarks>
        /// If not set in the inspector, find the first <see cref="BaseTrainingStep"/> in parents.
        /// </remarks>
        [SerializeField] private BaseTrainingStep _step;

        /// <summary>
        /// A list of animation data for the activatables.
        /// </summary>
        [SerializeField] private List<AnimationData> _activatablesAnimation = new();


        [Serializable]
        internal class AnimationData
        {
            [SerializeField] private Animator _animator;

            [FormerlySerializedAs("_triggerEvent")]
            [SerializeField] private AnimationStepEvents _stepEvent;

            [FormerlySerializedAs("_triggerName")]
            [AnimatorParam(nameof(_animator))]
            [SerializeField] private string _animationTrigger;

            [AnimatorClip(nameof(_animator))]
            [SerializeField] private AnimationClip _animationClip;

            public Animator animator => _animator;

            public AnimationStepEvents stepEvent => _stepEvent;

            public string animationTrigger => _animationTrigger;

            public AnimationClip animationClip => _animationClip;

            internal enum AnimationStepEvents
            {
                None,
                ActivatablesActivated,
                ActivatablesDeactivating,
                PersistantActivatablesActivated,
                PersistantActivatablesDeactivating
            }
        }


        /// <summary>
        /// Finds the step.
        /// </summary>
        private void Awake()
        {
            if (_step != null) return;
            if (TryGetComponent(out _step)) return;
            if (!(_step = GetComponentInParent<BaseTrainingStep>(true)))
                Debug.LogError($"{GetType()}: Step could not be found!", this);
        }

        private void OnEnable()
        {
            foreach (var animations in _activatablesAnimation)
            {
                if (animations.animator == null)
                {
                    Debug.LogWarning($"{GetType()}: Animator could not be found!", this);
                    continue;
                }

                {
                    if (animations.animationClip == null)
                    {
                        Debug.LogWarning($"{GetType()}: Animation clip could not be found!", this);
                        continue;
                    }

                    void TriggerAnimation(object sender, BaseTrainingStepEventArgs args) =>
                        animations.animator.SetTrigger(animations.animationTrigger);

                    switch (animations.stepEvent)
                    {
                        case AnimationData.AnimationStepEvents.ActivatablesActivated:
                            _step.timeoutActivatablesActivated =
                                Mathf.Max(_step.timeoutActivatablesActivated, animations.animationClip.length);
                            _step.OnActivatablesActivated -= TriggerAnimation;
                            _step.OnActivatablesActivated += TriggerAnimation;
                            break;

                        case AnimationData.AnimationStepEvents.ActivatablesDeactivating:
                            _step.timeoutActivatablesDeactivating =
                                Mathf.Max(_step.timeoutActivatablesDeactivating, animations.animationClip.length);
                            _step.OnActivatablesDeactivating -= TriggerAnimation;
                            _step.OnActivatablesDeactivating += TriggerAnimation;
                            break;

                        case AnimationData.AnimationStepEvents.PersistantActivatablesActivated:
                            _step.timeoutPersistantActivatablesActivated = Mathf.Max(
                                _step.timeoutPersistantActivatablesActivated, animations.animationClip.length);
                            _step.OnPersistantActivatablesActivated -= TriggerAnimation;
                            _step.OnPersistantActivatablesActivated += TriggerAnimation;
                            break;

                        case AnimationData.AnimationStepEvents.PersistantActivatablesDeactivating:
                            _step.timeoutPersistantActivatablesDeactivating = Mathf.Max(
                                _step.timeoutPersistantActivatablesDeactivating, animations.animationClip.length);
                            _step.OnPersistantActivatablesDeactivating -= TriggerAnimation;
                            _step.OnPersistantActivatablesDeactivating += TriggerAnimation;
                            break;

                        case AnimationData.AnimationStepEvents.None:
                            break;

                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                }
            }
        }
    }
}
using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

#if NMY_ENABLE_UNITY_ATOMS
using UnityAtoms.BaseAtoms;
#endif

#if NMY_ENABLE_UNITY_ATOMS_TAGS
using UnityAtoms.Tags;
#endif

namespace NMY.VirtualRealityTraining.Steps
{
    /// <summary>
    /// A training step that requires the player to touch an object for a certain amount of time.
    /// This class is a subclass of <see cref="BaseTrainingStep"/> and is used to define a training step
    /// that requires the player to touch an object for a certain amount of time. The step will only be
    /// considered complete when the player has touched the object for the required amount of time. The
    /// step can be configured to play sounds when the player starts and stops touching the object, as well
    /// as when the player completes the touch interaction. The step can also be configured to require
    /// the player to touch specific colliders or GameObjects with specific atom tags.
    /// </summary>
    public class TouchStep : BaseTrainingStep
    {
        /// <summary>
        /// An instance of the `TriggerEvent` class that is used to define an event that is raised when the player
        /// touches the object to complete the step.
        /// </summary>
        /// <remarks>
        /// This event is added and removed in the `PreStepActionAsync` and
        /// `PostStepActionAsync` methods, respectively, and is handled by the `OnTriggerEnter`, `OnTriggerStay`, and
        /// `OnTriggerExit` methods, which track the player's interactions with the object to touch. When the required
        /// touch duration is reached, the `StepCompleteAction` method is called and the `OnObjectTouched` event is raised.
        /// </remarks>
        [Header("Touch Settings")]
        [SerializeField] private TriggerEvent _touchableTriggerEvent;

        /// <summary>
        /// The minimum duration that the player must touch the object to complete the step, in seconds.
        /// </summary>
        /// <remarks>
        /// This value is used in the `OnTriggerStay` method to determine when the `StepCompleteAction` method
        /// should be called and the `OnObjectTouched` event should be raised.
        /// </remarks>
        [SerializeField] private float _touchDuration;

        /// <summary>
        /// An audio source that will be played when the player enters the trigger area of the object to touch.
        /// </summary>
        /// <remarks>
        /// This audio source is played when the player starts touching the object to touch.
        /// </remarks>
        [Header("Sound Settings")]
        [SerializeField] private AudioSource _soundOnTouchEnter;

        /// <summary>
        /// An audio source that will be played when the player exits the trigger area of the object to touch.
        /// </summary>
        /// <remarks>
        /// This audio source is played when the player stops touching the object to touch before the touch
        /// duration has been reached.
        /// </remarks>
        [SerializeField] private AudioSource _soundOnTouchExit;

        /// <summary>
        /// An audio source that will be played when the player completes the touch interaction.
        /// </summary>
        ///<remarks>
        /// This audio source is played when the touch duration has been reached and the player is still
        /// touching the object to touch.
        /// </remarks>
        [SerializeField] private AudioSource _soundOnTouchComplete;

        /// <summary>
        /// A list of colliders that will be considered as triggers for this step.
        /// </summary>
        [Header("Trigger Settings")]
        [Tooltip(" A list of specific Colliders.")]
        [SerializeField] private List<Collider> _triggers = new();

        /// <summary>
        /// A list of game objects whose colliders in their hierarchies will be considered as triggers for this step.
        /// </summary>
        [Tooltip("A list of GameObjects, all Colliders in their hierarchies will be considered.")]
        [SerializeField] private List<GameObject> _triggerHierarchy = new();

#if NMY_ENABLE_UNITY_ATOMS_TAGS
        /// <summary>
        /// A list of atom tags whose colliders will be considered as triggers for this step.
        /// </summary>
        [Tooltip("A list of Atom Tags whose Collider will be considered.")]
        [SerializeField] private List<StringConstant> _triggerTags = new();

        /// <summary>
        /// A list of atom tags whose colliders in their hierarchies will be considered as triggers for this step.
        /// This list is used to determine which game objects should be considered as triggers for this step.
        /// </summary>
        [Tooltip("A list of Atom Tags whose Colliders in their hierarchies will be considered.")]
        [SerializeField] private List<StringConstant> _triggerHierarchyTags = new();
#endif

        /// <summary>
        /// A flag that indicates whether the player is currently touching the object.
        /// </summary>
        private bool _isTouched;

        /// <summary>
        /// The amount of time that the player has spent touching the object.
        /// </summary>
        private float _touchTime;

        /// <summary>
        /// A set of colliders that are currently in the trigger area of the object to touch.
        /// This set is used to track the player's interactions with the object to touch and to determine
        /// when the touch duration has been reached.
        /// </summary>
        /// <remarks>
        /// Keep track of Colliders currently touching, as we want to check the total time ANY of our Colliders touches
        /// </remarks>
        private HashSet<Collider> _touchCount = new();

        /// <summary>
        /// Overrides the <see cref="BaseTrainingStep.Start"/> method of the <see cref="BaseTrainingStep"/> class
        /// to extend the list of triggers with the colliders in the hierarchies of the objects in the
        /// <see cref="_triggerHierarchy"/> field and with the colliders of objects with the specified tags in the
        /// <see cref="_triggerTags"/> and <see cref="_triggerHierarchyTags"/> fields.
        /// </summary>
        protected override void Start()
        {
            base.Start();

            // extend our main Collider List
            foreach (var go in _triggerHierarchy)
            {
                if (go == null) continue;
                _triggers.AddRange(go.GetComponentsInChildren<Collider>());
            }

#if NMY_ENABLE_UNITY_ATOMS_TAGS
            foreach (var constant in _triggerTags)
            {
                if (constant == null) continue;
                var list = AtomTags.FindAllByTag(constant.Value);
                foreach (var go in list)
                {
                    var c = go.GetComponent<Collider>();
                    if (c == null) continue;
                    _triggers.Add(c);
                }
            }

            foreach (var constant in _triggerHierarchyTags)
            {
                if (constant == null) continue;
                var list = AtomTags.FindAllByTag(constant.Value);
                foreach (var go in list)
                {
                    var colliders = go.GetComponentsInChildren<Collider>();
                    if (colliders.Length <= 0) continue;
                    _triggers.AddRange(colliders);
                }
            }
#endif
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
            _touchableTriggerEvent.onTriggerEnter.AddListener(OnTriggerEnter);
            _touchableTriggerEvent.onTriggerStay.AddListener(OnTriggerStay);
            _touchableTriggerEvent.onTriggerExit.AddListener(OnTriggerExit);
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
            _touchableTriggerEvent.onTriggerEnter.RemoveListener(OnTriggerEnter);
            _touchableTriggerEvent.onTriggerStay.RemoveListener(OnTriggerStay);
            _touchableTriggerEvent.onTriggerExit.RemoveListener(OnTriggerExit);
        }

        /// <summary>
        /// The event handler for the <see cref="TriggerEvent.onTriggerEnter"/> event of <see cref="TriggerEvent"/>.
        /// Called when the player enters the trigger area of the object to touch.
        /// </summary>
        /// <param name="other">The Collider of the object that entered the trigger area.</param>
        private void OnTriggerEnter(Collider other)
        {
            if (_triggers.Count == 0 || _triggers.Contains(other))
            {
                if (_touchCount.Count == 0)
                {
                    // set time only if this is the FIRST object that's touching
                    _touchTime = Time.time;
                    if (stepState == StepState.StepStarted && _soundOnTouchEnter)
                        _soundOnTouchEnter.Play();
                }

                _isTouched = true;
                _touchCount.Add(other);
            }
        }

        /// <summary>
        /// The event handler for the <see cref="TriggerEvent.onTriggerEnter"/> event of <see cref="TriggerEvent"/>.
        /// Called when the player stays om the trigger area of the object to touch.
        /// </summary>
        /// <remarks>
        /// We do not get OnTriggerEnter's for object that were already inside during OnEnable, so we also query OnTriggerStay.
        /// </remarks>
        /// <param name="other">The Collider of the object that entered the trigger area.</param>
        private void OnTriggerStay(Collider other)
        {
            if (_triggers.Count == 0 || _triggers.Contains(other))
            {
                if (_touchCount.Count == 0)
                {
                    // set time only if this is the FIRST object that's touching
                    _touchTime = Time.time;
                    if (stepState == StepState.StepStarted && _soundOnTouchEnter)
                        _soundOnTouchEnter.Play();
                }

                _isTouched = true;
                _touchCount.Add(other); // (note: no exception if already contained - will simply return false instead)
            }
        }

        /// <summary>
        /// The event handler for the <see cref="TriggerEvent.onTriggerEnter"/> event of <see cref="TriggerEvent"/>.
        /// Called when the player exits the trigger area of the object to touch.
        /// </summary>
        /// <param name="other">The Collider of the object that entered the trigger area.</param>
        private void OnTriggerExit(Collider other)
        {
            //Debug.Log($"{GetType()}: {other.name}", this);

            if (_triggers.Count == 0 || _triggers.Contains(other))
            {
                _touchCount.Remove(other); // (note: no exception if not found - will simply return false instead)

                if (_touchCount.Count == 0)
                {
                    // ONLY for the last Collider leaving do we want to reset the touch state
                    _isTouched = false;
                    if (stepState == StepState.StepStarted && _soundOnTouchExit)
                        _soundOnTouchExit.Play();
                }
            }
        }

        /// <summary>
        /// Overrides the <see cref="BaseTrainingStep.ClientStepActionAsync"/> method of the <see cref="BaseTrainingStep"/> class to
        /// asynchronously wait for the player to touch the trigger area before signaling that the step is finished for the local client.
        /// </summary>
        /// <remarks>
        /// This method calls <see cref="WaitForTouch"/> to wait for the player to touch the trigger area,
        /// and then calls <see cref="BaseTrainingStep.RaiseClientStepFinished"/> to signal that the step is finished.
        /// If the cancellation token is cancelled while waiting, <see cref="BaseTrainingStep.RaiseClientStepFinished"/> is still called.
        /// </remarks>
        /// <param name="ct">The cancellation token for the async operation.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        protected override async UniTask ClientStepActionAsync(CancellationToken ct)
        {
            try
            {
                await WaitForTouch(ct);
                RaiseClientStepFinished();
            }
            catch (OperationCanceledException)
            {
                RaiseClientStepFinished();
            }
        }

        /// <summary>
        /// Asynchronously waits until the player touches the object for the required duration.
        /// </summary>
        /// <param name="ct">A cancellation token that can be used to cancel the wait operation.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        private async UniTask WaitForTouch(CancellationToken ct)
        {
            await UniTask.WaitUntil(() => _isTouched && _touchDuration <= Time.time - _touchTime,
                                    cancellationToken: ct);

            if (stepState == StepState.StepStarted && _soundOnTouchComplete)
                _soundOnTouchComplete.Play();
        }
        
        /// <summary>
        /// Overrides the method <see cref="BaseTrainingStep.ResetStepState"/> of the <see cref="BaseTrainingStep"/> class to
        /// reset the <see cref="_isTouched"/> and <see cref="_touchCount"/> fields to their initial values.
        /// </summary>
        protected override void ResetStepState()
        {
            base.ResetStepState();
            _touchCount.Clear(); // in case we lost any Exit events
            _isTouched = false;
        }

        protected override string GameObjectPrefixName() => "[Touch Step]";
    }
}
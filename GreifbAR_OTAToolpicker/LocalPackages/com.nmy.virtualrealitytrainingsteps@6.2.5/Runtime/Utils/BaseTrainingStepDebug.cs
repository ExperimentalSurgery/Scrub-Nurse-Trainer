using NMY.VirtualRealityTraining.Steps;
using UnityEngine;
using UnityEngine.Assertions;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace NMY.VirtualRealityTraining
{
    /// <summary>
    /// This class is an extension to the <see cref="BaseTrainingStep"/> class.
    /// It listens to the events raised by each <see cref="BaseTrainingStep"/> and provides
    /// the user with helpful information while implementing a step sequence:
    /// <list type="bullet">
    /// <item> Pings the currently running step when activated. </item>
    /// <item> Debugs each event for the current step. </item>
    /// </list>
    /// </summary>
    [RequireComponent(typeof(BaseTrainingStep))]
    public class BaseTrainingStepDebug : MonoBehaviour
    {
        /// <summary>
        /// A flag that indicates whether the GameObject of this step will be highlighted in the hierarchy
        /// when this step starts.
        /// </summary>
        [SerializeField] private bool _pingObject = true;

        /// <summary>
        /// A flag that indicated whether to log <see cref="BaseTrainingStep.OnStepStarted"/>.
        /// </summary>
        [Header("Events")]
        [SerializeField] private bool _debugStepStarted = true;
        
        /// <summary>
        /// A flag that indicated whether to log <see cref="BaseTrainingStep.OnStepFinished"/>.
        /// </summary>
        [SerializeField] private bool _debugStepFinished  = true;
        
        /// <summary>
        /// A flag that indicated whether to log <see cref="BaseTrainingStep.OnStepCompleted"/>.
        /// </summary>
        [SerializeField] private bool _debugStepCompleted = true;

        /// <summary>
        /// A flag that indicated whether to log <see cref="BaseTrainingStep.OnStepActionStarted"/>.
        /// </summary>
        [SerializeField] private bool _debugStepActionStarted;
        
        /// <summary>
        /// A flag that indicated whether to log <see cref="BaseTrainingStep.OnStepActionFinished"/>.
        /// </summary>
        [SerializeField] private bool _debugStepActionFinished;

        /// <summary>
        /// A flag that indicated whether to log <see cref="BaseTrainingStep.OnActivatablesActivating"/>.
        /// </summary>
        [SerializeField] private bool _debugActivatablesActivating;
        
        /// <summary>
        /// A flag that indicated whether to log <see cref="BaseTrainingStep.OnActivatablesActivated"/>.
        /// </summary>
        [SerializeField] private bool _debugActivatablesActivated;
        
        /// <summary>
        /// A flag that indicated whether to log <see cref="BaseTrainingStep.OnActivatablesDeactivating"/>.
        /// </summary>
        [SerializeField] private bool _debugActivatablesDeactivating;
        
        /// <summary>
        /// A flag that indicated whether to log <see cref="BaseTrainingStep.OnActivatablesDeactivated"/>.
        /// </summary>
        [SerializeField] private bool _debugActivatablesDeactivated;
        
        /// <summary>
        /// A flag that indicated whether to log <see cref="BaseTrainingStep.OnPersistantActivatablesActivating"/>.
        /// </summary>
        [SerializeField] private bool _debugPersistantActivatablesActivating;

        /// <summary>
        /// A flag that indicated whether to log <see cref="BaseTrainingStep.OnPersistantActivatablesActivated"/>.
        /// </summary>
        [SerializeField] private bool _debugPersistantActivatablesActivated;
        
        /// <summary>
        /// A flag that indicated whether to log <see cref="BaseTrainingStep.OnPersistantActivatablesDeactivating"/>.
        /// </summary>
        [SerializeField] private bool _debugPersistantActivatablesDeactivating;
        
        /// <summary>
        /// A flag that indicated whether to log <see cref="BaseTrainingStep.OnPersistantActivatablesDeactivated"/>.
        /// </summary>
        [SerializeField] private bool _debugPersistantActivatablesDeactivated;

        /// <summary>
        /// A reference to the corresponding step.
        /// </summary>
        private BaseTrainingStep _step;

        /// <summary>
        /// Automatically gets the <see cref="BaseTrainingStep"/> attached to this component.
        /// </summary>
        private void Awake()
        {
            _step = GetComponent<BaseTrainingStep>();
            Assert.IsNotNull(_step, $"BaseTrainingStep Component could not be found!");
        }

        /// <summary>
        /// Adds event listener to all step events of <see cref="_step"/>.
        /// </summary>
        private void OnEnable()
        {
            _step.OnStepStarted   += OnStepStarted;
            _step.OnStepFinished  += OnStepFinished;
            _step.OnStepCompleted += OnOnStepCompleted;

            _step.OnStepActionStarted  += OnOnStepActionStarted;
            _step.OnStepActionFinished += OnOnStepActionFinished;

            _step.OnActivatablesActivating   += OnActivatablesActivating;
            _step.OnActivatablesActivated    += OnActivatablesActivated;
            _step.OnActivatablesDeactivating += OnActivatablesDeactivating;
            _step.OnActivatablesDeactivated  += OnActivatablesDeactivated;

            _step.OnPersistantActivatablesActivated    += OnPersistantActivatablesActivated;
            _step.OnPersistantActivatablesActivating   += OnPersistantActivatablesActivating;
            _step.OnPersistantActivatablesDeactivated  += OnPersistantActivatablesDeactivated;
            _step.OnPersistantActivatablesDeactivating += OnPersistantActivatablesDeactivating;
        }

        /// <summary>
        /// Removes all event listener from <see cref="_step"/>.
        /// </summary>
        private void OnDisable()
        {
            _step.OnStepStarted   -= OnStepStarted;
            _step.OnStepFinished  -= OnStepFinished;
            _step.OnStepCompleted -= OnOnStepCompleted;

            _step.OnStepActionStarted  -= OnOnStepActionStarted;
            _step.OnStepActionFinished -= OnOnStepActionFinished;

            _step.OnActivatablesActivating   -= OnActivatablesActivating;
            _step.OnActivatablesActivated    -= OnActivatablesActivated;
            _step.OnActivatablesDeactivating -= OnActivatablesDeactivating;
            _step.OnActivatablesDeactivated  -= OnActivatablesDeactivated;

            _step.OnPersistantActivatablesActivated    -= OnPersistantActivatablesActivated;
            _step.OnPersistantActivatablesActivating   -= OnPersistantActivatablesActivating;
            _step.OnPersistantActivatablesDeactivated  -= OnPersistantActivatablesDeactivated;
            _step.OnPersistantActivatablesDeactivating -= OnPersistantActivatablesDeactivating;
        }

        /// <summary>
        /// The event handler for the <see cref="BaseTrainingStep.OnStepStarted"/> event of <see cref="BaseTrainingStep"/>.
        /// Called when the step started. It also pings the GameObject if <see cref="_pingObject"/> is true.
        /// </summary>
        /// <param name="sender">The sender of the event.</param>
        /// <param name="e">The event arguments.</param>
        private void OnStepStarted(object sender, BaseTrainingStepEventArgs e)
        {
#if UNITY_EDITOR
            if (_pingObject) EditorGUIUtility.PingObject(this);
#endif
            if (!_debugStepStarted) return;
            Debug.Log($"{GetType()}: \"{e.step.name}\" started", this);
        }

        /// <summary>
        /// The event handler for the <see cref="BaseTrainingStep.OnStepFinished"/> event of <see cref="BaseTrainingStep"/>.
        /// Called when the step finished.
        /// </summary>
        /// <param name="sender">The sender of the event.</param>
        /// <param name="e">The event arguments.</param>
        private void OnStepFinished(object sender, BaseTrainingStepEventArgs e)
        {
            if (!_debugStepFinished) return;
            Debug.Log($"{GetType()}: \"{e.step.name}\" finished", this);
        }

        /// <summary>
        /// The event handler for the <see cref="BaseTrainingStep.OnStepCompleted"/> event of <see cref="BaseTrainingStep"/>.
        /// Called when the step completed.
        /// </summary>
        /// <param name="sender">The sender of the event.</param>
        /// <param name="e">The event arguments.</param>
        private void OnOnStepCompleted(object sender, BaseTrainingStepEventArgs e)
        {
            if (!_debugStepCompleted) return;
            Debug.Log($"{GetType()}: \"{e.step.name}\" completed", this);
        }

        /// <summary>
        /// The event handler for the <see cref="BaseTrainingStep.OnStepActionStarted"/> event of <see cref="BaseTrainingStep"/>.
        /// Called when the step action started.
        /// </summary>
        /// <param name="sender">The sender of the event.</param>
        /// <param name="e">The event arguments.</param>
        private void OnOnStepActionStarted(object sender, BaseTrainingStepEventArgs e)
        {
            if (!_debugStepActionStarted) return;
            Debug.Log($"{GetType()}: before the step action of \"{e.step.name}\"", this);
        }

        /// <summary>
        /// The event handler for the <see cref="BaseTrainingStep.OnStepActionFinished"/> event of <see cref="BaseTrainingStep"/>.
        /// Called when the step action finished.
        /// </summary>
        /// <param name="sender">The sender of the event.</param>
        /// <param name="e">The event arguments.</param>
        private void OnOnStepActionFinished(object sender, BaseTrainingStepEventArgs e)
        {
            if (!_debugStepActionFinished) return;
            Debug.Log($"{GetType()}: after the step action of \"{e.step.name}\"", this);
        }
        
        /// <summary>
        /// The event handler for the <see cref="BaseTrainingStep.OnActivatablesActivating"/> event of <see cref="BaseTrainingStep"/>.
        /// Called when the activatables are activating.
        /// </summary>
        /// <param name="sender">The sender of the event.</param>
        /// <param name="e">The event arguments.</param>
        private void OnActivatablesActivating(object sender, BaseTrainingStepEventArgs e)
        {
            if (!_debugActivatablesActivating) return;
            Debug.Log($"{GetType()}: Activatables for \"{e.step.name}\" activating", this);
        }

        /// <summary>
        /// The event handler for the <see cref="BaseTrainingStep.OnActivatablesActivated"/> event of <see cref="BaseTrainingStep"/>.
        /// Called when the activatables are activated.
        /// </summary>
        /// <param name="sender">The sender of the event.</param>
        /// <param name="e">The event arguments.</param>
        private void OnActivatablesActivated(object sender, BaseTrainingStepEventArgs e)
        {
            if (!_debugActivatablesActivated) return;
            Debug.Log($"{GetType()}: Activatables for \"{e.step.name}\" activated", this);
        }
        
        /// <summary>
        /// The event handler for the <see cref="BaseTrainingStep.OnActivatablesDeactivating"/> event of <see cref="BaseTrainingStep"/>.
        /// Called when the activatables are deactivating.
        /// </summary>
        /// <param name="sender">The sender of the event.</param>
        /// <param name="e">The event arguments.</param>
        private void OnActivatablesDeactivating(object sender, BaseTrainingStepEventArgs e)
        {
            if (!_debugActivatablesDeactivating) return;
            Debug.Log($"{GetType()}: Activatables for \"{e.step.name}\" deactivating", this);
        }

        /// <summary>
        /// The event handler for the <see cref="BaseTrainingStep.OnActivatablesDeactivated"/> event of <see cref="BaseTrainingStep"/>.
        /// Called when the activatables are deactivated.
        /// </summary>
        /// <param name="sender">The sender of the event.</param>
        /// <param name="e">The event arguments.</param>
        private void OnActivatablesDeactivated(object sender, BaseTrainingStepEventArgs e)
        {
            if (!_debugActivatablesDeactivated) return;
            Debug.Log($"{GetType()}: Activatables for \"{e.step.name}\" deactivated", this);
        }
        
        /// <summary>
        /// The event handler for the <see cref="BaseTrainingStep.OnPersistantActivatablesActivating"/> event of <see cref="BaseTrainingStep"/>.
        /// Called when the persistant activatables are activated.
        /// </summary>
        /// <param name="sender">The sender of the event.</param>
        /// <param name="e">The event arguments.</param>
        private void OnPersistantActivatablesActivating(object sender, BaseTrainingStepEventArgs e)
        {
            if (!_debugPersistantActivatablesActivating) return;
            Debug.Log($"{GetType()}: Persistant Activatables for \"{e.step.name}\" activating", this);
        }
        
        /// <summary>
        /// The event handler for the <see cref="BaseTrainingStep.OnPersistantActivatablesActivated"/> event of <see cref="BaseTrainingStep"/>.
        /// Called when the persistant activatables are activated.
        /// </summary>
        /// <param name="sender">The sender of the event.</param>
        /// <param name="e">The event arguments.</param>
        private void OnPersistantActivatablesActivated(object sender, BaseTrainingStepEventArgs e)
        {
            if (!_debugPersistantActivatablesActivated) return;
            Debug.Log($"{GetType()}: Persistant Activatables for \"{e.step.name}\" activated", this);
        }
        
        /// <summary>
        /// The event handler for the <see cref="BaseTrainingStep.OnPersistantActivatablesDeactivating"/> event of <see cref="BaseTrainingStep"/>.
        /// Called when the persistant activatables are deactivated.
        /// </summary>
        /// <param name="sender">The sender of the event.</param>
        /// <param name="e">The event arguments.</param>
        private void OnPersistantActivatablesDeactivating(object sender, BaseTrainingStepEventArgs e)
        {
            if (!_debugPersistantActivatablesDeactivating) return;
            Debug.Log($"{GetType()}: Persistant Activatables for \"{e.step.name}\" deactivating", this);
        }

        /// <summary>
        /// The event handler for the <see cref="BaseTrainingStep.OnPersistantActivatablesDeactivated"/> event of <see cref="BaseTrainingStep"/>.
        /// Called when the persistant activatables are deactivated.
        /// </summary>
        /// <param name="sender">The sender of the event.</param>
        /// <param name="e">The event arguments.</param>
        private void OnPersistantActivatablesDeactivated(object sender, BaseTrainingStepEventArgs e)
        {
            if (!_debugPersistantActivatablesDeactivated) return;
            Debug.Log($"{GetType()}: Persistant Activatables for \"{e.step.name}\" deactivated", this);
        }
    }
}
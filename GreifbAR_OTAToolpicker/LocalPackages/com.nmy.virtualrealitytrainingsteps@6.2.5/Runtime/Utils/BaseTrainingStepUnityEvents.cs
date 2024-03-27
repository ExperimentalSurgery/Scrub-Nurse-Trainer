using NMY.VirtualRealityTraining.Steps;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Events;

namespace NMY.VirtualRealityTraining
{
    /// <summary>
    /// This class is an extension to the <see cref="BaseTrainingStep"/> class.
    /// It listens to the events raised by the <see cref="BaseTrainingStep"/> class to
    /// expose the events to the inspector. Use this class if you want to add more
    /// functionality to the step without altering the step.
    /// </summary>
    [RequireComponent(typeof(BaseTrainingStep))]
    public class BaseTrainingStepUnityEvents : MonoBehaviour
    {
        /// <summary>
        /// A unity event that exposes <see cref="BaseTrainingStep.OnStepStarted"/> to the inspector. 
        /// </summary>
        public UnityEvent<BaseTrainingStepEventArgs> stepStartedEvent;
        
        /// <summary>
        /// A unity event that exposes <see cref="BaseTrainingStep.OnStepFinished"/> to the inspector. 
        /// </summary>
        public UnityEvent<BaseTrainingStepEventArgs> stepFinishedEvent;
        
        /// <summary>
        /// A unity event that exposes <see cref="BaseTrainingStep.OnStepCompleted"/> to the inspector. 
        /// </summary>
        public UnityEvent<BaseTrainingStepEventArgs> stepCompletedEvent;

        /// <summary>
        /// A unity event that exposes <see cref="BaseTrainingStep.OnStepActionStarted"/> to the inspector. 
        /// </summary>
        public UnityEvent<BaseTrainingStepEventArgs> stepActionStartedEvent;
        
        /// <summary>
        /// A unity event that exposes <see cref="BaseTrainingStep.OnStepActionFinished"/> to the inspector. 
        /// </summary>
        public UnityEvent<BaseTrainingStepEventArgs> stepActionFinishedEvent;
        
        /// <summary>
        /// A unity event that exposes <see cref="BaseTrainingStep.OnActivatablesActivating"/> to the inspector. 
        /// </summary>
        public UnityEvent<BaseTrainingStepEventArgs> activatablesActivating;

        /// <summary>
        /// A unity event that exposes <see cref="BaseTrainingStep.OnActivatablesActivated"/> to the inspector. 
        /// </summary>
        public UnityEvent<BaseTrainingStepEventArgs> activatablesActivated;
        
        /// <summary>
        /// A unity event that exposes <see cref="BaseTrainingStep.OnActivatablesDeactivating"/> to the inspector. 
        /// </summary>
        public UnityEvent<BaseTrainingStepEventArgs> activatablesDeactivating;
        
        /// <summary>
        /// A unity event that exposes <see cref="BaseTrainingStep.OnActivatablesDeactivated"/> to the inspector. 
        /// </summary>
        public UnityEvent<BaseTrainingStepEventArgs> activatablesDeactivated;
        
        /// <summary>
        /// A unity event that exposes <see cref="BaseTrainingStep.OnPersistantActivatablesActivating"/> to the inspector. 
        /// </summary>
        public UnityEvent<BaseTrainingStepEventArgs> persistantActivatablesActivating;
        
        /// <summary>
        /// A unity event that exposes <see cref="BaseTrainingStep.OnPersistantActivatablesActivated"/> to the inspector. 
        /// </summary>
        public UnityEvent<BaseTrainingStepEventArgs> persistantActivatablesActivated;
        
        /// <summary>
        /// A unity event that exposes <see cref="BaseTrainingStep.OnPersistantActivatablesDeactivating"/> to the inspector. 
        /// </summary>
        public UnityEvent<BaseTrainingStepEventArgs> persistantActivatablesDeactivating;
        
        /// <summary>
        /// A unity event that exposes <see cref="BaseTrainingStep.OnPersistantActivatablesDeactivated"/> to the inspector. 
        /// </summary>
        public UnityEvent<BaseTrainingStepEventArgs> persistantActivatablesDeactivated;

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
        /// Called when the step started.
        /// </summary>
        /// <param name="sender">The sender of the event.</param>
        /// <param name="e">The event arguments.</param>
        private void OnStepStarted(object sender, BaseTrainingStepEventArgs e) =>
            stepStartedEvent?.Invoke(e);

        /// <summary>
        /// The event handler for the <see cref="BaseTrainingStep.OnStepFinished"/> event of <see cref="BaseTrainingStep"/>.
        /// Called when the step finished.
        /// </summary>
        /// <param name="sender">The sender of the event.</param>
        /// <param name="e">The event arguments.</param>
        private void OnStepFinished(object sender, BaseTrainingStepEventArgs e) =>
            stepFinishedEvent?.Invoke(e);

        /// <summary>
        /// The event handler for the <see cref="BaseTrainingStep.OnStepCompleted"/> event of <see cref="BaseTrainingStep"/>.
        /// Called when the step completed.
        /// </summary>
        /// <param name="sender">The sender of the event.</param>
        /// <param name="e">The event arguments.</param>
        private void OnOnStepCompleted(object sender, BaseTrainingStepEventArgs e) =>
            stepCompletedEvent?.Invoke(e);

        /// <summary>
        /// The event handler for the <see cref="BaseTrainingStep.OnStepActionStarted"/> event of <see cref="BaseTrainingStep"/>.
        /// Called when the step action started.
        /// </summary>
        /// <param name="sender">The sender of the event.</param>
        /// <param name="e">The event arguments.</param>
        private void OnOnStepActionStarted(object sender, BaseTrainingStepEventArgs e) =>
            stepActionStartedEvent?.Invoke(e);

        /// <summary>
        /// The event handler for the <see cref="BaseTrainingStep.OnStepActionFinished"/> event of <see cref="BaseTrainingStep"/>.
        /// Called when the step action finished.
        /// </summary>
        /// <param name="sender">The sender of the event.</param>
        /// <param name="e">The event arguments.</param>
        private void OnOnStepActionFinished(object sender, BaseTrainingStepEventArgs e) =>
            stepActionFinishedEvent?.Invoke(e);

        /// <summary>
        /// The event handler for the <see cref="BaseTrainingStep.OnActivatablesActivating"/> event of <see cref="BaseTrainingStep"/>.
        /// Called before the activatables are activating.
        /// </summary>
        /// <param name="sender">The sender of the event.</param>
        /// <param name="e">The event arguments.</param>
        private void OnActivatablesActivating(object sender, BaseTrainingStepEventArgs e) =>
            activatablesActivating?.Invoke(e);
        
        /// <summary>
        /// The event handler for the <see cref="BaseTrainingStep.OnActivatablesActivated"/> event of <see cref="BaseTrainingStep"/>.
        /// Called after the activatables are activated.
        /// </summary>
        /// <param name="sender">The sender of the event.</param>
        /// <param name="e">The event arguments.</param>
        private void OnActivatablesActivated(object sender, BaseTrainingStepEventArgs e) =>
            activatablesActivated?.Invoke(e);
        
        /// <summary>
        /// The event handler for the <see cref="BaseTrainingStep.OnActivatablesDeactivating"/> event of <see cref="BaseTrainingStep"/>.
        /// Called before the activatables are deactivating.
        /// </summary>
        /// <param name="sender">The sender of the event.</param>
        /// <param name="e">The event arguments.</param>
        private void OnActivatablesDeactivating(object sender, BaseTrainingStepEventArgs e) =>
            activatablesDeactivating?.Invoke(e);

        /// <summary>
        /// The event handler for the <see cref="BaseTrainingStep.OnActivatablesDeactivated"/> event of <see cref="BaseTrainingStep"/>.
        /// Called after the activatables are deactivated.
        /// </summary>
        /// <param name="sender">The sender of the event.</param>
        /// <param name="e">The event arguments.</param>
        private void OnActivatablesDeactivated(object sender, BaseTrainingStepEventArgs e) =>
            activatablesDeactivated?.Invoke(e);
        
        /// <summary>
        /// The event handler for the <see cref="BaseTrainingStep.OnPersistantActivatablesActivating"/> event of <see cref="BaseTrainingStep"/>.
        /// Called before the persistant activatables are activating.
        /// </summary>
        /// <param name="sender">The sender of the event.</param>
        /// <param name="e">The event arguments.</param>
        private void OnPersistantActivatablesActivating(object sender, BaseTrainingStepEventArgs e) =>
            persistantActivatablesActivating?.Invoke(e);
        
        /// <summary>
        /// The event handler for the <see cref="BaseTrainingStep.OnPersistantActivatablesActivated"/> event of <see cref="BaseTrainingStep"/>.
        /// Called after the persistant activatables are activated.
        /// </summary>
        /// <param name="sender">The sender of the event.</param>
        /// <param name="e">The event arguments.</param>
        private void OnPersistantActivatablesActivated(object sender, BaseTrainingStepEventArgs e) =>
            persistantActivatablesActivated?.Invoke(e);

        /// <summary>
        /// The event handler for the <see cref="BaseTrainingStep.OnPersistantActivatablesDeactivating"/> event of <see cref="BaseTrainingStep"/>.
        /// Called before the persistant activatables are deactivating.
        /// </summary>
        /// <param name="sender">The sender of the event.</param>
        /// <param name="e">The event arguments.</param>
        private void OnPersistantActivatablesDeactivating(object sender, BaseTrainingStepEventArgs e) =>
            persistantActivatablesDeactivating?.Invoke(e);
        
        /// <summary>
        /// The event handler for the <see cref="BaseTrainingStep.OnPersistantActivatablesDeactivated"/> event of <see cref="BaseTrainingStep"/>.
        /// Called when the persistant activatables are deactivated.
        /// </summary>
        /// <param name="sender">The sender of the event.</param>
        /// <param name="e">The event arguments.</param>
        private void OnPersistantActivatablesDeactivated(object sender, BaseTrainingStepEventArgs e) =>
            persistantActivatablesDeactivated?.Invoke(e);
    }
}
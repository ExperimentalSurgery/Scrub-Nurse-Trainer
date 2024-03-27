using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Assertions;

namespace NMY.VirtualRealityTraining.Steps.VirtualAssistant
{
    /// <summary>
    /// A class for providing hints for a <see cref="BaseTrainingStep"/> after a certain amount of time.
    /// </summary>
    /// <remarks>
    /// The <see cref="_timeBeforeHint"/> field is used to specify the amount of time to wait before giving hints.
    /// </remarks>
    [RequireComponent(typeof(BaseTrainingStep))]
    public class VirtualAssistantStepHints : MonoBehaviour
    {
        /// <summary>
        /// The time in seconds to wait before providing hints.
        /// </summary>
        [SerializeField] private float _timeBeforeHint;

        /// <summary>
        /// The <see cref="BaseTrainingStep"/> object to provide hints for.
        /// </summary>
        private BaseTrainingStep _step;

        /// <summary>
        /// A <see cref="CancellationTokenSource"/> object that allows cancelling the timer that provides hints.
        /// </summary>
        private CancellationTokenSource _cancellationTokenSource;

        /// <summary>
        /// This method is called when the object is initialized. It gets the <see cref="BaseTrainingStep"/> object that this
        /// <see cref="VirtualAssistantStepHints"/> object is attached to, and makes sure it exists.
        /// </summary>
        private void Awake()
        {
            _step = GetComponent<BaseTrainingStep>();
            Assert.IsNotNull(_step, $"BaseTrainingStep Component could not be found!");
        }

        /// <summary>
        /// This method is called when the object is enabled. It subscribes to the `BaseTrainingStep` events.
        /// </summary>
        private void OnEnable()
        {
            _step.OnStepStarted  += OnStepStarted;
            _step.OnStepFinished += OnStepFinished;
        }

        /// <summary>
        /// This method is called when the object is disabled. It unsubscribes from the `BaseTrainingStep` events.
        /// </summary>
        private void OnDisable()
        {
            _step.OnStepStarted  -= OnStepStarted;
            _step.OnStepFinished -= OnStepFinished;
        }

        /// <summary>
        /// This method is called when the `BaseTrainingStep` object starts. It creates a new <see cref="CancellationTokenSource"/>
        /// object and starts the timer to provide hints.
        /// </summary>
        /// <param name="sender">The object that raised the event.</param>
        /// <param name="e">The event arguments.</param>
        private async void OnStepStarted(object sender, BaseTrainingStepEventArgs e)
        {
            _cancellationTokenSource = new CancellationTokenSource();
            await GiveHints(_cancellationTokenSource.Token);
        }

        /// <summary>
        /// This method provides hints to the user after a certain amount of time has passed.
        /// If the timer is cancelled, this method does nothing.
        /// </summary>
        /// <param name="ct">The cancellation token to use for cancelling the timer.</param>
        private async UniTask GiveHints(CancellationToken ct)
        {
            try
            {
                await UniTask.Delay(TimeSpan.FromSeconds(_timeBeforeHint), cancellationToken: ct);
                Debug.Log($"{GetType()}: TODO: SOME HINTS from the VA", this);
            }
            catch (OperationCanceledException)
            {
    
            }
        }

        /// <summary>
        /// This method is called when the `BaseTrainingStep` object finishes. It cancels the timer that provides hints.
        /// </summary>
        /// <param name="sender">The object that raised the event.</param>
        /// <param name="e">The event arguments.</param>
        private void OnStepFinished(object sender, BaseTrainingStepEventArgs e)
        {
            _cancellationTokenSource?.Cancel();
        }
    }
}
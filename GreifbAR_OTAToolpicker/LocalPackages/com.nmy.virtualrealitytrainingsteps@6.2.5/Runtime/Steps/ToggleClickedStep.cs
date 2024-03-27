using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

namespace NMY.VirtualRealityTraining.Steps
{
    /// <summary>
    /// A training step that allows users to click a toggle to finish the step.
    /// </summary>
    /// <remarks>
    /// This step is useful for providing a more interactive experience for users.
    /// When the toggle is clicked, it will trigger the OnToggleClicked event.
    /// If the 'setToggleInteractableAtStepStart' flag is set to true,
    /// the toggle will be set to interactable when the step starts.
    /// If the 'setToggleNotInteractableAtStepFinished' flag is set to true,
    /// the toggle will be set to not interactable when the step finishes.
    /// The step also has a multiplayer wait counter that displays the number of finished clients and total clients in the step.
    /// This can be configured by setting the 'text' field.
    /// </remarks>
    public class ToggleClickedStep : BaseTrainingStep
    {
        /// <summary>
        /// The <see cref="Toggle"/> that will be clicked by the user to complete the step.
        /// </summary>
        [Header("Toggle Setup")]
        [SerializeField] private Toggle _toggle;

        
        /// <summary>
        /// Specifies whether the <see cref="_toggle"/> should be set to interactable when the step is started.
        /// </summary>
        [SerializeField] private bool _setToggleInteractableAtStepStart;
        
        /// <summary>
        /// Specifies whether the <see cref="_toggle"/> should be set to not interactable when the step is finished.
        /// </summary>
        [SerializeField] private bool _setToggleNotInteractableAtStepFinished;
        
        /// <summary>
        /// The text UI element that displays the number of clients that have finished the step.
        /// </summary>
        /// <remarks>
        /// If this value is set, it will be enabled and the text will be updated based on the finishedClients and totalClients values
        /// when the step starts. The text will be disabled and the listener for value changes will be removed when the step finishes.
        /// </remarks>
        [Header("Multiplayer Wait Counter")]
        [SerializeField] private TMP_Text _text;

        /// <summary>
        /// A boolean value that is used to track if the <see cref="_toggle"/> has been clicked or not.
        /// </summary>
        private bool _toggleClicked;
        
#region Step Action

        /// <summary>
        /// Overrides the <see cref="BaseTrainingStep.PreStepActionAsync"/> method of the <see cref="BaseTrainingStep"/> class to
        /// register an event handler for the <see cref="Toggle.onValueChanged"/> event of the <see cref="Toggle"/> class,
        /// which is used to detect when the user is clicking <see cref="_toggle"/>.
        /// </summary>
        /// <remarks>
        /// It sets the <see cref="Toggle.interactable"/> flag of <see cref="_toggle"/> to true if
        /// <see cref="_setToggleInteractableAtStepStart"/> is also true.
        /// It deactivates the multiplayer wait counter text <see cref="_text"/>.
        /// </remarks>
        /// <param name="ct">The cancellation token for the async operation.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        /// <seealso cref="OnToggleClicked"/>
        protected override async UniTask PreStepActionAsync(CancellationToken ct)
        {
            await base.PreStepActionAsync(ct);
            _toggle.onValueChanged.AddListener(OnToggleClicked);
            
            if (_setToggleInteractableAtStepStart)
                _toggle.interactable = true;

            if (_text != null) _text.enabled = false;
        }
        
        /// <summary>
        /// <para>
        /// Overrides the <see cref="BaseTrainingStep.PreStepActionAsync"/> method of the <see cref="BaseTrainingStep"/> class to
        /// unregister the event handler for the <see cref="Toggle.onValueChanged"/> event of the <see cref="Toggle"/> class that
        /// were registered in the <see cref="PreStepActionAsync"/> method.
        /// </para>
        /// <para>
        /// Registers the event handlers for <see cref="NetworkVariable{T}.OnValueChanged"/> events for the <see cref="NetworkVariable{T}"/>
        /// <see cref="BaseTrainingStep.finishedClients"/> and <see cref="BaseTrainingStep.totalClients"/>
        /// to update the wait counter text. They will be unregistered in the <see cref="OnStepFinishedEvent"/> method.
        /// </para>
        /// </summary>
        /// <param name="ct">The cancellation token for the async operation.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        protected override async UniTask PostStepActionAsync(CancellationToken ct)
        {
            await base.PostStepActionAsync(ct);
            _toggle.onValueChanged.RemoveListener(OnToggleClicked);
            
            if (_setToggleNotInteractableAtStepFinished)
                _toggle.interactable = false;
            
            finishedClients.OnValueChanged += OnClientsUpdated;
            totalClients.OnValueChanged    += OnClientsUpdated;

            OnStepFinished += OnStepFinishedEvent;
            
            UpdateText();
        }
        
        /// <summary>
        /// The event handler for <see cref="BaseTrainingStep.OnStepFinished"/> event of <see cref="BaseTrainingStep"/>.
        /// Unregisters the event handlers that were registered in the <see cref="PostStepActionAsync"/> method.
        /// </summary>
        /// <param name="sender">The sender of the event.</param>
        /// <param name="e">The event arguments.</param>
        private void OnStepFinishedEvent(object sender, BaseTrainingStepEventArgs e)
        {
            finishedClients.OnValueChanged -= OnClientsUpdated;
            totalClients.OnValueChanged    -= OnClientsUpdated;

            OnStepFinished -= OnStepFinishedEvent;
        }
        
        /// <summary>
        /// Overrides the <see cref="BaseTrainingStep.ClientStepActionAsync"/> method of the <see cref="BaseTrainingStep"/> class to
        /// asynchronously wait until the toggle is clicked before signaling that the step is finished for the local client.
        /// </summary>
        /// <remarks>
        /// This method calls <see cref="WaitUntilToggleClicked"/> to wait for the toggle to be clicked,
        /// and then calls <see cref="BaseTrainingStep.RaiseClientStepFinished"/> to signal that the step is finished.
        /// If the cancellation token is cancelled while waiting, <see cref="BaseTrainingStep.RaiseClientStepFinished"/> is still called.
        /// </remarks>
        /// <param name="ct">The cancellation token for the async operation.</param>
        /// <returns>A task representing the asynchronous operation.</returns>

        protected override async UniTask ClientStepActionAsync(CancellationToken ct)
        {
            try
            {
                if (_toggle != null && !_toggle.isOn)
                    await WaitUntilToggleClicked(ct);
                RaiseClientStepFinished();
            }
            catch (OperationCanceledException)
            {
                RaiseClientStepFinished();
            }
        }

        /// <summary>
        /// The event handler for the <see cref="Toggle.onValueChanged"/> event of <see cref="Toggle"/>.
        /// This method is called when the toggle's value changes.
        /// </summary>
        /// <param name="value">The new value of the toggle.</param>
        private void OnToggleClicked(bool value)
        {
            _toggleClicked = true;
        }

        /// <summary>
        /// Asynchronously waits until the toggle has been clicked, or until the cancellation token is cancelled.
        /// </summary>
        /// <param name="ct">The cancellation token for the async operation.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        private UniTask WaitUntilToggleClicked(CancellationToken ct) => 
            UniTask.WaitUntil(() => _toggleClicked, cancellationToken: ct);

#endregion

        /// <summary>
        /// Overrides the method <see cref="BaseTrainingStep.ResetStepState"/> of the <see cref="BaseTrainingStep"/> class to
        /// reset the <see cref="_toggleClicked"/> to false.
        /// </summary>
        protected override void ResetStepState()
        {
            base.ResetStepState();
            _toggleClicked = false;
        }

        /// <summary>
        /// The event handler for the <see cref="NetworkVariable{T}.OnValueChanged"/> event of
        /// <see cref="BaseTrainingStep.finishedClients"/> and <see cref="BaseTrainingStep.totalClients"/>.
        /// Updates the text displayed on the multiplayer wait counter.
        /// This method is called when the number of finished clients changes.
        /// </summary>
        /// <param name="prev">The previous value of the number of finished clients.</param>
        /// <param name="curr">The current value of the number of finished clients.</param>
        private void OnClientsUpdated(int prev, int curr) => UpdateText();


        /// <summary>
        /// Updates the text displayed in the multiplayer wait counter.
        /// </summary>
        private void UpdateText()
        {
            if (_text == null) return;
            _text.enabled = _waitForAllClientsToFinish;
            _text.text    = $"{finishedClients.Value}/{totalClients.Value}";
        }
        
        protected override string GameObjectPrefixName() => "[Toggle Step]";
    }
}
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
    /// A training step where the user must click a button to proceed to the next step.
    /// </summary>
    /// <remarks>
    /// If <see cref="BaseTrainingStep._waitForAllClientsToFinish"/> is true, this step shows the number of finished
    /// clients in the provided <see cref="_text"/> component.
    /// </remarks>
    public class ButtonClickedStep : BaseTrainingStep
    {
        /// <summary>
        /// A reference to the button that must be clicked.
        /// </summary>
        [Header("Button Setup")]
        [SerializeField] private Button   _button;
        
        /// <summary>
        /// Whether to set the interactable state of the button at the start of the step.
        /// </summary>
        [SerializeField] private bool _setInteractableAtStepStart;
        
        /// <summary>
        /// Whether to set the button to not be interactable at the end of the step.
        /// </summary>
        [SerializeField] private bool _setNotInteractableAtStepFinished;
        
        [Header("Multiplayer Wait Counter")]
        [SerializeField] private TMP_Text _text;

        /// <summary>
        /// A boolean value indicating whether a button has been clicked during this step.
        /// </summary>
        private bool _buttonClicked;

#region Step Action

        /// <summary>
        /// Overrides the method <see cref="BaseTrainingStep.ResetStepState"/> of the <see cref="BaseTrainingStep"/> class to
        /// reset the <see cref="_buttonClicked"/> to false.
        /// </summary>
        protected override void ResetStepState()
        {
            base.ResetStepState();
            _buttonClicked = false;
        }

        /// <summary>
        /// Overrides the <see cref="BaseTrainingStep.PreStepActionAsync"/> method of the <see cref="BaseTrainingStep"/> class to
        /// register an event handler for the <see cref="Button.onClick"/> event of the <see cref="Button"/> class,
        /// which is used to detect when the user is clicking <see cref="_button"/>.
        /// </summary>
        /// <remarks>
        /// It sets the <see cref="Toggle.interactable"/> flag of <see cref="_button"/> to true if <see cref="_setInteractableAtStepStart"/>
        /// is also true. It deactivates the multiplayer wait counter text <see cref="_text"/>.
        /// </remarks>
        /// <param name="ct">The cancellation token for the async operation.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        /// <seealso cref="OnButtonClicked"/>
        protected override async UniTask PreStepActionAsync(CancellationToken ct)
        {
            await base.PreStepActionAsync(ct);
            _button.onClick.AddListener(OnButtonClicked);
            
            if (_setInteractableAtStepStart)
                _button.interactable = true;

            if (_text != null) _text.enabled = false;
        }
        
        /// <summary>
        /// <para>
        /// Overrides the <see cref="BaseTrainingStep.PreStepActionAsync"/> method of the <see cref="BaseTrainingStep"/> class to
        /// unregister the event handler for the <see cref="Button.onClick"/> event of the <see cref="Button"/> class that
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
            _button.onClick.RemoveListener(OnButtonClicked);
            
            if (_setNotInteractableAtStepFinished)
                _button.interactable = false;
            
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
        /// asynchronously wait until the button is clicked before signaling that the step is finished for the local client.
        /// </summary>
        /// <remarks>
        /// This method calls <see cref="WaitUntilButtonClicked"/> to wait for the button to be clicked,
        /// and then calls <see cref="BaseTrainingStep.RaiseClientStepFinished"/> to signal that the step is finished.
        /// If the cancellation token is cancelled while waiting, <see cref="BaseTrainingStep.RaiseClientStepFinished"/> is still called.
        /// </remarks>
        /// <param name="ct">The cancellation token for the async operation.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        protected override async UniTask ClientStepActionAsync(CancellationToken ct)
        {
            try
            {
                await WaitUntilButtonClicked(ct);
                RaiseClientStepFinished();
            }
            catch (OperationCanceledException)
            {
                RaiseClientStepFinished();
            }
        }

        /// <summary>
        /// The event handler for the <see cref="Button.onClick"/> event of the <see cref="Button"/> class.
        /// </summary>
        private void OnButtonClicked()
        {
            _buttonClicked = true;
        }

        /// <summary>
        /// Asynchronously waits until the user clicks the button, or until the cancellation token is cancelled.
        /// </summary>
        /// <param name="ct">A cancellation token that can be used to cancel the asynchronous operation.</param>
        /// <returns>A task that completes when the button is clicked or the cancellation token is cancelled.</returns>
        private UniTask WaitUntilButtonClicked(CancellationToken ct) => 
            UniTask.WaitUntil(() => _buttonClicked, cancellationToken: ct);

#endregion

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
        /// Updates the text when the set of clients that have completed this step is updates.
        /// </summary>
        private void UpdateText()
        {
            if (_text == null) return;
            _text.enabled = _waitForAllClientsToFinish;
            _text.text    = $"{finishedClients.Value}/{totalClients.Value}";
        }
        
        protected override string GameObjectPrefixName() => "[Button Step]";
    }
}
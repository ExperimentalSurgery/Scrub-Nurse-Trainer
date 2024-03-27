using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using Unity.Netcode;
using UnityEngine;

namespace NMY.VirtualRealityTraining.Steps
{
    /// <summary>
    /// An abstract class that adds functionality for handling decision-making in a training sequence.
    /// This class allows for defining a list of possible steps that can be taken based on certain conditions
    /// or user input, and provides methods for waiting until a step has been selected and added to the next
    /// steps in the sequence.
    /// </summary>
    /// <remarks>
    /// It overrides the <see cref="BaseTrainingStep.PreStepActionAsync"/>, <see cref="BaseTrainingStep.PostStepActionAsync"/>,
    /// <see cref="BaseTrainingStep.ClientStepActionAsync"/>, and <see cref="BaseTrainingStep.ServerStepActionAsync"/> methods
    /// of the <see cref="BaseTrainingStep"/> class to add logic for waiting until a step has been selected and added to
    /// the next steps in the sequence. It also has an `abstract` <see cref="SetSelectedStep"/> method that must be implemented
    /// in subclasses to set the selected step.
    /// <br/>
    /// This class is generic and the type parameter `T` represents the type of the decision being made by
    /// the user or determined by the conditions of the training sequence.
    /// </remarks>
    /// <typeparam name="T">The type of the decision being made by the user or determined by the conditions
    /// of the training sequence.</typeparam>
    public abstract class DecisionStep<T> : BaseTrainingStep
    {
        /// <summary>
        /// A list of tuples that contains the possible steps that can be taken based on certain conditions or user input,
        /// and the decision that corresponds to each step.
        /// </summary>
        [SerializeField] protected List<StepDecisionTuple<T>> _stepTuples;

        /// <summary>
        /// A class that represents a tuple of a decision and a step in a training sequence.
        /// </summary>
        /// <typeparam name="TT">The type of the decision.</typeparam>
        [Serializable]
        protected class StepDecisionTuple<TT>
        {
            /// <summary>
            /// The decision.
            /// </summary>
            public TT comparable;

            /// <summary>
            /// The step that corresponds to the decision.
            /// </summary>
            public BaseTrainingStep step;
        }

        /// <summary>
        /// A network variable that represents the selected step in the training sequence.
        /// This allows the selected step to be synchronized across all clients in a multiplayer training sequence.
        /// It is used to track the currently selected step, and to add that step to the next steps in the
        /// training sequence when it is selected.
        /// </summary>
        protected readonly NetworkVariable<NetworkBehaviourReference> _selectedStep = new();

        /// <summary>
        /// A flag that indicates whether the selected step has been added to the next steps in the training sequence.
        /// </summary>
        private bool _addedSelectedStepToNextSteps;

#region Properties

        /// <summary>
        /// Gets the currently selected step in the training sequence from <see cref="_selectedStep"/>.
        /// </summary>
        public BaseTrainingStep selectedStep => _selectedStep.Value.TryGet(out BaseTrainingStep step) ? step : null;

        /// <summary>
        /// Gets the step in the training sequence that is not the currently selected step if <see cref="_stepTuples"/>
        /// has two elements. Otherwise this is <c>null</c>.
        /// </summary>
        public BaseTrainingStep otherStep
        {
            get
            {
                if (selectedStep == null) return null;
                if (_stepTuples.Count == 2)
                {
                    return selectedStep == _stepTuples[0].step ? _stepTuples[1].step : _stepTuples[0].step;
                }

                return null;
            }
        }

#endregion
        
        // TODO: Remove this when NGO 1.7.1 is released; just a workaround for a bug in 1.7.0
        // https://github.com/Unity-Technologies/com.unity.netcode.gameobjects/issues/2755
        [ServerRpc] private void Workaround_ServerRpc() {}
        
        /// <summary>
        /// Overrides the <see cref="BaseTrainingStep.PreStepActionAsync"/> method of the <see cref="BaseTrainingStep"/> class to
        /// register the event handler for the <see cref="NetworkVariable{T}.OnValueChanged"/> event of the <see cref="NetworkVariable{T}"/> class,
        /// which is used to detect when the new step was selected.
        /// </summary>
        /// <param name="ct">The cancellation token for the async operation.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        /// <seealso cref="OnSelectedStepChanged"/>
        protected override UniTask PreStepActionAsync(CancellationToken ct)
        {
            _selectedStep.OnValueChanged += OnSelectedStepChanged;
            return base.PreStepActionAsync(ct);
        }

        /// <summary>
        /// Overrides the <see cref="BaseTrainingStep.PostStepActionAsync"/> method of the <see cref="BaseTrainingStep"/> class to
        /// unregister the event handler for the the <see cref="NetworkVariable{T}.OnValueChanged"/> event of the
        /// <see cref="NetworkVariable{T}"/> class that were registered in the <see cref="PreStepActionAsync"/> method.
        /// </summary>
        /// <param name="ct">The cancellation token for the async operation.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        protected override UniTask PostStepActionAsync(CancellationToken ct)
        {
            _selectedStep.OnValueChanged -= OnSelectedStepChanged;
            return base.PostStepActionAsync(ct);
        }

        /// <summary>
        /// The event handler for the <see cref="NetworkVariable{T}.OnValueChanged"/> event of the <see cref="_selectedStep"/> network variable.
        /// </summary>
        /// <param name="previousValue">The previous value of the <see cref="_selectedStep"/> network variable.</param>
        /// <param name="newValue">The new value of the <see cref="_selectedStep"/> network variable.</param>
        private void OnSelectedStepChanged(NetworkBehaviourReference previousValue, NetworkBehaviourReference newValue)
        {
            if (stepState is not StepState.StepStarted) return;

            if (newValue.TryGet(out BaseTrainingStep step))
            {
                // Add the step only if it does not exist already in next steps 
                if (!IsStepInNextSteps(step)) AddNextStep(step);
                _addedSelectedStepToNextSteps = true;
            }
        }
        
        /// <summary>
        /// Overrides the <see cref="BaseTrainingStep.ClientStepActionAsync"/> method of the <see cref="BaseTrainingStep"/> class to
        /// wait until the selected step has been selected or added to the next steps in the training
        /// sequence.
        /// </summary>
        /// <remarks>
        /// This method calls <see cref="WaitUntilStepSelected"/> and <see cref="WaitUntilStepAddedToNextSteps"/>
        /// to wait until the selected step has been selected or added to the next steps in the training
        /// sequence, and then calls <see cref="BaseTrainingStep.RaiseClientStepFinished"/> to signal that the step is
        /// finished for the local client.
        /// If the cancellation token is cancelled while waiting, <see cref="BaseTrainingStep.RaiseClientStepFinished"/> is still called.
        /// </remarks>
        /// <param name="ct">The cancellation token to use for cancelling the operation.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        protected override async UniTask ClientStepActionAsync(CancellationToken ct)
        {
            try
            {
                await UniTask.WhenAny(WaitUntilStepSelected(ct), WaitUntilStepAddedToNextSteps(ct));
                await WaitUntilStepAddedToNextSteps(ct);

                RaiseClientStepFinished();
            }
            catch (OperationCanceledException)
            {
                if (selectedStep != null && Application.isPlaying) AddNextStep(selectedStep);
                RaiseClientStepFinished();
            }
        }

        /// <summary>
        /// Overrides the <see cref="BaseTrainingStep.ServerStepActionAsync"/> method of the <see cref="BaseTrainingStep"/> class to
        /// asynchronously wait until the selected step has been added to the next steps in the training sequence
        /// on the server.
        /// </summary>
        /// <returns>A task that represents the asynchronous operation.</returns>
        protected override async UniTask ServerStepActionAsync(CancellationToken ct)
        {
            try
            {
                await WaitUntilStepAddedToNextSteps(ct);
            }
            catch (OperationCanceledException)
            {
                if (selectedStep != null) AddNextStep(selectedStep);
            }
        }

        /// <summary>
        /// Asynchronously waits until the selected step in the training sequence has been set.
        /// </summary>
        /// <param name="ct">The cancellation token to use for cancelling the wait operation.</param>
        /// <returns>A task that represents the asynchronous wait operation.</returns>
        private UniTask WaitUntilStepSelected(CancellationToken ct) =>
            UniTask.WaitUntil(() => selectedStep != null, cancellationToken: ct);

        /// <summary>
        /// Asynchronously waits until the selected step has been added to the next steps in the training sequence.
        /// </summary>
        /// <param name="ct">The cancellation token to use for cancelling the wait operation.</param>
        /// <returns>A task that represents the asynchronous wait operation.</returns>
        protected UniTask WaitUntilStepAddedToNextSteps(CancellationToken ct) =>
            UniTask.WaitUntil(() => _addedSelectedStepToNextSteps, cancellationToken: ct);

        /// <summary>
        /// Sets the selected step in the training sequence based on the specified decision.
        /// </summary>
        /// <remarks>
        /// Netcode does not support RPCs in generics, so we must make this abstract, and clone the ServerRpc in all derived classes.
        /// Make sure to correctly implement this in every derived class, by copying the SetSelectedStep and SetSelectedStep_ServerRpc from
        /// <see cref="ButtonDecisionStep"/>, and replacing the "Button" with your actual type parameter.
        /// See this issue for further details: https://github.com/Unity-Technologies/com.unity.netcode.gameobjects/issues/2193#issuecomment-1253173010
        /// </remarks>
        /// <param name="decisionTuple">The decision tuple that determines which step to select.</param>
        protected abstract void SetSelectedStep(StepDecisionTuple<T> decisionTuple);

        
        /// <summary>
        /// Overrides the <see cref="BaseTrainingStep.TryMoveToStep"/> method of the <see cref="BaseTrainingStep"/> class to
        /// add the step from <see cref="_stepTuples"/> to <see cref="BaseTrainingStep._nextSteps"/>
        /// where the <see cref="startTrainingStep"/> was found within the step sequence of the step.
        /// </summary>
        /// <param name="startTrainingStep">Instance of <see cref="BaseTrainingStep"/> that should be started.</param>
        /// <param name="stepToken">Instance of <see cref="CancellationToken"/> to cancel this UniTask externally if necessary.</param>
        /// <returns>Returns true iff <see cref="startTrainingStep"/> was found and started; false otherwise.</returns>
        public override async UniTask<bool> TryMoveToStep(BaseTrainingStep startTrainingStep,
                                                          CancellationToken stepToken)
        {
            void AddIff(BaseTrainingStep s)
            {
                if (s.IsStepInHierarchy(startTrainingStep)) AddNextStep(s);
            }

            for (var index = 0; index < _stepTuples.Count; index++)
            {
                var stepTuple = _stepTuples[index];
                AddIff(stepTuple.step);
            }

            return await base.TryMoveToStep(startTrainingStep, stepToken);
        }

        /// <summary>
        /// Overrides the <see cref="BaseTrainingStep.IsStepInHierarchy"/> method of the <see cref="BaseTrainingStep"/> class to
        /// check if the given step is in the hierarchy of steps that are related to this DecisionStep instance.
        /// </summary>
        /// <remarks>
        /// It does this by checking if the step is in the list of <see cref="_stepTuples"/> or if it is in the hierarchy of steps
        /// for this DecisionStep instance. The method returns true if the step is found in either of these checks,
        /// and false otherwise.
        /// This method is typically used to determine if a given step is a descendant of this DecisionStep instance
        /// in the hierarchy of steps in a training sequence.
        /// </remarks>
        /// <param name="step">Instance of <see cref="BaseTrainingStep"/> to check if it is part of this hierarchy.</param>
        /// <param name="visited">Instance of <see cref="HashSet{T}"/> to keep track of visited steps.</param>
        /// <returns>Returns true iff <paramref name="step"/> was found in the hierarchy; false otherwise.</returns>
        public override bool IsStepInHierarchy(BaseTrainingStep step, HashSet<BaseTrainingStep> visited = default)
        {
            // Initialize the visited HashSet if it's null
            visited ??= new HashSet<BaseTrainingStep>();
    
            // If the step is already visited, it means we are in a cycle
            if (visited.Contains(step)) return false;
    
            // Base case: if this step is the one we're looking for, return true
            if (this == step) return true;
    
            // Add the current step to the visited set to prevent revisiting
            visited.Add(this);
    
            // Check if the step is in any of the tuples
            foreach (var t in _stepTuples)
            {
                // If step is found in tuples, it's part of the hierarchy
                if (t.step == step) return true;
        
                // Recursively check the hierarchy without revisiting steps
                if (!visited.Contains(t.step) && t.step.IsStepInHierarchy(step, visited)) return true;
            }

            // Check the base class hierarchy only if it's not already checked in the tuples
            if (!visited.Contains(this) && base.IsStepInHierarchy(step, visited)) return true;

            // If not found, return false
            return false;
        }

        /// <summary>
        /// Overrides the method <see cref="BaseTrainingStep.ResetStepState"/> of the <see cref="BaseTrainingStep"/> class to
        /// remove <see cref="selectedStep"/> from <see cref="BaseTrainingStep._nextSteps"/> and to reset all steps from
        /// <see cref="_stepTuples"/>.
        /// </summary>
        protected override async void ResetStepState()
        {
            base.ResetStepState();
            RemoveNextStep(selectedStep);

            if (IsServer)
            {
                _selectedStep.Value = new NetworkBehaviourReference();
            }

            _addedSelectedStepToNextSteps = false;

            for (var index = 0; index < _stepTuples.Count; index++)
            {
                var stepTuple = _stepTuples[index];
                await stepTuple.step.ResetStep(true, true);
            }
        }

        /// <summary>
        /// Overrides the method <see cref="BaseTrainingStep.StopStepAction"/> of the <see cref="BaseTrainingStep"/> class to
        /// stop all steps from <see cref="_stepTuples"/>.
        /// </summary>
        /// <param name="stopNextSteps">Stops all steps in <see cref="BaseTrainingStep._nextSteps"/> if true; otherwise, stops only this step.</param>
        /// <param name="useTimeouts">Optional: Use the timeouts</param>
        public override async UniTask StopStepAction(bool stopNextSteps, bool useTimeouts = false)
        {
            await base.StopStepAction(stopNextSteps, useTimeouts);

            for (var index = 0; index < _stepTuples.Count; index++)
            {
                var stepTuple = _stepTuples[index];
                if (stepTuple.step != this && !base.IsStepInHierarchy(stepTuple.step))
                    await stepTuple.step.StopStepAction(true, useTimeouts);
            }
        }

        /// <summary>
        /// Overrides the method <see cref="BaseTrainingStep.StepCompletedAction"/> of the <see cref="BaseTrainingStep"/> class to
        /// remove <see cref="selectedStep"/> from <see cref="BaseTrainingStep._nextSteps"/> if this step is finished.
        /// </summary>
        protected override void StepCompletedAction()
        {
            base.StepCompletedAction();
            RemoveNextStep(selectedStep);
        }

        /// <summary>
        /// Overrides the method <see cref="BaseTrainingStep.Reset"/> of the <see cref="BaseTrainingStep"/> class to
        /// add an additional child GameObject for 'Optional Steps'.
        /// </summary>
        protected override void Reset()
        {
            base.Reset();

            const string optionalStepsName = "[Optional Steps]";

            var children = transform.Cast<Transform>().ToList();
            if (!children.Exists(t => t.name.Equals(optionalStepsName)))
            {
                var go = new GameObject(optionalStepsName);
                go.transform.SetParent(transform);
            }
        }

        protected override string GameObjectPrefixName() => "[Decision Step]";
    }
}
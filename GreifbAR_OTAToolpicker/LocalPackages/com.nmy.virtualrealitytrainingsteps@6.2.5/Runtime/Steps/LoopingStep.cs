using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace NMY.VirtualRealityTraining.Steps
{
    /// <summary>
    /// A training step that runs a list of <see cref="BaseTrainingStep"/> in a loop.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This step should be used with <see cref="ParallelExecutionStep"/> to run for example
    /// some reminder steps that should be looped until another step finished.
    /// </para>
    /// <para>
    /// <see cref="_iterations"/> = 0 --> runs infinitely<br/>
    /// <see cref="_iterations"/> > 0 --> runs x times<br/>
    /// </para>
    /// </remarks>
    public class LoopingStep : BaseTrainingStep
    {
        /// <summary>
        /// A list of <see cref="BaseTrainingStep"/> that should run in a loop.
        /// </summary>
        [SerializeField] private List<BaseTrainingStep> _loopingSteps;

        /// <summary>
        /// The number of iterations the loop should take. If value == 0, the loop runs infinitely; otherwise, the
        /// loop runs x times. 
        /// </summary>
        [SerializeField] private int _iterations;
        
        private CancellationTokenSource _loopingCancellationTokenSource = new();

        /// <summary>
        /// Overrides the <see cref="BaseTrainingStep.Start"/> method of the <see cref="BaseTrainingStep"/> class to
        /// set <see cref="BaseTrainingStep._waitForAllClientsToFinish"/> to false.
        /// </summary>
        protected override void Start()
        {
            base.Start();
            _waitForAllClientsToFinish = false;
        }

        /// <summary>
        /// Overrides the <see cref="BaseTrainingStep.ClientStepActionAsync"/> method of the <see cref="BaseTrainingStep"/> class to
        /// run all steps within <see cref="_loopingSteps"/> in a loop.
        /// sequence.
        /// </summary>
        /// <param name="ct">The cancellation token to use for cancelling the operation.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        protected override async UniTask ClientStepActionAsync(CancellationToken ct)
        {
            // Create a new linked token source so that we can safely terminate the looping steps within here. 
            var linkedSource =
                CancellationTokenSource.CreateLinkedTokenSource(ct, _loopingCancellationTokenSource.Token);
            var linkedToken = linkedSource.Token;

            try
            {
                await WaitForLoopFinished(linkedToken);
            }
            catch (OperationCanceledException)
            {
            }
            finally
            {
                await ResetLoopingSteps();
                RaiseClientStepFinished();
            }
        }

        /// <summary>
        /// Overrides the <see cref="BaseTrainingStep.ServerStepActionAsync"/> method of the <see cref="BaseTrainingStep"/> class to
        /// run all steps within <see cref="_loopingSteps"/> in a loop.
        /// sequence.
        /// </summary>
        /// <param name="ct">The cancellation token to use for cancelling the operation.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        protected override async UniTask ServerStepActionAsync(CancellationToken ct)
        {
            await base.ServerStepActionAsync(ct);

            // Don't run it twice of playing in host-mode
            if (IsHost) return;

            // Create a new linked token source so that we can safely terminate the looping steps within here. 
            var linkedSource =
                CancellationTokenSource.CreateLinkedTokenSource(ct, _loopingCancellationTokenSource.Token);
            var linkedToken = linkedSource.Token;

            try
            {
                await WaitForLoopFinished(linkedToken);
            }
            catch (OperationCanceledException)
            {
            }
            finally
            {
                await ResetLoopingSteps();
            }
        }

        /// <summary>
        /// Checks the value of <see cref="_iterations"/> and starts either <see cref="LoopInfinitely"/>
        /// or <see cref="LoopForIterations"/>. Asynchronously waits until loop terminates.
        /// </summary>
        /// <param name="ct">The cancellation token to use for cancelling the operation.</param>
        private async UniTask WaitForLoopFinished(CancellationToken ct)
        {
            if (_iterations == 0) await LoopInfinitely(ct);
            else await LoopForIterations(ct);
        }

        /// <summary>
        /// Asynchronously runs all steps from <see cref="_loopingSteps"/> infinitely until <see cref="ct"/> is canceled.
        /// </summary>
        /// <param name="ct">The cancellation token to use for cancelling the operation.</param>
        private async UniTask LoopInfinitely(CancellationToken ct)
        {
            while (!ct.IsCancellationRequested)
            {
                foreach (var step in _loopingSteps)
                {
                    await step.StartStep(ct);
                }
            }
        }

        /// <summary>
        /// Asynchronously runs all steps from <see cref="_loopingSteps"/> x times in a loop.
        /// </summary>
        /// <param name="ct">The cancellation token to use for cancelling the operation.</param>
        private async UniTask LoopForIterations(CancellationToken ct)
        {
            for (var i = 0; i < _iterations; i++)
            {
                foreach (var step in _loopingSteps)
                {
                    await step.StartStep(ct);
                }
            }
        }

        /// <summary>
        /// Resets all steps from <see cref="_loopingSteps"/>
        /// </summary>
        private async UniTask ResetLoopingSteps()
        {
            _loopingCancellationTokenSource.Cancel();

            foreach (var step in _loopingSteps)
                await step.ResetStep(true, true, true);
        }

        /// <summary>
        /// Stops the loop.
        /// </summary>
        public void StopLooping()
        {
            _loopingCancellationTokenSource.Cancel();
        }

        /// <summary>
        /// Overrides the <see cref="BaseTrainingStep.IsStepInHierarchy"/> method of the <see cref="BaseTrainingStep"/> class to
        /// check if the given step is in the hierarchy of steps that are related to this step instance.
        /// </summary>
        /// <remarks>
        /// It does this by checking if the step is in the list of <see cref="_loopingSteps"/> or if it is in the hierarchy of steps
        /// for this DecisionStep instance. The method returns true if the step is found in either of these checks,
        /// and false otherwise.
        /// This method is typically used to determine if a given step is a descendant of this step instance
        /// in the hierarchy of steps in a training sequence.
        /// </remarks>
        /// <param name="step">Instance of <see cref="BaseTrainingStep"/> to check if it is part of this hierarchy.</param>
        /// <param name="visited">The set of <see cref="BaseTrainingStep"/> instances that have already been visited.</param>
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
    
            // Check if the step is in the looping steps
            foreach (var t in _loopingSteps)
            {
                if (t == step) return true;
        
                // Recursively check the hierarchy without revisiting steps
                if (!visited.Contains(t) && t.IsStepInHierarchy(step, visited)) return true;
            }

            // Check the base class hierarchy only if it's not already checked in the looping steps
            return base.IsStepInHierarchy(step, visited);
        }


        /// <summary>
        /// Overrides the method <see cref="BaseTrainingStep.ResetStepState"/> of the <see cref="BaseTrainingStep"/> class to
        /// reset all steps from <see cref="_loopingSteps"/>.
        /// </summary>
        protected override async void ResetStepState()
        {
            base.ResetStepState();

            foreach (var step in _loopingSteps)
                await step.ResetStep(true, true);
        }

        /// <summary>
        /// Overrides the method <see cref="BaseTrainingStep.StopStepAction"/> of the <see cref="BaseTrainingStep"/> class to
        /// stop all steps from <see cref="_loopingSteps"/>.
        /// </summary>
        /// <param name="stopNextSteps">Stops all steps in <see cref="BaseTrainingStep._nextSteps"/> if true; otherwise, stops only this step.</param>
        /// <param name="useTimeouts">Optional: Use the timeouts</param>
        public override async UniTask StopStepAction(bool stopNextSteps, bool useTimeouts = false)
        {
            await base.StopStepAction(stopNextSteps, useTimeouts);

            foreach (var step in _loopingSteps)
            {
                if (step != this && !base.IsStepInHierarchy(step))
                    await step.StopStepAction(true, useTimeouts);
            }
        }

        /// <summary>
        /// Overrides the method <see cref="BaseTrainingStep.Reset"/> of the <see cref="BaseTrainingStep"/> class to
        /// add an additional child GameObject for 'Optional Steps'.
        /// </summary>
        protected override void Reset()
        {
            base.Reset();

            const string optionalStepsName = "[Looping Steps]";

            var children = transform.Cast<Transform>().ToList();
            if (!children.Exists(t => t.name.Equals(optionalStepsName)))
            {
                var go = new GameObject(optionalStepsName);
                go.transform.SetParent(transform);
            }
        }

        protected override string GameObjectPrefixName() => "[Looping Step]";
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace NMY.VirtualRealityTraining.Steps
{
    /// <summary>
    /// A training step that starts a set of steps in parallel and waits until they are finished.
    /// </summary>
    /// <remarks>
    /// If <see cref="_finishingCriteria"/> is <see cref="FinishingCriteria.Any"/>, this step is finished if the
    /// first of the steps in <see cref="_parallelSteps"/> are completed. All other steps will be terminated.
    /// If <see cref="_finishingCriteria"/> is <see cref="FinishingCriteria.All"/>, this step is finished after all
    /// steps in <see cref="_parallelSteps"/> are completed.
    /// If <see cref="_finishingCriteria"/> is <see cref="FinishingCriteria.Selected"/>, this step is finished after all
    /// steps in <see cref="_selectedParallelSteps"/> from <see cref="_parallelSteps"/> are completed.
    /// </remarks>
    public class ParallelExecutionStep : BaseTrainingStep
    {
        /// <summary>
        /// The criteria when the step action of the step will be completed.
        /// </summary>
        [SerializeField] private FinishingCriteria _finishingCriteria;
        
        /// <summary>
        /// A list of <see cref="BaseTrainingStep"/> that should run in parallel.
        /// </summary>
        [SerializeField] private List<BaseTrainingStep> _parallelSteps;

        /// <summary>
        /// A list of <see cref="BaseTrainingStep"/> that are contained in <see cref="_parallelSteps"/> that
        /// must be completed in order to complete this step.
        /// All other steps can complete without completing this step.
        /// When all steps from this list are completed, all remaining steps get canceled.
        /// </summary>
        [SerializeField] private List<BaseTrainingStep> _selectedParallelSteps;

        private CancellationTokenSource _parallelExecutionStepTokenSource = new();

        /// <summary>
        /// An enumeration representing the criteria when the step action of the step will be completed. 
        /// </summary>
        private enum FinishingCriteria
        {
            Any,
            All,
            Selected
        }

        /// <summary>
        /// Overrides the <see cref="BaseTrainingStep.ClientStepActionAsync"/> method of the <see cref="BaseTrainingStep"/> class to
        /// wait until the parallel steps are completed.
        /// sequence.
        /// </summary>
        /// <param name="ct">The cancellation token to use for cancelling the operation.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        protected override async UniTask ClientStepActionAsync(CancellationToken ct)
        {
            // Create a new linked token source so that we can safely terminate the parallel steps within here. 
            var linkedSource =
                CancellationTokenSource.CreateLinkedTokenSource(ct, _parallelExecutionStepTokenSource.Token);
            var linkedToken = linkedSource.Token;

            try
            {
                await WaitUntilParallelStepsFinished(linkedToken);
            }
            catch (OperationCanceledException)
            {
            }
            finally
            {
                RaiseClientStepFinished();
                StopAllSubsteps();
            }
        }

        /// <summary>
        /// Overrides the <see cref="BaseTrainingStep.ServerStepActionAsync"/> method of the <see cref="BaseTrainingStep"/> class to
        /// wait until the parallel steps are completed.
        /// sequence.
        /// </summary>
        /// <param name="ct">The cancellation token to use for cancelling the operation.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        protected override async UniTask ServerStepActionAsync(CancellationToken ct)
        {
            // Don't run it twice of playing in host-mode
            if (IsHost) return;

            // Create a new linked token source so that we can safely terminate the parallel steps within here. 
            var linkedSource =
                CancellationTokenSource.CreateLinkedTokenSource(ct, _parallelExecutionStepTokenSource.Token);
            var linkedToken = linkedSource.Token;

            try
            {
                await WaitUntilParallelStepsFinished(linkedToken);
            }
            catch (OperationCanceledException)
            {
            }
            finally
            {
                StopAllSubsteps();
            }
        }

        /// <summary>
        /// Asynchronously starts and waits for all steps from <see cref="_parallelSteps"/> to be completed.
        /// </summary>
        /// <remarks>
        /// Depending on the value of <see cref="_finishingCriteria"/>,
        /// this method either waits until any step completes (<see cref="FinishingCriteria"/> == <see cref="FinishingCriteria.Any"/>) or when all
        /// steps completes (<see cref="FinishingCriteria"/> == <see cref="FinishingCriteria.All"/>)
        /// </remarks>
        /// <param name="ct">The cancellation token to use for cancelling the operation.</param>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        private async UniTask WaitUntilParallelStepsFinished(CancellationToken ct)
        {
            // Simple store for the Selected mode
            var selectedDict = new Dictionary<UniTask, BaseTrainingStep>();
            var allStepTasks = new List<UniTask>();
            
            foreach (var step in _parallelSteps)
            {
                // We need this train of method invocations for the selected method.
                // Unity throws the following error when we try to run a while-loop with a normal UniTask:
                // InvalidOperationException: Already continuation registered, can not await twice or get Status after await.
                var lazyTask = step.StartStep(ct).ToAsyncLazy().Task;
                
                allStepTasks.Add(lazyTask);
                
                // Only save those tasks whose corresponding step is in the selected list
                if (_selectedParallelSteps.Contains(step))
                    selectedDict.TryAdd(lazyTask, step);
            }

            switch (_finishingCriteria)
            {
                case FinishingCriteria.Any:
                    await UniTask.WhenAny(allStepTasks);
                    break;
                case FinishingCriteria.All:
                    await UniTask.WhenAll(allStepTasks);
                    break;
                case FinishingCriteria.Selected:
                    while (allStepTasks.Count > 0)
                    {
                        // Run all steps and wait for the first to complete.
                        var taskIndex = await UniTask.WhenAny(allStepTasks);
                        var task      = allStepTasks[taskIndex];
                        
                        // Check if completed step was one of the selected steps.
                        if (selectedDict.ContainsKey(task))
                        {
                            selectedDict.Remove(task);
                            // If all selected steps are completed, ignore the rest and break out.
                            if (selectedDict.Count == 0) break;
                        }
                    
                        allStepTasks.RemoveAt(taskIndex);
                    }
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        /// <summary>
        /// Stops all sub steps from <see cref="_parallelSteps"/>.
        /// </summary>
        private void StopAllSubsteps()
        {
            _parallelExecutionStepTokenSource.Cancel();
        }


        /// <summary>
        /// Overrides the <see cref="BaseTrainingStep.IsStepInHierarchy"/> method of the <see cref="BaseTrainingStep"/> class to
        /// check if the given step is in the hierarchy of steps that are related to this step instance.
        /// </summary>
        /// <remarks>
        /// It does this by checking if the step is in the list of <see cref="_parallelSteps"/> or if it is in the hierarchy of steps
        /// for this DecisionStep instance. The method returns true if the step is found in either of these checks,
        /// and false otherwise.
        /// This method is typically used to determine if a given step is a descendant of this step instance
        /// in the hierarchy of steps in a training sequence.
        /// </remarks>
        /// <param name="step">Instance of <see cref="BaseTrainingStep"/> to check if it is part of this hierarchy.</param>
        /// <param name="visited">List of <see cref="BaseTrainingStep"/> that have already been visited.</param>
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
    
            // Check if the step is in the parallel steps
            foreach (var t in _parallelSteps)
            {
                if (t == step) return true;
        
                // Recursively check the hierarchy without revisiting steps
                if (!visited.Contains(t) && t.IsStepInHierarchy(step, visited)) return true;
            }

            // Check the base class hierarchy only if it's not already checked in the parallel steps
            return base.IsStepInHierarchy(step, visited);
        }


        /// <summary>
        /// Overrides the method <see cref="BaseTrainingStep.ResetStepState"/> of the <see cref="BaseTrainingStep"/> class to
        /// reset all steps from <see cref="_parallelSteps"/>.
        /// </summary>
        protected override async void ResetStepState()
        {
            base.ResetStepState();

            foreach (var step in _parallelSteps)
                await step.ResetStep(true, true);
        }

        /// <summary>
        /// Overrides the method <see cref="BaseTrainingStep.StopStepAction"/> of the <see cref="BaseTrainingStep"/> class to
        /// stop all steps from <see cref="_parallelSteps"/>.
        /// </summary>
        /// <param name="stopNextSteps">Stops all steps in <see cref="BaseTrainingStep._nextSteps"/> if true; otherwise, stops only this step.</param>
        /// <param name="useTimeouts">Optional: Use the timeouts</param>
        public override async UniTask StopStepAction(bool stopNextSteps, bool useTimeouts = false)
        {
            await base.StopStepAction(stopNextSteps, useTimeouts);

            foreach (var step in _parallelSteps)
            {
                if (step != this && !base.IsStepInHierarchy(step))
                    await step.StopStepAction(true, useTimeouts);
            }
        }

        public override async UniTask<bool> TryMoveToStep(BaseTrainingStep startTrainingStep,
                                                          CancellationToken stepToken)
        {
            // the step is not found in the hierarchy, we can't move to it
            if (!IsStepInHierarchy(startTrainingStep))
                return await base.TryMoveToStep(startTrainingStep, stepToken);

            // The ParallelStep is the one we want to move to. 
            if (this == startTrainingStep)
            {
                await StartStep(stepToken);
                stepToken.ThrowIfCancellationRequested();

                // we executed a regular StartStep(), which also takes care of all of its _nextSteps, and all events,
                // so we simply return here
                return true;
            }

            // This ParallelStep contains the desired step deeper in the hierarchy.
            
            // Try to mimic the state like we would begin from the start
            Debug.Log($"{GetType()}: Skip {name}", this);

            InvokeOnStepStarted(new BaseTrainingStepEventArgs(this));

            InvokeOnActivatablesActivating(new BaseTrainingStepEventArgs(this));
            InvokeOnActivatablesActivated(new BaseTrainingStepEventArgs(this));
            
            await ActivatePersistantActivatablesAsync(stepToken, false);

            InvokeOnStepActionStarted(new BaseTrainingStepEventArgs(this));
            
            await ExecuteMoveToStepAction(startTrainingStep, stepToken);

            InvokeOnStepActionFinished(new BaseTrainingStepEventArgs(this));
            InvokeOnStepFinished(new BaseTrainingStepEventArgs(this));

            if (IsServer) _stepState.Value = StepState.StepFinished;
            
            InvokeOnActivatablesDeactivating(new BaseTrainingStepEventArgs(this));
            InvokeOnActivatablesDeactivated(new BaseTrainingStepEventArgs(this));
            await DeactivatePersistantStepsAsync(stepToken, false);
            
            // Start all other steps that comes after the start step
            for (var i = 0; i < nextSteps.Count; i++)
            {
                if (nextSteps[i] == null) continue;
                await nextSteps[i].StartStep(stepToken);
                stepToken.ThrowIfCancellationRequested();
            }
            
            await UniTask.Delay(TimeSpan.FromSeconds(timeoutBeforeStepComplete), cancellationToken: stepToken);

            stepToken.ThrowIfCancellationRequested();

            InvokeOnStepCompleted(new BaseTrainingStepEventArgs(this));
            if (IsServer) _stepState.Value = StepState.StepCompleted;
            
            return true;
        }

        /// <summary>
        /// This is a more or less copy of the <see cref="WaitUntilParallelStepsFinished"/> functionality, but with the
        /// the twist that we try to move to the start training step when executing in parallel.
        /// </summary>
        /// <param name="startTrainingStep"></param>
        /// <param name="ct"></param>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        private async UniTask ExecuteMoveToStepAction(BaseTrainingStep startTrainingStep, CancellationToken ct)
        {
            // Simple store for the Selected mode
            var selectedDict = new Dictionary<UniTask, BaseTrainingStep>();
            var allStepTasks = new List<UniTask>();
            
            var completedAny = false;
            
            foreach (var step in _parallelSteps)
            {
                // We need this train of method invocations for the selected method.
                // Unity throws the following error when we try to run a while-loop with a normal UniTask:
                // InvalidOperationException: Already continuation registered, can not await twice or get Status after await.

                UniTask lazyTask;

                // The parallel step is the start step
                if (step == startTrainingStep)
                {
                    lazyTask = step.StartStep(ct).ToAsyncLazy().Task;
                    allStepTasks.Add(lazyTask);
                    
                    // Only save those tasks whose corresponding step is in the selected list
                    if (_selectedParallelSteps.Contains(step))
                        selectedDict.TryAdd(lazyTask, step);
                }
                // Iff one of the steps in the parallel steps contain the start step, try to move to it
                else if (step.IsStepInHierarchy(startTrainingStep))
                {
                    lazyTask = step.TryMoveToStep(startTrainingStep, ct);
                    allStepTasks.Add(lazyTask);
                    
                    // Only save those tasks whose corresponding step is in the selected list
                    if (_selectedParallelSteps.Contains(step))
                        selectedDict.TryAdd(lazyTask, step);
                }
                // For all other parallel steps that do not contain the start step, we just start them
                // TODO: This could potentially fail if two steps are started at the same time while the hierarchy is too deep.
                else
                {
                    lazyTask = step.StartStep(ct).ToAsyncLazy().Task;
                    allStepTasks.Add(lazyTask);
                    
                    // Only save those tasks whose corresponding step is in the selected list
                    if (_selectedParallelSteps.Contains(step))
                        selectedDict.TryAdd(lazyTask, step);
                }

                // Remove all completed steps to be in sync with server
                if (step.stepState is StepState.StepCompleted or StepState.StepStopped)
                {
                    completedAny = true;
                    allStepTasks.Remove(lazyTask);
                    if (selectedDict.ContainsKey(lazyTask))
                        selectedDict.Remove(lazyTask);
                }
            }

            switch (_finishingCriteria)
            {
                case FinishingCriteria.Any:
                    if (!completedAny) await UniTask.WhenAny(allStepTasks);
                    break;
                case FinishingCriteria.All:
                    await UniTask.WhenAll(allStepTasks);
                    break;
                case FinishingCriteria.Selected:
                    while (allStepTasks.Count > 0)
                    {
                        // Run all steps and wait for the first to complete.
                        var taskIndex = await UniTask.WhenAny(allStepTasks);
                        var task      = allStepTasks[taskIndex];
                        
                        // Check if completed step was one of the selected steps.
                        if (selectedDict.ContainsKey(task))
                        {
                            selectedDict.Remove(task);
                            // If all selected steps are completed, ignore the rest and break out.
                            if (selectedDict.Count == 0) break;
                        }
                    
                        allStepTasks.RemoveAt(taskIndex);
                    }
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }        
            
            if (IsClient) RaiseClientStepFinished();
            StopAllSubsteps();
        }

        /// <summary>
        /// Overrides the method <see cref="BaseTrainingStep.Reset"/> of the <see cref="BaseTrainingStep"/> class to
        /// add an additional child GameObject for 'Optional Steps'.
        /// </summary>
        protected override void Reset()
        {
            base.Reset();

            const string optionalStepsName = "[Parallel Steps]";

            var children = transform.Cast<Transform>().ToList();
            if (!children.Exists(t => t.name.Equals(optionalStepsName)))
            {
                var go = new GameObject(optionalStepsName);
                go.transform.SetParent(transform);
            }
        }

        protected override string GameObjectPrefixName() => "[Parallel Step]";

        private void OnValidate()
        {
            for (var i = _selectedParallelSteps.Count - 1; i >= 0; i--)
            {
                var selectedStep = _selectedParallelSteps[i];

                if (!_parallelSteps.Contains(selectedStep))
                {
                    _selectedParallelSteps.RemoveAt(i);
                }
            }
        }
    }
}
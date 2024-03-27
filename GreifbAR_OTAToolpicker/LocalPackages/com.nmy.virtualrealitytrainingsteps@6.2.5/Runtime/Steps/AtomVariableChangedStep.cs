#if NMY_ENABLE_UNITY_ATOMS

using System;
using System.Threading;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using UnityAtoms;
using UnityEngine;

namespace NMY.VirtualRealityTraining.Steps
{
    /// <summary>
    /// A training step that waits for a change in the value of an Atom variable before moving
    /// on to the next step in the training sequence.
    /// <remarks>
    /// This component is only available if 'Unity Atoms' is included in the Package Manager.<br/>
    /// Use this step when you only need to know if a variable has changed its value,
    /// but do not need the exact value to match an other value.
    /// </remarks>
    /// </summary>
    public class AtomVariableChangedStep : BaseTrainingStep
    {
        /// <summary>
        /// The Atom variable to be monitored for changes.
        /// </summary>
        [SerializeField] private AtomBaseVariable _atomVariable;

        /// <summary>
        /// Overrides the <see cref="BaseTrainingStep.ClientStepActionAsync"/> method of the <see cref="BaseTrainingStep"/> class to
        /// asynchronously wait for a change in the value of the Atom variable specified by the <see cref="_atomVariable"/> property.
        /// </summary>
        /// <remarks>
        /// This method calls <see cref="WaitUntilAtomVariableChanged"/> to wait until the atom variable changed,
        /// and then calls <see cref="BaseTrainingStep.RaiseClientStepFinished"/> to signal that the step is finished.
        /// If the Atom variable cannot be found, a warning is logged and the step is skipped.
        /// If the cancellation token is cancelled while waiting, <see cref="BaseTrainingStep.RaiseClientStepFinished"/> is still called.
        /// </remarks>
        /// <param name="ct">The cancellation token for the async operation.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        protected override async UniTask ClientStepActionAsync(CancellationToken ct)
        {
            try
            {
                if (_atomVariable != null)
                    await WaitUntilAtomVariableChanged(ct);
                else Debug.LogWarning($"{GetType()}: Atom Variable could not be found, skip this step!", this);
                RaiseClientStepFinished();
            }
            catch (OperationCanceledException)
            {
                RaiseClientStepFinished();
            }
        }

        /// <summary>
        /// Asynchronously waits until the value of the atom variable changes.
        /// </summary>
        /// <remarks>
        /// This method uses the <see cref="UniTask.WaitUntilValueChanged{T,U}"/> method to determine whether
        /// <see cref="AtomVariable{T,P,E1,E2,F}.BaseValue"/> of <see cref="AtomVariable{T,P,E1,E2,F}"/> has changed its value
        /// and waits until this condition is met or until the cancellation token is cancelled.
        /// </remarks>
        /// <param name="ct">The cancellation token for the async operation.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        private async UniTask WaitUntilAtomVariableChanged(CancellationToken ct)
        {
            await UniTask.WaitUntilValueChanged(_atomVariable, x => _atomVariable.BaseValue,
                                                cancellationToken: ct);
        }

        protected override string GameObjectPrefixName() => "[Atom Changed Step]";
    }
}

#endif
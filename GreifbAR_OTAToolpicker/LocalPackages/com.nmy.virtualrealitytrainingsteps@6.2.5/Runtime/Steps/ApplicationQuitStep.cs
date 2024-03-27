using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace NMY.VirtualRealityTraining.Steps
{
    /// <summary>
    /// A class for defining a training step that quits the application when executed.
    /// This class extends the <see cref="BaseTrainingStep"/> class and is used to define a step that quits the application
    /// when the `ClientStepActionAsync` or `ServerStepActionAsync` methods are called.
    /// </summary>
    /// <remarks>
    /// The <see cref="_serverDelay"/> field can be used to specify the amount of time to wait on the server before quitting the application.
    /// </remarks>
    public class ApplicationQuitStep : BaseTrainingStep
    {
        [SerializeField] private float _serverDelay = 5;

#region Overrides

        /// <summary>
        /// Overrides the <see cref="BaseTrainingStep.ClientStepActionAsync"/> method of the <see cref="BaseTrainingStep"/> class to
        /// quit the application by calling <see cref="ExitGame"/>.
        /// </summary>
        /// <param name="ct">The cancellation token for the async operation.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        protected override async UniTask ClientStepActionAsync(CancellationToken ct)
        {
            ExitGame();
            await UniTask.CompletedTask;
        }

        /// <summary>
        /// Overrides the <see cref="BaseTrainingStep.ServerStepActionAsync"/> method of the <see cref="BaseTrainingStep"/> class to
        /// wait for the specified amount of time as determined by the <see cref="_serverDelay"/> field
        /// and then to quit the application by calling <see cref="ExitGame"/>.
        /// </summary>
        /// <param name="ct">The cancellation token for the async operation.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        protected override async UniTask ServerStepActionAsync(CancellationToken ct)
        {
            try
            {
                await UniTask.Delay(TimeSpan.FromSeconds(_serverDelay), cancellationToken: ct);
                ExitGame();
            }
            catch (OperationCanceledException) { }
        }

        /// <summary>
        /// Overrides the <see cref="BaseTrainingStep.ExecuteMoveToStepAction"/> method of <see cref="BaseTrainingStep"/>
        /// to automatically exit the game when the step is skipped.
        /// </summary>
        /// <param name="ct"></param>
        /// <returns>A task representing the asynchronous operation.</returns>
        protected override async UniTask ExecuteMoveToStepAction(CancellationToken ct = default)
        {
            ExitGame();
            await base.ExecuteMoveToStepAction(ct);
        }
        
        protected override string GameObjectPrefixName() => "[Quit Step]";

#endregion


        /// <summary>
        /// Quits the application.
        /// </summary>
        private static void ExitGame()
        {
#if UNITY_EDITOR
            if (EditorApplication.isPlaying) EditorApplication.ExitPlaymode();
#endif
            Application.Quit();
        }
    }
}

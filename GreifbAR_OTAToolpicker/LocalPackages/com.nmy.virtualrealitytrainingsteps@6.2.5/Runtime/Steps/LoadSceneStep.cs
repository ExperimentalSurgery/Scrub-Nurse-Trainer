using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace NMY.VirtualRealityTraining.Steps
{
    /// <summary>
    /// A training step that loads a scene when this step is executed.
    /// </summary>
    /// <remarks>
    /// Typically used at the end of a scene to switch to another scene.
    /// </remarks>
    public class LoadSceneStep : BaseTrainingStep
    {
        /// <summary>
        /// the name of the scene to be loaded when this step is executed.
        /// </summary>
        /// <remarks>
        /// The scene name should be the name of the scene as it appears in the build settings.
        /// </remarks>
        [Scene]
        [SerializeField] private string _sceneName;

        /// <summary>
        /// The <see cref="CameraFadeBase"/> instance that is used to control the fade effect when loading the scene.
        /// </summary>
        [Header("Fade Settings")]
        [SerializeField] private CameraFadeBase _cameraFade;

        /// <summary>
        /// The duration of the fade effect when loading the scene in seconds.
        /// </summary>
        [SerializeField] private float _fadeDuration = 2f;

        /// <summary>
        /// The duration between the end of the  fade effect and loading the scene in seconds.
        /// </summary>
        [SerializeField] private float _waitDuration = 0f;
        

        /// <summary>
        /// Overrides the <see cref="BaseTrainingStep.ClientStepActionAsync"/> method of the <see cref="BaseTrainingStep"/> class to
        /// asynchronously start the fade effect for the client before signaling that the step is finished for the local client.
        /// </summary>
        /// <param name="ct">A <see cref="CancellationToken"/> used to cancel the asynchronous operation.</param>
        /// <returns>A <see cref="UniTask"/> that represents the asynchronous operation.</returns>
        protected override async UniTask ClientStepActionAsync(CancellationToken ct)
        {
            try
            {
                if (_cameraFade) await _cameraFade.FadeInAsync(_fadeDuration);
                await UniTask.Delay(TimeSpan.FromSeconds(_waitDuration), cancellationToken: ct);
                RaiseClientStepFinished();
            }
            catch (OperationCanceledException)
            {
                RaiseClientStepFinished();
            }
        }

        /// <summary>
        /// Overrides the <see cref="BaseTrainingStep.ServerStepActionAsync"/> method of the <see cref="BaseTrainingStep"/> class to
        /// asynchronously wait for <see cref="_fadeDuration"/> seconds before loading the new scene.
        /// </summary>
        /// <param name="ct">A <see cref="CancellationToken"/> used to cancel the asynchronous operation.</param>
        /// <returns>A <see cref="UniTask"/> that represents the asynchronous operation.</returns>
        /// <seealso cref="LoadSceneSingle"/>
        protected override async UniTask ServerStepActionAsync(CancellationToken ct)
        {
            try
            {
                await UniTask.Delay(TimeSpan.FromSeconds(_fadeDuration + _waitDuration), cancellationToken: ct);
                LoadSceneSingle(_sceneName);
            }
            catch (OperationCanceledException)
            {
            }
        }

        /// <summary>
        /// Loads the specified scene in single mode on the server.
        /// </summary>
        /// <remarks>
        /// It propagates the scene change to all clients through <see cref="NetworkSceneManager.LoadScene"/>
        /// </remarks>
        /// <param name="sceneName">The name of the scene to be loaded.</param>
        private static void LoadSceneSingle(string sceneName)
        {
            if (NetworkManager.Singleton.IsServer && !string.IsNullOrEmpty(sceneName))
            {
                var status = NetworkManager.Singleton.SceneManager.LoadScene(sceneName, LoadSceneMode.Single);
                if (status != SceneEventProgressStatus.Started)
                {
                    Debug.LogWarning($"{typeof(LoadSceneStep)}: Failed to load {sceneName} " +
                                     $"with a {nameof(SceneEventProgressStatus)}: {status}");
                }
            }
        }

        protected override string GameObjectPrefixName() => "[Load Scene Step]";
    }
}
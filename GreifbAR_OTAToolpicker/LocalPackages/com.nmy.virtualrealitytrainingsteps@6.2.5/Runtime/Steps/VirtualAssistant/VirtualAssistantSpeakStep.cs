using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
#if NMY_ENABLE_GOOGLE_CLOUD_TTS
using NMY.GoogleCloudTextToSpeech;
#endif

namespace NMY.VirtualRealityTraining.Steps.VirtualAssistant
{
    /// <summary>
    /// A training step that allows a virtual assistant to speak using <see cref="LocalizedTextToSpeechAudioClip"/>
    /// before signaling that the step is finished.
    /// </summary>
    public class VirtualAssistantSpeakStep : BaseVirtualAssistantTrainingStep
    {
#region Serialized Fields

#if NMY_ENABLE_GOOGLE_CLOUD_TTS
        /// <summary>
        /// The localized text-to-speech audio clip that the virtual assistant should use to speak in this training step.
        /// </summary>
        [Header("Speak Settings")]
        [SerializeField] private LocalizedTextToSpeechAudioClip _localizedTtsAudioClip;
#endif

#endregion

        /// <summary>
        /// Overrides the <see cref="BaseTrainingStep.ClientStepActionAsync"/> method of the <see cref="BaseTrainingStep"/> class to
        /// asynchronously wait for the virtual assistant to speak the given audio from <see cref="_localizedTtsAudioClip"/>
        /// before signaling that the step is finished for the local client.
        /// </summary>
        /// <remarks>
        /// If the cancellation token is cancelled while waiting, the virtual assistant stops speaking
        /// and <see cref="BaseTrainingStep.RaiseClientStepFinished"/> is still called.
        /// </remarks>
        /// <param name="ct">The cancellation token for the async operation.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        protected override async UniTask ClientStepActionAsync(CancellationToken ct)
        {
            try
            {
                await base.ClientStepActionAsync(ct);
#if NMY_ENABLE_GOOGLE_CLOUD_TTS
                await VirtualRealityTraining.VirtualAssistant.VirtualAssistant.instance.Speak(_localizedTtsAudioClip, ct, (float)deltaTime);
#endif
                RaiseClientStepFinished();
            }
            catch (OperationCanceledException)
            {
                await VirtualRealityTraining.VirtualAssistant.VirtualAssistant.instance.StopSpeaking();
                RaiseClientStepFinished();
            }
        }
        
        /// <summary>
        /// Overrides the <see cref="AbstractTimeBasedStep.ServerStepActionAsync"/> method of the <see cref="AbstractTimeBasedStep"/> class to
        /// asynchronously wait for the virtual assistant to speak the given audio from <see cref="_localizedTtsAudioClip"/> on the server side.
        /// </summary>
        protected override async UniTask ServerStepActionAsync(CancellationToken ct)
        {
            try
            {
                await base.ServerStepActionAsync(ct);
#if NMY_ENABLE_GOOGLE_CLOUD_TTS
                await VirtualRealityTraining.VirtualAssistant.VirtualAssistant.instance.Speak(_localizedTtsAudioClip, ct, (float)deltaTime);
#endif
            }
            catch (OperationCanceledException)
            {
                await VirtualRealityTraining.VirtualAssistant.VirtualAssistant.instance.StopSpeaking();
            }
        }

        /// <summary>
        /// Overrides the <see cref="BaseTrainingStep.ExecuteMoveToStepAction"/> method of <see cref="BaseTrainingStep"/>
        /// to automatically stop the virtual assistant to speak.
        /// </summary>
        /// <param name="ct"></param>
        /// <returns>A task representing the asynchronous operation.</returns>
        protected override async UniTask ExecuteMoveToStepAction(CancellationToken ct = default)
        {
            await VirtualRealityTraining.VirtualAssistant.VirtualAssistant.instance.StopSpeaking();
            await base.ExecuteMoveToStepAction(ct);
        }

#if NMY_ENABLE_GOOGLE_CLOUD_TTS
        /// <summary>
        /// Sets the text and language that the virtual assistant should speak in this training step.
        /// </summary>
        /// <param name="audioClip">The localized text-to-speech audio clip to use for this training step.</param>
        public void SetLocalizedAudioClip(LocalizedTextToSpeechAudioClip audioClip) =>
            _localizedTtsAudioClip = audioClip;
#endif

        protected override string GameObjectPrefixName() => "[VA Speak Step]";

    }
}
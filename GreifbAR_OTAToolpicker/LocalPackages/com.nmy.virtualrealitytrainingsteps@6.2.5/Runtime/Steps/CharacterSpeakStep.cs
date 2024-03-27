#if NMY_ENABLE_GOOGLE_CLOUD_TTS

using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using NMY.GoogleCloudTextToSpeech;
using UnityEngine;
using UnityEngine.Localization.Settings;
using UnityEngine.ResourceManagement.Exceptions;

namespace NMY.VirtualRealityTraining.Steps
{
    /// <summary>
    /// A training step that involves a character speaking and animating.
    /// </summary>
    public class CharacterSpeakStep : AbstractTimeBasedStep
    {
        /// <summary>
        /// The <see cref="AudioSource"/> component used to play the character's speech audio.
        /// </summary>
        [SerializeField] private AudioSource _audioSource;

        /// <summary>
        /// The <see cref="LocalizedTextToSpeechAudioClip"/> asset containing the character's speech audio and timestamps for animating the character.
        /// </summary>
        [SerializeField] private LocalizedTextToSpeechAudioClip _localizedAudioClip;

        /// <summary>
        /// The <see cref="Animator"/> component used to animate the character.
        /// </summary>
        [SerializeField] private Animator _animator;

        /// <summary>
        /// The animation trigger for the talk animation.
        /// </summary>
        [AnimatorParam(nameof(_animator))]
        [SerializeField] private string _animationTriggerTalk = "talk";
        
        /// <summary>
        /// The animation trigger for the idle animation.
        /// </summary>
        [AnimatorParam(nameof(_animator))]
        [SerializeField] private string _animationTriggerIdle = "idle";
        

        /// <summary>
        /// Overrides the <see cref="AbstractTimeBasedStep.ClientStepActionAsync"/> method of the <see cref="AbstractTimeBasedStep"/> class to
        /// asynchronously perform the character speaking and animate the character
        /// before signaling that the step is finished for the local client.
        /// </summary>
        /// <remarks>
        /// This method calls <see cref="WaitForClipFinished"/> to wait for the character to speak until the end,
        /// and then calls <see cref="BaseTrainingStep.RaiseClientStepFinished"/> to signal that the step is finished.
        /// If the cancellation token is cancelled while loading the asset or while waiting to finish the audio to play,
        /// <see cref="BaseTrainingStep.RaiseClientStepFinished"/> is still called.
        /// </remarks>
        /// <param name="ct">A <see cref="CancellationToken"/> used to cancel the asynchronous operation.</param>
        /// <returns>A <see cref="UniTask"/> that represents the asynchronous operation.</returns>
        protected override async UniTask ClientStepActionAsync(CancellationToken ct)
        {
            if (_audioSource == null)
            {
                Debug.LogWarning($"{GetType()}: {nameof(_audioSource)} is null", this);
                RaiseClientStepFinished();
                return;
            }

            if (_localizedAudioClip == null || _localizedAudioClip.IsEmpty)
            {
                Debug.LogError($"{GetType()}: Localized Audioclip '{nameof(_localizedAudioClip)}' is null or empty", this);
                RaiseClientStepFinished();
                return;
            }

            try
            {
                await base.ClientStepActionAsync(ct);
                await LocalizationSettings.InitializationOperation;
                var item = await _localizedAudioClip.LoadAssetAsync().ToUniTask(cancellationToken: ct);

                if (item != null && item.audioClip != null)
                {
                    foreach (var entry in item.timestamps) TriggerExpression(entry, ct).Forget();

                    _audioSource.clip = item.audioClip;
                    _audioSource.time = (float)CorrectedTime(item.GetDuration());
                    _audioSource.Play();

                    TalkAnimation();
                    await WaitForClipFinished(RemainingTime(item.GetDuration()), ct);
                    IdleAnimation();
                }
                else
                {
                    Debug.LogError($"{GetType()}: LocalizedTextToSpeechItem or AudioClip could not be loaded!", this);
                }

                RaiseClientStepFinished();
            }
            catch (OperationCanceledException)
            {
                _audioSource.Stop();
                IdleAnimation();

                RaiseClientStepFinished();
            }
            // Something went completely wrong with the asset in the AssetTable - maybe the asset was deleted from the
            // file system and could therefore not be loaded?
            catch (OperationException)
            {
                Debug.LogError($"{GetType()}: Could not load LocalizedTextToSpeechAudioClip Asset.", this);
                RaiseClientStepFinished();
            }
        }
        
        /// <summary>
        /// Overrides the <see cref="AbstractTimeBasedStep.ServerStepActionAsync"/> method of the <see cref="AbstractTimeBasedStep"/> class to
        /// asynchronously perform the character speaking and animate the character on server side.
        /// </summary>
        protected override async UniTask ServerStepActionAsync(CancellationToken ct)
        {
            if (_audioSource == null)
            {
                Debug.LogWarning($"{GetType()}: {nameof(_audioSource)} is null", this);
                return;
            }

            if (_localizedAudioClip == null || _localizedAudioClip.IsEmpty)
            {
                Debug.LogError($"{GetType()}: Localized Audioclip is null or empty", this);
                return;
            }

            try
            {
                await base.ServerStepActionAsync(ct);
                await LocalizationSettings.InitializationOperation;
                var item = await _localizedAudioClip.LoadAssetAsync().ToUniTask(cancellationToken: ct);

                if (item != null && item.audioClip != null)
                {
                    foreach (var entry in item.timestamps) TriggerExpression(entry, ct).Forget();

                    _audioSource.clip = item.audioClip;
                    _audioSource.time = (float)CorrectedTime(item.GetDuration());
                    _audioSource.Play();

                    TalkAnimation();
                    await WaitForClipFinished(RemainingTime(item.GetDuration()), ct);
                    IdleAnimation();
                }
                else
                {
                    Debug.LogError($"{GetType()}: LocalizedTextToSpeechItem or AudioClip could not be loaded!", this);
                }
            }
            catch (OperationCanceledException)
            {
                _audioSource.Stop();
                IdleAnimation();
            }
            // Something went completely wrong with the asset in the AssetTable - maybe the asset was deleted from the
            // file system and could therefore not be loaded?
            catch (OperationException)
            {
                Debug.LogError($"{GetType()}: Could not load LocalizedTextToSpeechAudioClip Asset.", this);
            }
        }

        /// <summary>
        /// Starts the talk animation of the character if an animator is provided.
        /// </summary>
        private void TalkAnimation()
        {
            if (!_animator) return;
            if (_animationTriggerTalk == "") return;
            _animator.SetTrigger(_animationTriggerTalk);
        }

        /// <summary>
        /// Starts the idle animation of the character if an animator is provided.
        /// </summary>
        private void IdleAnimation()
        {
            if (!_animator) return;
            if (_animationTriggerIdle == "") return;
            _animator.SetTrigger(_animationTriggerIdle);
        }

        /// <summary>
        /// Asynchronously waits for a clip to finish playing on all clients before moving on to the next step.
        /// </summary>
        /// <param name="duration">The duration to wait for.</param>
        /// <param name="ct">A <see cref="CancellationToken"/> used to cancel the asynchronous operation.</param>
        /// <returns>A <see cref="UniTask"/> that represents the asynchronous operation.</returns>
        private static UniTask WaitForClipFinished(double duration, CancellationToken ct) =>
            UniTask.Delay(TimeSpan.FromSeconds(duration), cancellationToken: ct);

        /// <summary>
        /// Trigger an expression on a client after a specified time has elapsed.
        /// </summary>
        /// <param name="entry">An object that specifies the time to wait and the expression to trigger.</param>
        /// <param name="ct">A cancellation token that can be used to cancel the operation.</param>
        private async UniTaskVoid TriggerExpression(TextToSpeechTimestampEntry entry, CancellationToken ct)
        {
            await UniTask.Delay(TimeSpan.FromSeconds(entry.timeSeconds), cancellationToken: ct);
            TriggerExpression(entry.markName);
        }

        /// <summary>
        /// Trigger an expression on the client's animator.
        /// </summary>
        /// <param name="trigger">The name of the trigger to set on the animator.</param>
        public void TriggerExpression(string trigger)
        {
            if (_animator is null) return;
            if (string.IsNullOrEmpty(trigger)) return;

            _animator.SetTrigger(trigger);
        }

        protected override string GameObjectPrefixName() => "[Speak Step]";
    }
}
#endif
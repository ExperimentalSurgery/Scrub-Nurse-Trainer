using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace NMY.VirtualRealityTraining.Steps
{
    /// <summary>
    /// A training step that plays a timeline on a Playable Director.
    /// The step will wait until the start time is set on the server before playing the timeline,
    /// and it will correct the playback time to match the amount of time that has elapsed since the step was started on the server.
    /// The step will finish when the timeline is finished playing.
    /// <remarks>
    /// The step action is to wait until the start time of the step is set on the server and then play the director
    /// on the client. The director is only played once on the client and any further changes to the director will not
    /// be reflected.
    /// </remarks>
    /// </summary>
    public class TimelineStep : AbstractTimeBasedStep
    {
        /// <summary>
        /// The <see cref="PlayableDirector"/> that will be played by this step.
        /// </summary>
        [SerializeField] private PlayableDirector _playableDirector;

        /// <summary>
        /// Overrides the <see cref="BaseTrainingStep.Awake"/> method of the <see cref="BaseTrainingStep"/> class to
        /// set the <see cref="PlayableDirector.playOnAwake"/> flag of the <see cref="PlayableDirector"/> class to false.
        /// </summary>
        protected override void Awake()
        {
            base.Awake();
            if (_playableDirector) _playableDirector.playOnAwake = false;
        }

        /// <summary>
        /// Overrides the <see cref="BaseTrainingStep.ClientStepActionAsync"/> method of the <see cref="BaseTrainingStep"/> class to
        /// asynchronously wait until the timeline has finished playing before signaling that the step is finished for the local client.
        /// </summary>
        /// <remarks>
        /// This method calls <see cref="WaitForTimelineFinished"/> to wait until the timeline has finished playing,
        /// and then calls <see cref="BaseTrainingStep.RaiseClientStepFinished"/> to signal that the step is finished.
        /// If the cancellation token is cancelled while waiting, the <see cref="_playableDirector"/> is stopped and set to
        /// the end frame, than <see cref="BaseTrainingStep.RaiseClientStepFinished"/> is called.
        /// </remarks>
        /// <param name="ct">The cancellation token for the async operation.</param>
        /// <returns>A task representing the asynchronous operation.</returns>

        protected override async UniTask ClientStepActionAsync(CancellationToken ct)
        {
            try
            {
                await base.ClientStepActionAsync(ct);
                
                if (_playableDirector != null)
                {
                    _playableDirector.time = CorrectedTime(_playableDirector.duration);
                    _playableDirector.Play();
                    await WaitForTimelineFinished(ct);
                }
                else
                {
                    Debug.LogWarning($"{GetType()}: Playable Director could not be found, skip this step.", this);
                }
                
                RaiseClientStepFinished();
            }
            catch (OperationCanceledException)
            {
                _playableDirector.time = _playableDirector.duration;
                RaiseClientStepFinished();
            }
        }
        
        /// <summary>
        /// Overrides the <see cref="AbstractTimeBasedStep.ServerStepActionAsync"/> method of the <see cref="AbstractTimeBasedStep"/> class to
        /// asynchronously wait for the timeline to be finished.
        /// </summary>
        protected override async UniTask ServerStepActionAsync(CancellationToken ct)
        {
            try
            {
                await base.ServerStepActionAsync(ct);
                if (_playableDirector != null)
                {
                    _playableDirector.time = CorrectedTime(_playableDirector.duration);
                    _playableDirector.Play();
                    await WaitForTimelineFinished(ct);
                }
                else
                {
                    Debug.LogWarning($"{GetType()}: Playable Director could not be found, skip this step.", this);
                }
            }
            catch (OperationCanceledException)
            {
                _playableDirector.time = _playableDirector.duration;
            }
        }

        /// <summary>
        /// Asynchronously waits until the timeline is finished playing.
        /// </summary>
        /// <param name="ct">The cancellation token for the async operation.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        private UniTask WaitForTimelineFinished(CancellationToken ct) =>
            UniTask.WaitUntil(() => _playableDirector.extrapolationMode is DirectorWrapMode.Hold or DirectorWrapMode.Loop
                                    ? Math.Abs(_playableDirector.time - _playableDirector.duration) < 0.01
                                    : _playableDirector.state != PlayState.Playing, cancellationToken: ct);

        /// <summary>
        /// Overrides the base implementation to play the <see cref="_playableDirector"/> to the end.
        /// </summary>
        /// <param name="ct"></param>
        /// <returns>A task that completes when the teleportation is complete.</returns>
        protected override async UniTask ExecuteMoveToStepAction(CancellationToken ct = default)
        {
            if (_playableDirector != null)
            {

                var easeOutDuration = GetEndEaseOutDuration();
                _playableDirector.time = _playableDirector.duration - easeOutDuration;
                _playableDirector.Play();
                //TODO: do we need to wait for the easeout duration?
                //await UniTask.Delay(TimeSpan.FromSeconds(easeOutDuration), cancellationToken: ct);
            }
            
            await base.ExecuteMoveToStepAction(ct);
        }

        /// <summary>
        /// Finds the greatest ease out duration value at the end of the timeline for blending character's animation
        /// into their animator idle animation
        /// </summary>
        /// <returns>The greatest ease out duration value at the and of the timeline; 0 otherwise</returns>
        private double GetEndEaseOutDuration()
        {
            var greatestEaseOutDuration = 0d;

            var timeline = _playableDirector.playableAsset as TimelineAsset;
            if (timeline == null) return greatestEaseOutDuration;
            
            foreach (var outputTrack in timeline.GetOutputTracks())
            {
                foreach (var timelineClip in outputTrack.GetClips())
                {
                    // consider only those clips that are at the end of the timeline
                    if (Math.Abs(timelineClip.end - _playableDirector.duration) < 0.0001)
                    {
                        greatestEaseOutDuration = math.max(greatestEaseOutDuration, timelineClip.easeOutDuration);
                    }
                }
            }

            return greatestEaseOutDuration;
        }

        protected override string GameObjectPrefixName() => "[Timeline Step]";
    }
}

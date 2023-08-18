using System;
using UnityEngine;
using UnityEngine.Playables;

namespace NMY.GoogleCloudTextToSpeech
{
#if NMY_ENABLE_TIMELINE
    [Serializable]
    class LocalizedTextToSpeechItemPlayableBehaviour : PlayableBehaviour
    {
        public                LocalizedTextToSpeechAudioClip localizedAudioClip;
        [Range(0f,1f)] public float                          volume = 1;
        public                bool                           loop { get; set; }

        private AudioSource _audioSource;
        
        private bool        _isRunning;
        private bool        _isAudioClipPlaying;

        public override void OnGraphStart(Playable playable)
        {
            base.OnGraphStart(playable);
            // Debug.Log($"{GetType()}::{MethodBase.GetCurrentMethod().Name}");

            _isRunning = true;
        }

        public override void OnGraphStop(Playable playable)
        {
            base.OnGraphStop(playable);
            // Debug.Log($"{GetType()}::{MethodBase.GetCurrentMethod().Name}");
            
            _isRunning = false;
            PauseAudio();
        }

        public override void OnPlayableCreate(Playable playable)
        {
            base.OnPlayableCreate(playable);
            // Debug.Log($"{GetType()}::{MethodBase.GetCurrentMethod().Name}");
        }

        public override void OnPlayableDestroy(Playable playable)
        {
            base.OnPlayableDestroy(playable);
            // Debug.Log($"{GetType()}::{MethodBase.GetCurrentMethod().Name}");
            PauseAudio();
        }


        public override void OnBehaviourPlay(Playable playable, FrameData info)
        {
            base.OnBehaviourPlay(playable, info);
            // Debug.Log($"{GetType()}::{MethodBase.GetCurrentMethod().Name}");
        }

        public override void OnBehaviourPause(Playable playable, FrameData info)
        {
            base.OnBehaviourPause(playable, info);
            // Debug.Log($"{GetType()}::{MethodBase.GetCurrentMethod().Name}");
            
            if (Application.isPlaying)
                PauseAudio();
            else
                StopAudio();
        }


        public override void PrepareData(Playable playable, FrameData info)
        {
            base.PrepareData(playable, info);
            // Debug.Log($"{GetType()}::{MethodBase.GetCurrentMethod().Name}");
        }

        public override void PrepareFrame(Playable playable, FrameData info)
        {
            base.PrepareFrame(playable, info);
            // Debug.Log($"{GetType()}::{MethodBase.GetCurrentMethod().Name}");
        }

        public override void ProcessFrame(Playable playable, FrameData info, object playerData)
        {
            base.ProcessFrame(playable, info, playerData);
            // Debug.Log($"{GetType()}::{MethodBase.GetCurrentMethod().Name}");
            
            _audioSource = playerData as AudioSource;
            if (!_audioSource) return;
            
            if (_isRunning)
            {
                PlayAudio(playable);
            }
        }

        private void PlayAudio(Playable playable)
        {
            if (_audioSource == null) return;
            if (localizedAudioClip == null || localizedAudioClip.IsEmpty) return;

            // Timeline Editor checks for pausing the audio if needed
            // Checks previous and next frame as well as random clicks
            if (!Application.isPlaying && 
                (Math.Abs(playable.GetTime() - playable.GetPreviousTime()) <= 1.0f / 60f || // Next frame
                Math.Abs(playable.GetTime() - playable.GetPreviousTime()) > 0.1 ))
            {
                _audioSource.Pause();
                return;
            }
            
            if (_isAudioClipPlaying) return;
            
            var item = localizedAudioClip.LoadAsset();
            if (item == null) return;

            _audioSource.clip = item.audioClip;

            var clipLength = (double)_audioSource.clip.samples / _audioSource.clip.frequency;
            if (playable.GetTime() <= clipLength)
            {
                _audioSource.time = (float)playable.GetTime();
                _audioSource.Play();
            }
            else
            {
                _audioSource.Pause();
            }

            _isAudioClipPlaying = true;
        }
        
        private void PauseAudio()
        {
            if (_audioSource == null) return;
            _audioSource.Pause();
            _isAudioClipPlaying = false;
        }
        
        private void StopAudio()
        {
            if (_audioSource == null) return;
            _audioSource.Stop();
            _isAudioClipPlaying = false;
        }
    }
#endif
}
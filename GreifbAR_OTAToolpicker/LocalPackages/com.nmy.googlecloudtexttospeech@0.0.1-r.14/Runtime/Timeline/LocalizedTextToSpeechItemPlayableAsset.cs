using System;
using UnityEngine;

#if NMY_ENABLE_TIMELINE
using UnityEngine.Playables;
using UnityEngine.Timeline;
#endif

namespace NMY.GoogleCloudTextToSpeech
{
#if NMY_ENABLE_TIMELINE
    [Serializable]
    public class LocalizedTextToSpeechItemPlayableAsset : PlayableAsset, ITimelineClipAsset
    {
        [SerializeField] private LocalizedTextToSpeechAudioClip _localizedAudioClip;

        private AudioClip _audioClip;
        
        private AudioClip audioClip
        {
            get
            {
                if (_localizedAudioClip.IsEmpty) return null;
                var item = _localizedAudioClip.LoadAsset();

                if (item != null)
                {
                    if (_audioClip != item.audioClip)
                    {
                        _audioClip = item.audioClip;
                        Debug.Log($"{GetType()}: {_audioClip.name} {_audioClip.length}", this);
                    }
                    return _audioClip;
                }
                
                return null;
            }
        }

        /// <summary>
        /// Returns the duration required to play the audio clip exactly once
        /// </summary>
        public override double duration
        {
            get
            {
                if (audioClip == null) return base.duration;

                // use this instead of length to avoid rounding precision errors,
                return (double)audioClip.samples / audioClip.frequency;
            }
        }
        

        public override Playable CreatePlayable(PlayableGraph graph, GameObject owner)
        {
            var playable  = ScriptPlayable<LocalizedTextToSpeechItemPlayableBehaviour>.Create(graph);
            var behaviour = playable.GetBehaviour();
            behaviour.localizedAudioClip = _localizedAudioClip;
            
            return playable;
        }
        
        public ClipCaps clipCaps => ClipCaps.None;
    }
#endif
}

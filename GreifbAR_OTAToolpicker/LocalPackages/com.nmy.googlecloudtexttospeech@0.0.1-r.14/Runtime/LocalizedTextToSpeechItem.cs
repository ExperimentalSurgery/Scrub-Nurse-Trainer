using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Localization;

namespace NMY.GoogleCloudTextToSpeech
{
    [CreateAssetMenu(fileName = "LocalizedTextToSpeechItem", menuName = "TTS/LocalizedTextToSpeechItem", order = 0)]
    public class LocalizedTextToSpeechItem : ScriptableObject
    {
        [SerializeField] private AudioClip _audioClip;
        [SerializeField] private TextAsset _timestampAsset;
        [SerializeField] private List<TextToSpeechTimestampEntry> _timestamps = new();

        public float GetDuration() => _audioClip == null ? 0 : audioClip.length; // cannot use "is null" here, due to Unity's operator== overload

        public AudioClip audioClip  => _audioClip;
        public TextAsset timestampAsset  => _timestampAsset;
        public List<TextToSpeechTimestampEntry> timestamps  => _timestamps;
        
        public static LocalizedTextToSpeechItem Create(AudioClip audioClip, TextAsset timestampAsset)
        {
            var data = CreateInstance<LocalizedTextToSpeechItem>();
            data._audioClip = audioClip;
            data._timestampAsset = timestampAsset;
            data._timestamps = JsonConvert.DeserializeObject<List<TextToSpeechTimestampEntry>>(timestampAsset.text);

            return data;
        }
    }
    
    [Serializable]
    public class TextToSpeechTimestampEntry
    {
        public string markName;
        public float  timeSeconds;
    }
    
    [Serializable]
    public class LocalizedTextToSpeechAudioClip : LocalizedAsset<LocalizedTextToSpeechItem> { }
}

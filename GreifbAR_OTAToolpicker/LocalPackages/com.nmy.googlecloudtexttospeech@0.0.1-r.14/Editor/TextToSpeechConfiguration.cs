using System;
using Google.Cloud.TextToSpeech.V1Beta1;
using UnityEngine;
using UnityEngine.Localization;

namespace NMY.GoogleCloudTextToSpeech
{
    [CreateAssetMenu(fileName = "TtsConfiguration", menuName = "TTS/TextToSpeechConfiguration", order = 0)]
    public class TextToSpeechConfiguration : ScriptableObject
    {
        public Locale locale;

        [Header("Voice Selection")]
        [SerializeField] private string _voiceName;
        [SerializeField] private string _languageCode;
        [SerializeField] private SsmlVoiceGender _voiceGender = SsmlVoiceGender.Female;
        
        [Header("Audio Config")]
        [SerializeField] private AudioEncoding _encoding = AudioEncoding.Linear16;

        [SerializeField] [Range(-20.0f, 20.0f)] private double _pitch        = 0.0f;
        [SerializeField] [Range(0.25f, 4.0f)]   private double _speakingRate = 1.0f;
        [SerializeField] [Range(-96.0f, 16.0f)] private double _volumeGainDb = 0;
        
        [Header("Timepoint Config")]
        [SerializeField] private SynthesizeSpeechRequest.Types.TimepointType _timepoint = SynthesizeSpeechRequest.Types.TimepointType.SsmlMark;

        public SynthesizeSpeechRequest.Types.TimepointType timepoint => _timepoint;


        public VoiceSelectionParams GetVoiceSelectionParams()
        {
            if (!string.IsNullOrEmpty(_voiceName))
            {
                return new VoiceSelectionParams
                {
                    Name = _voiceName,
                    LanguageCode = _languageCode
                };
            }

            return new VoiceSelectionParams
            {
                LanguageCode = _languageCode,
                SsmlGender   = _voiceGender,
            };
        }

        public AudioConfig GetAudioConfig()
        {
            return new AudioConfig
            {
                AudioEncoding = _encoding,
                Pitch = _pitch,
                SpeakingRate = _speakingRate,
                VolumeGainDb = _volumeGainDb
            };
        }

        public string GetAudioExtension()
        {
            switch (_encoding)
            {
                case AudioEncoding.Linear16:
                case AudioEncoding.Mulaw:
                case AudioEncoding.Alaw:
                    return ".wav";
                case AudioEncoding.Mp3:
                case AudioEncoding.Mp364Kbps:
                    return ".mp3";
                case AudioEncoding.OggOpus:
                    return ".ogg";
                case AudioEncoding.Unspecified:
                default:
                    throw new Exception("Unspecified or unsupported encoding type!");
            }
        }
    }
}
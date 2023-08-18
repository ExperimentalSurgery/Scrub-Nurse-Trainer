using System;
using UnityEngine;
using UnityEngine.Localization.Metadata;

namespace NMY.GoogleCloudTextToSpeech
{
    [Serializable]
    public class LocalizedTextToSpeechMetadata : IMetadata
    {
        public string created;
        public string lastModified;
        
        [TextArea(5, 10)]
        public string spokenText;
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Localization;

namespace NMY.OTAToolpicker
{
    [System.Serializable]
    public struct RuleData
    {
        [Tooltip("If the rule is not enabled it will be skipped in the level 2 flow.")]
        public bool isEnabled;
        public List<InstrumentData> instruments;
        public AudioClip audioClip;
        public AudioClip successAudioClip;
        public LocalizedString dialogTitle;
        public LocalizedString dialogMessage;
        public LocalizedString successText;
        public bool isDirectionCheckEnabled;
        public AudioClip directionCheckAudioClip;
        public LocalizedString directionCheckText;
    }
}

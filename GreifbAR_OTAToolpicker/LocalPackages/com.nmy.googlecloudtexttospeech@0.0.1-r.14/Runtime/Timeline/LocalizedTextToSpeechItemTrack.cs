using UnityEngine;

#if NMY_ENABLE_TIMELINE
using UnityEngine.Timeline;
#endif

namespace NMY.GoogleCloudTextToSpeech
{
#if NMY_ENABLE_TIMELINE
    [TrackColor(0,0,0)]
    [TrackBindingType(typeof(AudioSource))]
    [TrackClipType(typeof(LocalizedTextToSpeechItemPlayableAsset))]
    public class LocalizedTextToSpeechItemTrack : TrackAsset
    {

    }
#endif
}

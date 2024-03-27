using System.Collections;
using System.Collections.Generic;
using Leap.Unity;
using UnityEngine;

namespace NMY.OTAToolpicker
{
    [CreateAssetMenu(fileName = "HandoverData", menuName = "OTA/HandoverData", order = 0)]
    public class HandoverData : ScriptableObject
    {
        [SerializeField] private HandPoseScriptableObject handPose;
        public HandPoseScriptableObject HandPose => handPose;

        [SerializeField] private Chirality chirality = Chirality.Right;
        public Chirality Chirality => chirality;

        [SerializeField] private Sprite exampleImage;
        public Sprite ExampleImage => exampleImage;
    }
}

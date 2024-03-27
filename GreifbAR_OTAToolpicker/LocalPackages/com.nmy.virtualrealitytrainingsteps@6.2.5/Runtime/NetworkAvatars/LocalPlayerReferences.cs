using System;
using UnityEngine;

namespace NMY.VirtualRealityTraining.NetworkAvatars
{
    /// <summary>
    /// This class represents the local player's rig in a network environment.
    /// </summary>
    [Serializable]
    public class LocalPlayerReferences
    {
        /// <summary>
        /// The root transform of the rig.
        /// </summary>
        public Transform root;
            
        /// <summary>
        /// The transform for the head of the rig.
        /// </summary>
        public Transform head;
            
        /// <summary>
        /// The transform for the left hand of the rig.
        /// </summary>
        public Transform leftHand;
            
        /// <summary>
        /// The transform for the right hand of the rig.
        /// </summary>
        public Transform rightHand;
    }
}
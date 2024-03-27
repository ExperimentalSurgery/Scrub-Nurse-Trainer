using UnityEngine;

namespace NMY.VirtualRealityTraining.NetworkAvatars
{
    [CreateAssetMenu(fileName = "NetworkAvatarOffset",
                     menuName = "NMY/VirtualRealityTraining/NetworkAvatars/NetworkAvatarOffsets", order = 0)]
    public class NetworkAvatarOffsetScriptableObject : ScriptableObject
    {
       
        /// <summary>
        /// A Vector3 that stores an offset value that is applied to the position of the avatar's head transform.
        /// This offset can be used to fine-tune the position of the head relative to its default value.
        /// </summary>
        [Header("Offsets")]
        [SerializeField] private Vector3 _headPositionOffset;

        /// <summary>
        /// A Vector3 that stores an offset value that is applied to the rotation of the avatar's head transform.
        /// This offset can be used to fine-tune the rotation of the head relative to its default value.
        /// </summary>
        [SerializeField] private Vector3 _headRotationOffset;

        /// <summary>
        /// A Vector3 that stores an offset value that is applied to the position of the avatar's neck transform.
        /// This offset can be used to fine-tune the position of the neck relative to its default value.
        /// </summary>
        [SerializeField] private Vector3 _neckPositionOffset;

        /// <summary>
        /// A Vector3 that stores an offset value that is applied to the rotation of the avatar's neck transform.
        /// This offset can be used to fine-tune the rotation of the neck relative to its default value.
        /// </summary>
        [SerializeField] private Vector3 _neckRotationOffset;

        /// <summary>
        /// A Vector3 that stores an offset value that is applied to the position of the avatar's left hand transform.
        /// This offset can be used to fine-tune the position of the left hand relative to its default value.
        /// </summary>
        [SerializeField] private Vector3 _leftHandPositionOffset;

        /// <summary>
        /// A Vector3 that stores an offset value that is applied to the rotation of the avatar's left hand transform.
        /// This offset can be used to fine-tune the rotation of the left hand relative to its default value.
        /// </summary>
        [SerializeField] private Vector3 _leftHandRotationOffset;

        /// <summary>
        /// A Vector3 that stores an offset value that is applied to the position of the avatar's right hand transform.
        /// This offset can be used to fine-tune the position of the right hand relative to its default value.
        /// </summary>
        [SerializeField] private Vector3 _rightHandPositionOffset;

        /// <summary>
        /// A Vector3 that stores an offset value that is applied to the rotation of the avatar's right hand transform.
        /// This offset can be used to fine-tune the rotation of the right hand relative to its default value.
        /// </summary>
        [SerializeField] private Vector3 _rightHandRotationOffset;

        public Vector3 headPositionOffset => _headPositionOffset;

        public Vector3 headRotationOffset => _headRotationOffset;

        public Vector3 neckPositionOffset => _neckPositionOffset;

        public Vector3 neckRotationOffset => _neckRotationOffset;

        public Vector3 leftHandPositionOffset => _leftHandPositionOffset;

        public Vector3 leftHandRotationOffset => _leftHandRotationOffset;

        public Vector3 rightHandPositionOffset => _rightHandPositionOffset;

        public Vector3 rightHandRotationOffset => _rightHandRotationOffset;
    }
}
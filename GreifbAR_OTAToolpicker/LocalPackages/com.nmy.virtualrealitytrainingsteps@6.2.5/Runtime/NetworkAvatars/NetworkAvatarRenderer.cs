using System;
using System.Collections.Generic;
using TMPro;
using Unity.Multiplayer.Samples.Utilities.ClientAuthority;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.XR;

namespace NMY.VirtualRealityTraining.NetworkAvatars
{
    /// <summary>
    /// This class represents one specific avatar that is managed by <see cref="NetworkAvatar"/>, and it is
    /// responsible for updating the transforms of the networked avatar.
    /// </summary>
    public class NetworkAvatarRenderer : MonoBehaviour
    {
        /// <summary>
        /// A static readonly integer field that stores the hash code of the "TriggerButtonL" animation parameter.
        /// This parameter is used to control the avatar's left trigger button animation.
        /// </summary>
        private static readonly int LeftTriggerAnimParameter  = Animator.StringToHash("TriggerButtonL");
        
        /// <summary>
        /// A static readonly integer field that stores the hash code of the "GripButtonL" animation parameter.
        /// This parameter is used to control the avatar's left grip button animation.
        /// </summary>
        private static readonly int LeftGripAnimParameter     = Animator.StringToHash("GripButtonL");
        
        /// <summary>
        /// A parameter hash id for the right trigger animation.
        /// </summary>
        private static readonly int RightTriggerAnimParameter = Animator.StringToHash("TriggerButtonR");
        
        /// <summary>
        /// A static readonly integer field that stores the hash code of the "TriggerButtonR" animation parameter.
        /// This parameter is used to control the avatar's right trigger button animation.
        /// </summary>
        private static readonly int RightGripAnimParameter    = Animator.StringToHash("GripButtonR");

        /// <summary>
        /// A list of all <see cref="Renderer"/> components that belongs to this avatar.
        /// </summary>
        [SerializeField] private List<Renderer> _avatarRenderers;

        /// <summary>
        /// The transform that represents the root GameObject of the avatar.
        /// </summary>
        [SerializeField] private Transform _root;
        
        /// <summary>
        /// The transform that represents the neck GameObject of this avatar.
        /// </summary>
        [SerializeField] private Transform _neck;
        
        /// <summary>
        /// The transform that represents the head GameObject of this avatar.
        /// </summary>
        [SerializeField] private Transform _head;
        
        /// <summary>
        /// The transform that represents the left hand GameObject of this avatar.
        /// </summary>
        [SerializeField] private Transform _leftHand;
        
        /// <summary>
        /// The transform that represents the right hand GameObject of this avatar.
        /// </summary>
        [SerializeField] private Transform _rightHand;
        
        [Header("Offsets")]
        [SerializeField] private NetworkAvatarOffsetScriptableObject _offsets;

        /// <summary>
        /// A reference to the root object of the avatar name.
        /// </summary>
        [Header("UI")]
        [SerializeField] private GameObject _displayNameRoot;

        /// <summary>
        /// A text reference that is used to display the avatar's name.
        /// </summary>
        [SerializeField] private TMP_Text _displayNameText;

        /// <summary>
        /// A reference to the <see cref="Animator"/> that is used to control the avatar's hand animations.
        /// If this field is not null, the avatar's hand animations will be enabled.
        /// </summary>
        [Header("Animations")]
        [SerializeField] private Animator _handAnimator;

        /// <summary>
        /// A static readonly List of XRNodeState objects that is used to store the current state of the VR device's XR nodes.
        /// The list is cleared by the GetNodeStates method before it is populated with the current node states.
        /// </summary>
        private static readonly List<XRNodeState> NodeStates = new();

        public Transform root => _root;

        public Transform neck => _neck;

        public Transform head => _head;

        public Transform leftHand => _leftHand;

        public Transform rightHand => _rightHand;

        /// <summary>
        /// A property that returns a boolean value indicating whether the avatar's hand animations are enabled.
        /// The hand animations are enabled if the <see cref="_handAnimator"/> field is not null.
        /// </summary>
        public bool useHandAnimation => _handAnimator != null;

        public NetworkAvatarOffsetScriptableObject offsets
        {
            get => _offsets;
            set => _offsets = value;
        }
        
        

        private void Awake()
        {
            if (_avatarRenderers.Count == 0) _avatarRenderers.AddRange(GetComponentsInChildren<Renderer>());

            CheckClientNetworkTransform(_neck);
            CheckClientNetworkTransform(_head);
            CheckClientNetworkTransform(_leftHand);
            CheckClientNetworkTransform(_rightHand);
        }

        /// <summary>
        /// Checks the specified GameObject to see if it contains a <see cref="ClientNetworkTransform"/>.
        /// If not, it adds the component and sets the scale synchronization to false for all axes.
        /// </summary>
        /// <param name="obj">A GameObject to be checked if it contains a <see cref="ClientNetworkTransform"/>.</param>
        private static void CheckClientNetworkTransform(Transform obj)
        {
            if (obj == null) return;
            if (obj.TryGetComponent(out ClientNetworkTransform clientNetworkTransform)) return;
            
            clientNetworkTransform            = obj.gameObject.AddComponent<ClientNetworkTransform>();
            clientNetworkTransform.SyncScaleX = false;
            clientNetworkTransform.SyncScaleY = false;
            clientNetworkTransform.SyncScaleZ = false;
        }


        /// <summary>
        /// Updates the avatar's transforms and animations based on data received from a <see cref="NetworkAvatar"/>.
        /// The method updates the avatar's root, head, left hand, and right hand transforms using the data from the <see cref="localPlayerReferences"/> parameter.
        /// If the head, left hand, or right hand transforms could not be updated, the method attempts to update them using the XRNode APIs.
        /// </summary>
        /// <param name="localPlayerReferences">A <see cref="LocalPlayerReferences"/> object that stores data about the avatar's transforms and animations.</param>
        public void UpdateAvatar(LocalPlayerReferences localPlayerReferences)
        {
            // Flags to fetch XRNode position/rotation state
            UpdateRoot(localPlayerReferences.root);
            var updateHeadWithXRNode      = !UpdateHead(localPlayerReferences.head);
            var updateLeftHandWithXRNode  = !UpdateLeftHand(localPlayerReferences.leftHand);
            var updateRightHandWithXRNode = !UpdateRightHand(localPlayerReferences.rightHand);

            // Update head/hands using XRNode APIs if needed
            if (updateHeadWithXRNode || updateLeftHandWithXRNode || updateRightHandWithXRNode)
            {
                InputTracking.GetNodeStates(NodeStates); // the list is cleared by GetNodeStates

                foreach (var nodeState in NodeStates)
                {
                    switch (nodeState.nodeType)
                    {
                        case XRNode.Head when updateHeadWithXRNode:
                            UpdateTransformWithNodeState(_head, nodeState);
                            break;
                        case XRNode.LeftHand when updateLeftHandWithXRNode:
                            UpdateTransformWithNodeState(_leftHand, nodeState);
                            break;
                        case XRNode.RightHand when updateRightHandWithXRNode:
                            UpdateTransformWithNodeState(_rightHand, nodeState);
                            break;
                    }
                }
            }
        }

        /// <summary>
        /// Updates the specified transform using the data from the XRNodeState parameter.
        /// The method attempts to retrieve the position and rotation data from the XRNodeState and applies it to the transform.
        /// </summary>
        /// <param name="t">The transform to be updated.</param>
        /// <param name="state">The XRNodeState object that stores the position and rotation data.</param>
        private void UpdateTransformWithNodeState(Transform t, XRNodeState state)
        {
            if (state.TryGetPosition(out var position))
            {
                t.localPosition = position;
            }

            if (state.TryGetRotation(out var rotation))
            {
                t.localRotation = rotation;
            }
        }

        /// <summary>
        /// Updates the avatar's root transform.
        /// The method returns a boolean value indicating whether the root transform was updated.
        /// </summary>
        /// <param name="rootTransform">The Transform object that stores the position and rotation data to be applied to the avatar's root transform.</param>
        /// <returns>True if the root transform was updated, false otherwise.</returns>
        private bool UpdateRoot(Transform rootTransform)
        {
            if (_root == null) return false;
            _root.SetPositionAndRotation(rootTransform.position, rootTransform.rotation);
            return true;
        }

        /// <summary>
        /// Updates the avatar's head and neck transforms.
         /// </summary>
        /// <remarks>
        /// It is applying the <see cref="_headPositionOffset"/> and <see cref="_headRotationOffset"/> values if necessary.
        /// The method returns a boolean value indicating whether the head and neck transforms were updated.
        /// </remarks>
        /// <param name="headTransform">The Transform object that stores the position and rotation data to be applied to the avatar's head and neck transforms.</param>
        /// <returns>True if the head and neck transforms were updated, false otherwise.</returns>
        private bool UpdateHead(Transform headTransform)
        {
            if (_head == null) return false;

            var eulers     = headTransform.localRotation.eulerAngles;
            var yRot       = Quaternion.Euler(0, eulers.y, 0);
            var xzRotation = Quaternion.Euler(eulers.x, 0, eulers.z);
            
            _head.position = headTransform.position + _head.TransformVector(_offsets.headPositionOffset);
            _head.rotation = _head.parent.transform.rotation * xzRotation * Quaternion.Euler(_offsets.headRotationOffset);

            if (_neck != null)
            {
                _neck.position = _head.position + _neck.TransformVector(_offsets.neckPositionOffset);
                _neck.rotation = _neck.parent.transform.rotation * yRot;
            }

            return true;
        }

        /// <summary>
        /// Updates the avatar's left hand transform.
        /// </summary>
        /// <remarks>
        /// It is applying the <see cref="_leftHandPositionOffset"/> and <see cref="_leftHandRotationOffset"/> values if necessary.
        /// The method returns a boolean value indicating whether the left hand transform was updated.
        /// </remarks>
        /// <param name="leftHandTransform">The Transform object that stores the position and rotation data to be applied to the avatar's left hand transform.</param>
        /// <returns>True if the left hand transform was updated, false otherwise.</returns>
        private bool UpdateLeftHand(Transform leftHandTransform)
        {
            if (_leftHand == null || leftHandTransform == null) return false;

            _leftHand.position = leftHandTransform.position +
                                 _leftHand.TransformVector(_offsets.leftHandPositionOffset);

            _leftHand.rotation = leftHandTransform.parent.transform.rotation *
                leftHandTransform.localRotation * Quaternion.Euler(_offsets.leftHandRotationOffset);

            return true;
        }

        /// <summary>
        /// Updates the avatar's right hand transform.
        /// </summary>
        /// <remarks>
        /// It is applying the <see cref="_rightHandPositionOffset"/> and <see cref="_rightHandRotationOffset"/> values if necessary.
        /// The method returns a boolean value indicating whether the right hand transform was updated.
        /// </remarks>
        /// <param name="rightHandTransform">The Transform object that stores the position and rotation data to be applied to the avatar's right hand transform.</param>
        /// <returns>True if the right hand transform was updated, false otherwise.</returns>
        private bool UpdateRightHand(Transform rightHandTransform)
        {
            if (_rightHand == null || rightHandTransform == null) return false;
            _rightHand.position = rightHandTransform.position +
                                  _rightHand.TransformVector(_offsets.rightHandPositionOffset);

            _rightHand.rotation = rightHandTransform.parent.transform.rotation *
                rightHandTransform.localRotation * Quaternion.Euler(_offsets.rightHandRotationOffset);

            return true;
        }

        /// <summary>
        /// Sets the visibility of the right hand according of the tracking state
        /// </summary>
        public void SetRightHandVisibility(bool newValue)
        {
            if (_rightHand == null) return;
            _rightHand.localScale = newValue ? Vector3.one : Vector3.zero;
        }

        /// <summary>
        /// Sets the visibility of the right hand according of the tracking state
        /// </summary>
        public void SetLeftHandVisibility(bool newValue)
        {
            if (_leftHand == null) return;
            _leftHand.localScale = newValue ? Vector3.one : Vector3.zero;
        }

        /// <summary>
        /// Hides the avatar.
        /// </summary>
        /// <remarks>
        /// The method disables the rendering of the avatar's renderers and, if the <see cref="_displayNameText"/> field is not null,
        /// disables the game object containing the display name text.
        /// </remarks>
        public void HideAvatar()
        {
            foreach (var t in _avatarRenderers)
            {
                t.enabled = false;
            }
            
            if (_displayNameRoot != null)
                _displayNameRoot.SetActive(false);
        }

        /// <summary>
        /// Shows the avatar.
        /// </summary>
        /// <remarks>
        /// The method enables the rendering of the avatar's renderers and, if the _displayNameText field is not null,
        /// enables the game object containing the display name text.
        /// </remarks>
        public void ShowAvatar()
        {
            foreach (var t in _avatarRenderers)
            {
                t.enabled = true;
            }
            
            if (_displayNameRoot != null)
                _displayNameRoot.SetActive(true);
        }
        
        /// <summary>
        /// Updates the display name of the avatar.
        /// </summary>
        /// <remarks>
        /// If the <see cref="_displayNameText"/> field is null, the method returns without modifying the display name.
        /// Otherwise, the method sets the text of the display name text object to the value of the string parameter.
        /// </remarks>
        /// <param name="newValue">The new value for the display name of the avatar.</param>
        public void SetDisplayName(string newValue)
        {
            if (_displayNameText == null) return;
            _displayNameText.text = newValue;
        }

        /// <summary>
        /// Sets the animation state of the avatar's left trigger using the float parameter value.
        /// </summary>
        /// <remarks>
        /// The method uses the <see cref="_handAnimator"/> field and the <see cref="LeftTriggerAnimParameter"/> static field to set the animation state.
        /// </remarks>
        /// <param name="value">The value to set for the left trigger animation state.</param>
        public void SetLeftTriggerAnimation(float value)
        {
            if (!useHandAnimation) return;
            _handAnimator.SetFloat(LeftTriggerAnimParameter, value);
        }

        /// <summary>
        /// Sets the animation state of the avatar's left grip using the float parameter value.
        /// </summary>
        /// <remarks>
        /// The method uses the <see cref="_handAnimator"/> field and the <see cref="LeftGripAnimParameter"/> static field to set the animation state.
        /// </remarks>
        /// <param name="value">The value to set for the left grip animation state.</param>
        public void SetLeftGripAnimation(float value)
        {
            if (!useHandAnimation) return;
            _handAnimator.SetFloat(LeftGripAnimParameter, value);
        }

        /// <summary>
        /// Sets the animation state of the avatar's right trigger using the float parameter value.
        /// </summary>
        /// <remarks>
        /// The method uses the <see cref="_handAnimator"/> field and the <see cref="RightTriggerAnimParameter"/> static field to set the animation state.
        /// </remarks>
        /// <param name="value">The value to set for the right trigger animation state.</param>
        public void SetRightTriggerAnimation(float value)
        {
            if (!useHandAnimation) return;
            _handAnimator.SetFloat(RightTriggerAnimParameter, value);
        }

        /// <summary>
        /// Sets the animation state of the avatar's right grip using the float parameter value.
        /// </summary>
        /// <remarks>
        /// The method uses the <see cref="_handAnimator"/> field and the <see cref="RightGripAnimParameter"/> static field to set the animation state.
        /// </remarks>
        /// <param name="value">The value to set for the right grip animation state.</param>
        public void SetRightGripAnimation(float value)
        {
            if (!useHandAnimation) return;
            _handAnimator.SetFloat(RightGripAnimParameter, value);
        }
    }
}
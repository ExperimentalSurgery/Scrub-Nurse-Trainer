using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;
#if NMY_ENABLE_UNITY_ATOMS
using UnityAtoms.BaseAtoms;
#endif

namespace NMY.VirtualRealityTraining.NetworkAvatars
{
    /// <summary>
    /// This class represents an avatar in a network environment.
    /// It is responsible for synchronizing the state of the avatar over the network,
    /// allowing multiple players to see and interact with the same avatar in a shared virtual environment.
    /// It is also responsible for updating the avatar's animation and appearance based on input from the
    /// player's controllers.
    /// </summary>
    public class NetworkAvatar : NetworkBehaviour
    {
        /// <summary>
        /// A boolean value indicating whether the avatar should be hidden when it is controlled by the local player.
        /// </summary>
        [Header("Avatar Setup")]
        [SerializeField] private bool _hideLocalAvatar;

        /// <summary>
        /// A list of <see cref="NetworkAvatarRenderer"/> objects that represent different avatars that the player can choose from.
        /// </summary>
        [SerializeField] private List<NetworkAvatarRenderer> _avatars = new();
        
        /// <summary>
        /// The currently selected avatar.
        /// </summary>
        [SerializeField] private NetworkAvatarRenderer _currentAvatar;

#if NMY_ENABLE_UNITY_ATOMS
        /// <summary>
        /// The UnityAtoms variable representing the current avatar. If this variable changes, the avatar will also change.
        /// </summary>
        /// <remarks>
        /// This field is only visible if `UnityAtoms` is installed in the project.
        /// </remarks>
        [SerializeField] private IntVariable _atomAvatarID;
#endif

        /// <summary>
        /// A list of strings that contain the names of the avatars.
        /// </summary>
        [SerializeField] private List<string> _nameList;

        /// <summary>
        /// An InputActionReference reference that defines the input action for the left trigger on the player's controllers.
        /// </summary>
        [Header("Hand Animation References")]
        [SerializeField] private InputActionReference _leftTriggerAction;
        
        /// <summary>
        /// An InputActionReference reference that defines the input action for the left grip on the player's controllers.
        /// </summary>
        [SerializeField] private InputActionReference _leftGripAction;
        
        /// <summary>
        /// An InputActionReference reference that defines the input action for the right trigger on the player's controllers.
        /// </summary>
        [SerializeField] private InputActionReference _rightTriggerAction;
        
        /// <summary>
        /// An InputActionReference reference that defines the input action for the right grip on the player's controllers.
        /// </summary>
        [SerializeField] private InputActionReference _rightGripAction;

        /// <summary>
        /// The local player's rig.
        /// </summary>
        public LocalPlayerReferences localPlayerReferences
        {
            get => _localPlayerReferences;
            set => SetLocalPlayer(value);
        }

        /// <summary>
        /// The currently selected avatar.
        /// </summary>
        public NetworkAvatarRenderer currentAvatar
        {
            get => _currentAvatar;
            set
            {
                // Check if the new avatar is part of the available avatars
                if (!_avatars.Contains(value)) return;
                
                if (_currentAvatar != value)
                {
                    _currentAvatar.HideAvatar();
                }
                _currentAvatar = value;

                // The owner sets the ID for all remote clients so that they update this representation if needed
                if (IsOwner) _networkAvatarID.Value = _avatars.IndexOf(value);

                if (_currentAvatar != null)
                {
                    _currentAvatar.SetDisplayName(displayName);

                    if (IsOwner)
                    {
                        if (!_hideLocalAvatar) _currentAvatar.ShowAvatar();
                    }
                    else
                    {
                        _currentAvatar.ShowAvatar();
                    }
                }
                
                if (IsOwner) LocalAvatarChanged?.Invoke(NetworkManager.LocalClientId, _currentAvatar);
                else RemoteAvatarChanged?.Invoke(NetworkManager.LocalClientId, currentAvatar);
            }
        }

        public event Action<ulong, NetworkAvatarRenderer> LocalAvatarChanged;
        public event Action<ulong, NetworkAvatarRenderer> RemoteAvatarChanged;
        public event Action<string> LocalAvatarNameChanged;
        

        /// <summary>
        /// The local player's rig.
        /// </summary>
        private LocalPlayerReferences _localPlayerReferences;

        /// <summary>
        /// The <see cref="NetworkAvatarManager"/> object that connects the rig in the scene with the avatar.
        /// </summary>
        private NetworkAvatarManager _networkAvatarManager;

        /// <summary>
        /// A NetworkVariable that stores the display name of the avatar. It will the shown to each client.
        /// </summary>
        private NetworkVariable<ForceNetworkSerializeByMemcpy<FixedString64Bytes>> _networkDisplayName =
            new(writePerm: NetworkVariableWritePermission.Owner);

        
        /// <summary>
        /// The name of the avatar.
        /// </summary>
        public string displayName => _networkDisplayName.Value.Value.Value;

        /// <summary>
        /// A NetworkVariable that stores the value of the left grip input on the player's controllers.
        /// </summary>
        private NetworkVariable<float> _leftGripValue     = new(writePerm: NetworkVariableWritePermission.Owner);
        
        /// <summary>
        /// A NetworkVariable that stores the value of the left trigger input on the player's controllers.
        /// </summary>
        private NetworkVariable<float> _leftTriggerValue  = new(writePerm: NetworkVariableWritePermission.Owner);
        
        /// <summary>
        /// A NetworkVariable that stores the value of the right grip input on the player's controllers.
        /// </summary>
        private NetworkVariable<float> _rightGripValue    = new(writePerm: NetworkVariableWritePermission.Owner);
        
        /// <summary>
        /// A NetworkVariable that stores the value of the right trigger input on the player's controllers.
        /// </summary>
        private NetworkVariable<float> _rightTriggerValue = new(writePerm: NetworkVariableWritePermission.Owner);

        /// <summary>
        /// A NetworkVariable that stores the ID of the currently selected avatar.
        /// </summary>
        private NetworkVariable<int> _networkAvatarID = new(writePerm: NetworkVariableWritePermission.Owner);

        /// <summary>
        /// A NetworkVariable that stores the tracked state of the left controller / hand
        /// </summary>
        private NetworkVariable<bool> _isLeftHandTracked = new (writePerm: NetworkVariableWritePermission.Owner);

        /// <summary>
        /// A NetworkVariable that stores the tracked state of the right controller / hand
        /// </summary>
        private NetworkVariable<bool> _isRightHandTracked = new (writePerm: NetworkVariableWritePermission.Owner);
        
        
        /// <summary>
        /// Overrides the <see cref="NetworkBehaviour.OnNetworkSpawn"/> method of the <see cref="NetworkBehaviour"/> class to
        /// find the <see cref="_networkAvatarManager"/> in the scene and to set up the event handlers for the network variables.
        /// </summary>
        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            // Register with NetworkAvatarManager
            FindAndRegisterToNetworkAvatarManager();

            // Set up event handlers for network variables
            _networkDisplayName.OnValueChanged += OnNetworkDisplayNameChanged;
            _leftGripValue.OnValueChanged      += OnLeftGripValueChanged;
            _leftTriggerValue.OnValueChanged   += OnLeftTriggerValueChanged;
            _isLeftHandTracked.OnValueChanged  += OnLeftHandTrackedChanged;
            _rightGripValue.OnValueChanged     += OnRightGripValueChanged;
            _rightTriggerValue.OnValueChanged  += OnRightTriggerValueChanged;
            _isRightHandTracked.OnValueChanged += OnRightHandTrackedChanged;

            InitializeAvatar();
        }

        /// <summary>
        /// Initializes the avatar. If <see cref="_atomAvatarID"/> is set, use this ID for the initial avatar.
        /// Otherwise, use the first avatar in the list. It also sets the name of the avatar based in the avatar id.
        /// </summary>
        private void InitializeAvatar()
        {
            // Set Avatar
            foreach (var avatar in _avatars) avatar.HideAvatar();

            _networkAvatarID.OnValueChanged += OnNetworkAvatarIdChanged;
#if NMY_ENABLE_UNITY_ATOMS
            if (IsOwner)
            {
                if (_atomAvatarID != null)
                {
                    _atomAvatarID.Changed.Register(OnAtomAvatarIdChanged);
                    _networkAvatarID.Value = _atomAvatarID.Value;
                }
            }
#endif
            if (IsOwner) _networkAvatarID.Value = _avatars.IndexOf(_currentAvatar);
            UpdateAvatar();
            
            // Set User Name
            if (IsOwner)
                SetUserName(GetUserNameFromList());
            else
            {
                currentAvatar.SetDisplayName(displayName);
            }

            if (IsOwner && !_hideLocalAvatar)
            {
                currentAvatar.ShowAvatar();
            }
        }

        /// <summary>
        /// Returns the name of the avatar based on the <see cref="NetworkBehaviour.OwnerClientId"/> of the class <see cref="NetworkBehaviour"/>.
        /// </summary>
        /// <returns>The name of the owning client represented in the NameList</returns>
        private string GetUserNameFromList() => _nameList[(int)(OwnerClientId % (ulong)_nameList.Count)];

        public void SetUserName(string newName)
        {
            if (!IsOwner) return;
            _networkDisplayName.Value = new ForceNetworkSerializeByMemcpy<FixedString64Bytes>($"{newName}");
            LocalAvatarNameChanged?.Invoke(newName);
        }
        
        /// <summary>
        /// Updates the <see cref="currentAvatar"/>. If we don't have an ID and current avatar yet,
        /// use the first avatar in the <see cref="_avatars"/> list.
        /// </summary>
        private void UpdateAvatar()
        {
            // Set the currentAvatar to the first element in the List if not manually set
            if (_networkAvatarID.Value == -1 && _avatars.Count > 0 && currentAvatar != null)
            {
                currentAvatar = _avatars[0];
                return;
            }
            
            // Make sure that we have no overflow with the ids
            var id = _networkAvatarID.Value % _avatars.Count;
            
            currentAvatar = _avatars[id];
        }

        /// <summary>
        /// The event handler for the <see cref="NetworkVariable{T}.OnValueChanged"/> event of the  <see cref="NetworkVariable{T}"/> class
        /// for the <see cref="_networkAvatarID"/> variable to update the avatar if the ID of the avatar changed.
        /// </summary>
        /// <param name="previousValue">The previous ID of the avatar.</param>
        /// <param name="newValue">The new ID of the avatar.</param>
        private void OnNetworkAvatarIdChanged(int previousValue, int newValue)
        {
            UpdateAvatar();
        }
        
#if NMY_ENABLE_UNITY_ATOMS
        /// <summary>
        /// The event handler for the <see cref="IntVariable.Changed"/> event of the <see cref="IntVariable"/> class
        /// for the <see cref="_atomAvatarID"/> variable to update the value of <see cref="_networkAvatarID"/>.
        /// </summary>
        /// <param name="value">The new ID for the avatar.</param>
        private void OnAtomAvatarIdChanged(int value)
        {
            _networkAvatarID.Value = value;
        }
#endif

        /// <summary>
        /// Searches for the <see cref="_networkAvatarManager"/> in the scene to register itself to it.
        /// </summary>
        private void FindAndRegisterToNetworkAvatarManager()
        {
            if (_networkAvatarManager == null)
                _networkAvatarManager = FindObjectOfType<NetworkAvatarManager>();
            if (_networkAvatarManager != null)
                _networkAvatarManager.RegisterAvatar(OwnerClientId, this);
        }

        /// <summary>
        /// The event handler for the <see cref="NetworkVariable{T}.OnValueChanged"/> event of the <see cref="NetworkVariable{T}"/> class
        /// of the <see cref="_leftGripValue"/> variable to update the animation of the current avatar if the left grip button was pressed.
        /// </summary>
        /// <param name="previousValue">The previous left grip value.</param>
        /// <param name="newValue">The new left grip value.</param>
        private void OnLeftGripValueChanged(float previousValue, float newValue)
        {
            if (IsOwner) return;
            if (currentAvatar == null) return;
            
            currentAvatar.SetLeftGripAnimation(Mathf.Lerp(previousValue, newValue, Time.deltaTime));
        }

        /// <summary>
        /// The event handler for the <see cref="NetworkVariable{T}.OnValueChanged"/> event of the <see cref="NetworkVariable{T}"/> class
        /// of the <see cref="_leftTriggerValue"/> variable to update the animation of the current avatar if the left trigger button was pressed.
        /// </summary>
        /// <param name="previousValue">The previous left trigger value.</param>
        /// <param name="newValue">The new left trigger value.</param>
        private void OnLeftTriggerValueChanged(float previousValue, float newValue)
        {
            if (IsOwner) return;
            if (currentAvatar == null) return;
            if (!currentAvatar.useHandAnimation) return;
            
            currentAvatar.SetLeftTriggerAnimation(Mathf.Lerp(previousValue, newValue, Time.deltaTime));
        }

        /// <summary>
        /// The event handler for the <see cref="NetworkVariable{T}.OnValueChanged"/> event of the <see cref="NetworkVariable{T}"/> class
        /// of the <see cref="_rightGripValue"/> variable to update the animation of the current avatar if the right grip button was pressed.
        /// </summary>
        /// <param name="previousValue">The previous right grip value.</param>
        /// <param name="newValue">The new right grip value.</param>
        private void OnRightGripValueChanged(float previousValue, float newValue)
        {
            if (IsOwner) return;
            if (currentAvatar == null) return;
            if (!currentAvatar.useHandAnimation) return;
            
            currentAvatar.SetRightGripAnimation(Mathf.Lerp(previousValue, newValue, Time.deltaTime));
        }

        /// <summary>
        /// The event handler for the <see cref="NetworkVariable{T}.OnValueChanged"/> event of the <see cref="NetworkVariable{T}"/> class
        /// of the <see cref="_rightTriggerValue"/> variable to update the animation of the current avatar if the right trigger button was pressed.
        /// </summary>
        /// <param name="previousValue">The previous right trigger value.</param>
        /// <param name="newValue">The new right trigger value.</param>
        private void OnRightTriggerValueChanged(float previousValue, float newValue)
        {
            if (IsOwner) return;
            if (currentAvatar == null) return;
            if (!currentAvatar.useHandAnimation) return;
            
            currentAvatar.SetRightTriggerAnimation(Mathf.Lerp(previousValue, newValue, Time.deltaTime));
        }

        /// <summary>
        /// The event handler for the <see cref="NetworkVariable{T}.OnValueChanged"/> event of the <see cref="NetworkVariable{T}"/> class
        /// of the <see cref="_isLeftHandTracked"/> variable to update the visibility of the left hand.
        /// </summary>
        /// <param name="previousValue">The previous left trigger value.</param>
        /// <param name="newValue">The new left trigger value.</param>
        private void OnLeftHandTrackedChanged(bool previousValue, bool newValue)
        {
            if (currentAvatar == null) return;
            currentAvatar.SetLeftHandVisibility(newValue); 
        }

        /// <summary>
        /// The event handler for the <see cref="NetworkVariable{T}.OnValueChanged"/> event of the <see cref="NetworkVariable{T}"/> class
        /// of the <see cref="_isRightHandTracked"/> variable to update the visibility of the right hand.
        /// </summary>
        /// <param name="previousValue">The previous right trigger value.</param>
        /// <param name="newValue">The new right trigger value.</param>
        private void OnRightHandTrackedChanged(bool previousValue, bool newValue)
        {
            if (currentAvatar == null) return;
            currentAvatar.SetRightHandVisibility(newValue);
        }

        /// <summary>
        /// Set the tracking network value for the left hand
        /// </summary>
        /// <param name="newValue"></param>
        public void SetRightHandTrackingState(bool newValue)
        {
            //Debug.Log("SetRightHandTrackingState " + newValue);
            _isRightHandTracked.Value = newValue;
        }

        /// <summary>
        /// Set the tracking network value for the right hand
        /// </summary>
        /// <param name="newValue"></param>
        public void SetLeftHandTrackingState(bool newValue)
        {
            //Debug.Log("SetLeftHandTrackingState " + newValue);
            _isLeftHandTracked.Value = newValue;
        }

        /// <summary>
        /// The event handler for the <see cref="NetworkVariable{T}.OnValueChanged"/> event of the <see cref="NetworkVariable{T}"/> class
        /// of the <see cref="_networkDisplayName"/> variable to update the name of the avatar.
        /// </summary>
        /// <param name="previousValue">The previous name.</param>
        /// <param name="newValue">The new name.</param>
        private void OnNetworkDisplayNameChanged(ForceNetworkSerializeByMemcpy<FixedString64Bytes> previousValue,
                                                 ForceNetworkSerializeByMemcpy<FixedString64Bytes> newValue)
        {
            currentAvatar.SetDisplayName(newValue.Value.Value);
        }

        /// <summary>
        /// Overrides the <see cref="NetworkBehaviour.OnNetworkDespawn"/> method of the <see cref="NetworkBehaviour"/> class to
        /// unregister the avatar from the <see cref="_networkAvatarManager"/> and unregister all event handlers.
        /// </summary>
        public override void OnNetworkDespawn()
        {
            if (_networkAvatarManager != null)
                _networkAvatarManager.UnregisterAvatar(OwnerClientId, this);

            _networkDisplayName.OnValueChanged   -= OnNetworkDisplayNameChanged;
            _networkAvatarID.OnValueChanged -= OnNetworkAvatarIdChanged;
            
            localPlayerReferences = null;

            base.OnNetworkDespawn();
        }

        [ServerRpc(RequireOwnership = false)]
        private void RequestDisconnectServerRpc(ulong clientID)
        {
            if (!NetworkManager.Singleton.IsListening) return;
            NetworkManager.Singleton.DisconnectClient(clientID);
        }

        /// <summary>
        /// Updates the avatar transformation for the local player and hand animations.
        /// Calls <see cref="UpdateAvatarTransformsForLocalPlayer"/> and <see cref="UpdateHandAnimations"/>.
        /// </summary>
        private void Update()
        {
            UpdateAvatarTransformsForLocalPlayer();
            UpdateHandAnimations();
        }

        /// <summary>
        /// Sets the local player for this avatar.
        /// </summary>
        /// <param name="localPlayerReferences">The new local player for this avatar.</param>
        private void SetLocalPlayer(LocalPlayerReferences localPlayerReferences)
        {
            if (localPlayerReferences == null) return;

            _localPlayerReferences = localPlayerReferences;
        }

        /// <summary>
        /// Updates the transform of the avatar only for the owner of this avatar.
        /// </summary>
        private void UpdateAvatarTransformsForLocalPlayer()
        {
            // Make sure this avatar is a local player
            if (!IsOwner || !IsLocalPlayer) return;

            // After a scene loading, the avatar does not get destroyed, so OnNetworkSpawn is only called once
            // the Rig is different; therefore we need to register again to the new network manager 
            if (_localPlayerReferences == null || _localPlayerReferences.root == null)
            {
                FindAndRegisterToNetworkAvatarManager();
                if (_localPlayerReferences == null || _localPlayerReferences.root == null) return;
            }
            
            currentAvatar.UpdateAvatar(localPlayerReferences);
        }

        /// <summary>
        /// Updates the network variables for the controller values.
        /// </summary>
        private void UpdateHandAnimations()
        {
            if (!IsOwner) return;
            if (currentAvatar == null && !currentAvatar.useHandAnimation) return;

            if (_leftTriggerAction != null)
                _leftTriggerValue.Value = _leftTriggerAction.action.ReadValue<float>();
            if (_leftGripAction != null)
                _leftGripValue.Value = _leftGripAction.action.ReadValue<float>();
            if (_rightTriggerAction != null)
                _rightTriggerValue.Value = _rightTriggerAction.action.ReadValue<float>();
            if (_rightGripAction != null)
                _rightGripValue.Value = _rightGripAction.action.ReadValue<float>();

            // Update local avatar immediately 
            if (!_hideLocalAvatar && currentAvatar.useHandAnimation)
            {
                currentAvatar.SetLeftTriggerAnimation(_leftTriggerValue.Value);
                currentAvatar.SetLeftGripAnimation(_leftGripValue.Value);
                currentAvatar.SetRightTriggerAnimation(_rightTriggerValue.Value);
                currentAvatar.SetRightGripAnimation(_rightGripValue.Value);
            }
        }

        private void OnValidate()
        {
            if (!_avatars.Contains(_currentAvatar))
                _currentAvatar = null;
        }
    }
}
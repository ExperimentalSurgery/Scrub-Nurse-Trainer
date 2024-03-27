using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;
using UnityEngine.XR;

namespace NMY.VirtualRealityTraining.NetworkAvatars
{
    /// <summary>
    /// This class manages the communication between the local player references of the rig and the local
    /// avatar. It registers the avatar upon spawning, and it unregisters them upton despawning.
    /// It holds a dictionary of all connected avatars in the scene. 
    /// </summary>
    /// <remarks>
    /// Place this component into the scene. Each <see cref="NetworkAvatar"/> will find this object
    /// to call <see cref="RegisterAvatar"/>
    /// </remarks>
    public class NetworkAvatarManager : MonoBehaviour
    {
        /// <summary>
        /// An object containing the references to the rig elements of the local player.
        /// </summary>
        [FormerlySerializedAs("_localPlayer")]
        [SerializeField] private LocalPlayerReferences _localPlayerReferences;

        /// <summary>
        /// A unity event that is called when the local avatar spawned.
        /// </summary>
        [Header("Events")]
        public UnityEvent onLocalAvatarSpawned = new();

        /// <summary>
        /// A unity event that is called when the local avatar despawned.
        /// </summary>
        public UnityEvent onLocalAvatarDespawned = new();

        /// <summary>
        /// A dictionary containing the references to all <see cref="NetworkAvatar"/> and their corresponding client IDs.
        /// </summary>
        private static Dictionary<ulong, NetworkAvatar> _networkAvatars = new();

        public static NetworkAvatar localAvatar => _networkAvatars.ContainsKey(NetworkManager.Singleton.LocalClientId)
            ? _networkAvatars[NetworkManager.Singleton.LocalClientId]
            : null;

        public LocalPlayerReferences localPlayerReferences
        {
            get => _localPlayerReferences;
            set
            {
                _localPlayerReferences = value;
                if (localAvatar != null) localAvatar.localPlayerReferences = localPlayerReferences;
            }
        }

        private void Awake()
        {
            if (_localPlayerReferences == null)
                _localPlayerReferences = new LocalPlayerReferences();
        }

        /// <summary>
        /// Adds event listener to the network manager clients events.
        /// </summary>
        private async void OnEnable()
        {
            await UniTask.WaitUntil(() => NetworkManager.Singleton != null, 
                                    cancellationToken: this.GetCancellationTokenOnDestroy());
            NetworkManager.Singleton.OnServerStarted            += OnServerStarted;
            NetworkManager.Singleton.OnClientConnectedCallback  += OnClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnect;

            InputTracking.trackingAcquired += TrackingAcquired;
            InputTracking.trackingLost     += TrackingLost;
        }


        private void TrackingAcquired(XRNodeState obj)
        {
            switch (obj.nodeType)
            {
                case XRNode.LeftHand:
                    localAvatar.SetLeftHandTrackingState(true);
                    break;
                case XRNode.RightHand:
                    localAvatar.SetRightHandTrackingState(true);
                    break;
                default:
                    break;
            }
        }

        private void TrackingLost(XRNodeState obj)
        {
            switch (obj.nodeType)
            {
                case XRNode.LeftHand:
                    localAvatar.SetLeftHandTrackingState(false);
                    break;
                case XRNode.RightHand:
                    localAvatar.SetRightHandTrackingState(false);
                    break;
                default:
                    break;
            }
        }


        /// <summary>
        /// Removes event listener from the network manager clients events.
        /// </summary>
        private void OnDisable()
        {
            if (NetworkManager.Singleton == null) return;
            NetworkManager.Singleton.OnServerStarted            -= OnServerStarted;
            NetworkManager.Singleton.OnClientConnectedCallback  -= OnClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnect;

            InputTracking.trackingAcquired -= TrackingAcquired;
            InputTracking.trackingLost     -= TrackingLost;
        }

        /// <summary>
        /// An event handler for the <see cref="NetworkManager.OnServerStarted"/> event of the <see cref="NetworkManager"/> class.
        /// </summary>
        private void OnServerStarted()
        {

        }

        /// <summary>
        /// An event handler for the <see cref="NetworkManager.OnClientConnectedCallback"/> event of the <see cref="NetworkManager"/> class.
        /// </summary>
        private void OnClientConnected(ulong clientID)
        {

        }

        /// <summary>
        /// An event handler for the <see cref="NetworkManager.OnClientDisconnectCallback"/> event of the <see cref="NetworkManager"/> class.
        /// </summary>
        private void OnClientDisconnect(ulong clientID)
        {
            // Are we the client that is disconnecting?
            if (clientID == NetworkManager.Singleton.LocalClientId)
            {
                // Show UI to connect again
            }
        }

        /// <summary>
        /// Registers a <see cref="NetworkAvatar"/> to this manager. The manager sets the local player to the
        /// local client if <see cref="clientID"/> is equals to <see cref="NetworkManager.LocalClientId"/>.
        /// </summary>
        /// <param name="clientID">The client ID of the avatar.</param>
        /// <param name="networkAvatar">The network avatar that will be added to this manager.</param>
        public void RegisterAvatar(ulong clientID, NetworkAvatar networkAvatar)
        {
            Debug.Log($"{GetType()}: Register avatar from clientID={clientID}.", this);

            if (_networkAvatars.ContainsKey(clientID)) _networkAvatars[clientID] = networkAvatar;
            else _networkAvatars.Add(clientID, networkAvatar);

            if (clientID == NetworkManager.Singleton.LocalClientId)
            {
                onLocalAvatarSpawned?.Invoke();
                networkAvatar.localPlayerReferences = _localPlayerReferences;
            }
        }

        /// <summary>
        /// Unregisters a <see cref="NetworkAvatar"/> from this manager.
        /// </summary>
        /// <param name="clientID">The client ID of the avatar.</param>
        /// <param name="networkAvatar">The network avatar that will be removed to this manager.</param>
        public void UnregisterAvatar(ulong clientID, NetworkAvatar networkAvatar)
        {
            Debug.Log($"{GetType()}: Unregister avatar from clientID={clientID}.", this);

            if (clientID == NetworkManager.Singleton.LocalClientId)
            {
                onLocalAvatarDespawned?.Invoke();
            }

            if (_networkAvatars.ContainsKey(clientID))
                _networkAvatars.Remove(clientID);
        }

        public void SetLeftHand(Transform newLeftHand)
        {
            localPlayerReferences.leftHand = newLeftHand;
            localPlayerReferences          = localPlayerReferences;
        }
        
        public void SetRightHand(Transform newRightHand)
        {
            localPlayerReferences.rightHand = newRightHand;
            localPlayerReferences          = localPlayerReferences;
        }
    }
}
using System.Collections.Generic;
using System.Linq;
using NMY.VirtualRealityTraining.NetworkAvatars;
using UnityEngine;

namespace NMY.VirtualRealityTraining
{
    /// <summary>
    /// Disables all renderers from this GameObject and all children renderers when the local avatar is spawned.
    /// </summary>
    public class DisableRenderersAtLocalAvatarSpawn : MonoBehaviour
    {
        [SerializeField] private bool _activateRenderersAtStart;
        [SerializeField] private bool _includeInactive;

        /// <summary>
        /// A list of renderers to be deactivated when the local avatar is spawned.
        /// </summary>
        /// <remarks>
        /// List gets filled in <see cref="Awake"/>.
        /// If <see cref="_includeInactive"/> is true, it also uses inactive renderers from this component and all children
        /// </remarks>
        private List<Renderer> _renderers = new();

        /// <summary>
        /// A reference to the <see cref="NetworkAvatarManager"/> in the scene.
        /// </summary>
        private NetworkAvatarManager _networkAvatarManager;

        /// <summary>
        /// Finds all renderer components from this component and all children.
        /// Also finds the <see cref="NetworkAvatarManager"/> in the scene.
        /// </summary>
        private void Awake()
        {
            _renderers            = GetComponentsInChildren<Renderer>(_includeInactive).ToList();
            _networkAvatarManager = FindObjectOfType<NetworkAvatarManager>();
        }

        /// <summary>
        /// Activates all renderers if <see cref="_activateRenderersAtStart"/> is true.
        /// </summary>
        private void Start()
        {
            if (_activateRenderersAtStart) ActivateRenderers();
        }

        /// <summary>
        /// Adds event listener to the events of <see cref="NetworkAvatarManager"/>.
        /// </summary>
        private void OnEnable()
        {
            _networkAvatarManager.onLocalAvatarSpawned.AddListener(OnLocalAvatarSpawned);
            _networkAvatarManager.onLocalAvatarDespawned.AddListener(OnLocalAvatarDespawned);
        }

        /// <summary>
        /// Removes event listener from the events of <see cref="NetworkAvatarManager"/>.
        /// </summary>
        private void OnDisable()
        {
            _networkAvatarManager.onLocalAvatarSpawned.RemoveListener(OnLocalAvatarSpawned);
            _networkAvatarManager.onLocalAvatarDespawned.RemoveListener(OnLocalAvatarDespawned);
        }

        /// <summary>
        /// The event handler for the <see cref="NetworkAvatarManager.onLocalAvatarSpawned"/> event of the <see cref="NetworkAvatarManager"/> class.
        /// </summary>
        private void OnLocalAvatarSpawned() => DeactivateRenderers();

        /// <summary>
        /// The event handler for the <see cref="NetworkAvatarManager.onLocalAvatarDespawned"/> event of the <see cref="NetworkAvatarManager"/> class.
        /// </summary>
        private void OnLocalAvatarDespawned() => ActivateRenderers();


        /// <summary>
        /// Activates all renderers from <see cref="_renderers"/>.
        /// </summary>
        private void ActivateRenderers()   => _renderers.ForEach(r => r.enabled = true);
        
        /// <summary>
        /// Deactivates all renderers from <see cref="_renderers"/>.
        /// </summary>
        private void DeactivateRenderers() => _renderers.ForEach(r => r.enabled = false);
    }
}
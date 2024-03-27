using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

#if NMY_ENABLE_UNITY_ATOMS
using UnityAtoms.BaseAtoms;
#endif

#if NMY_ENABLE_UNITY_ATOMS_TAGS
using UnityAtoms.Tags;
#endif

namespace NMY.VirtualRealityTraining
{
    public class TriggerEvent : MonoBehaviour
    {
        [Tooltip(" A list of specific Colliders")]
        [SerializeField] private List<Collider> _trigger = new();
        
        [Tooltip("A list of GameObjects, all Colliders in their hierarchies will be considered")]
        [SerializeField] private List<GameObject> _triggerHierarchy = new();

#if NMY_ENABLE_UNITY_ATOMS_TAGS
        [Tooltip("A list of Atom Tags whose Collider will be considered")]
        [SerializeField] private List<StringConstant> _triggerTags = new();
        
        [Tooltip("A list of Atom Tags whose Colliders in their hierarchies will be considered")]
        [SerializeField] private List<StringConstant> _triggerHierarchyTags = new();
#endif

        [Space]
        public UnityEvent<Collider> onTriggerEnter;
        public UnityEvent<Collider> onTriggerStay;
        public UnityEvent<Collider> onTriggerExit;

        private void Start()
        {
            // extend our main Collider List
            foreach (var go in _triggerHierarchy)
            {
                if (go == null) continue;
                _trigger.AddRange(go.GetComponentsInChildren<Collider>());
            }

#if NMY_ENABLE_UNITY_ATOMS_TAGS
            foreach (var constant in _triggerTags)
            {
                if (constant == null) continue;
                var list = AtomTags.FindAllByTag(constant.Value);
                foreach (var go in list)
                {
                    var c = go.GetComponent<Collider>();
                    if (c == null) continue;
                    _trigger.Add(c);
                }
            }

            foreach (var constant in _triggerHierarchyTags)
            {
                if (constant == null) continue;
                var list = AtomTags.FindAllByTag(constant.Value);
                foreach (var go in list)
                {
                    var colliders = go.GetComponentsInChildren<Collider>();
                    if (colliders.Length <= 0) continue;
                    _trigger.AddRange(colliders);
                }
            }
#endif
        }


        private void OnTriggerEnter(Collider other)
        {
            if (_trigger.Count == 0 || _trigger.Contains(other)) 
                onTriggerEnter?.Invoke(other);
        }

        private void OnTriggerStay(Collider other)
        {
            if (_trigger.Count == 0 || _trigger.Contains(other))
                onTriggerStay?.Invoke(other);
        }

        private void OnTriggerExit(Collider other)
        {
            if (_trigger.Count == 0 || _trigger.Contains(other))
                onTriggerExit?.Invoke(other);
        }
    }
}

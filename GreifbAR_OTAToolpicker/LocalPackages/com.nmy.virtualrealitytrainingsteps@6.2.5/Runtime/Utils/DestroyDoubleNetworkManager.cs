using System.Linq;
using Unity.Netcode;
using UnityEngine;

namespace NMY.VirtualRealityTraining
{
    [RequireComponent(typeof(NetworkManager))]
    public class DestroyDoubleNetworkManager : MonoBehaviour
    {
        private void Awake()
        {
            var managers = FindObjectsOfType<NetworkManager>();
            
            if (managers.Length > 1 && managers.Contains(GetComponent<NetworkManager>()))
                Destroy(gameObject);
        }
    }
}

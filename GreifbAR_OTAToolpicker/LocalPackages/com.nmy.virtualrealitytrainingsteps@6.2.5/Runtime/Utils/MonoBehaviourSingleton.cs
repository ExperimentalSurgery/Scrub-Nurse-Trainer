using UnityEngine;

namespace NMY.VirtualRealityTraining
{
    public abstract class MonoBehaviourSingleton<T> : MonoBehaviour where T : MonoBehaviour
    {
        // Check to see if we're about to be destroyed.
        private static readonly object Lock = new();
        protected static        T      _instance;

        /// <summary>
        /// Access singleton instance through this propriety.
        /// </summary>
        public static T instance
        {
            get
            {
                lock (Lock)
                {
                    if (_instance == null)
                    {
                        // Search for existing instance.
                        _instance = (T)FindObjectOfType(typeof(T));

                        // Create new instance if one doesn't already exist.
                        if (_instance == null)
                        {
                            // Need to create a new GameObject to attach the singleton to.
                            var singletonObject = new GameObject();
                            _instance            = singletonObject.AddComponent<T>();
                            singletonObject.name = typeof(T) + " (Singleton)";
                        }
                    }
                    // Make instance persistent even if its already in the scene
                    DontDestroyOnLoad(_instance.gameObject);
                    return _instance;
                }
            }
        }
    }
}


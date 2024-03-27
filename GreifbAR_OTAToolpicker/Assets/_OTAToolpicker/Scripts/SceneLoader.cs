using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace NMY.OTAToolpicker
{
    public class SceneLoader : MonoBehaviour
    {
        [SerializeField] private KeyCode reloadScene = KeyCode.Z;

        // Update is called once per frame
        void Update()
        {
            if (Input.GetKeyDown(reloadScene))
            {
                SceneManager.LoadScene(0);
            }
        }
    }
}

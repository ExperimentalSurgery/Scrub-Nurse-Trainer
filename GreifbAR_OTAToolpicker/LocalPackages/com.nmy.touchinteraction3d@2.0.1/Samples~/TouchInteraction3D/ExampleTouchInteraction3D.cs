using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace NMY.Example {

    public class ExampleTouchInteraction3D : MonoBehaviour
    {
        private Vector3 origPos;
        private Quaternion origRot;

        void Start()
        {
            Camera cam = Camera.main;
            origPos = cam.transform.position;
            origRot = cam.transform.rotation;
        }

        public void ResetCamera()
        {
            Camera cam = Camera.main;
            cam.transform.position = origPos;
            cam.transform.rotation = origRot;
        }
    }

} // namespace

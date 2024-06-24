using Leap.Unity;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace NMY.OTAToolpicker
{
    public class AttachFloating : MonoBehaviour
    {
        [SerializeField]
        Transform attachTransform;
        public Vector3 defaultPosition;
        public Vector3 offsetDir;
        public Vector3 rotationOffset;

        public float floatingSpeed;
        public float floatingMinDistance;

        Camera cam;
        Vector3 currentVelocity;
        Vector3 targetPos;
        // Start is called before the first frame update
        void Start()
        {
        }

        // Update is called once per frame
        void Update()
        {
            if (attachTransform == null)
                return;

            Vector3 target = GetTargetPosition();

            //Slowly float towards target position if the distance to the object is too large
            float distance = Mathf.Abs(Vector3.Distance(target, transform.position));
            if (distance > floatingMinDistance)
                targetPos = target;

            transform.position = Vector3.SmoothDamp(transform.position, targetPos, ref currentVelocity, floatingSpeed * Time.deltaTime);
            
            RotateTowardsCamera();
        }

        public void AttachToTransform(Transform trans)
        {
            attachTransform = trans;

            if(cam == null)
                cam = Camera.main;

            transform.position = GetTargetPosition();
            RotateTowardsCamera();
        }

        Vector3 GetTargetPosition()
        {
            Vector3 forward = cam.transform.position - attachTransform.position;
            Vector3 up = Vector3.up;
            Vector3 pos = attachTransform.position;

            //Offset to source object, aligned to the camera plane
            Quaternion rotationToCamera = Quaternion.LookRotation(forward, up);
            Matrix4x4 target = Matrix4x4.TRS(pos, rotationToCamera, transform.localScale);
            Matrix4x4 offset = Matrix4x4.TRS(offsetDir, Quaternion.identity, transform.localScale);
            target *= offset;

            return target.GetPosition();
        }

        void RotateTowardsCamera()
        {
            transform.LookAt(cam.transform);
            Quaternion rotOffset = Quaternion.Euler(rotationOffset);
            transform.rotation *= rotOffset;
        }


    }
}

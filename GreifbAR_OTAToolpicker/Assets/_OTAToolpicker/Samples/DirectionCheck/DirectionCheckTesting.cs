using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace NMY.OTAToolpicker
{
    public class DirectionCheckTesting : MonoBehaviour
    {

        public float angleA;
        public float angleB;

        public Transform sourceDirection;
        public Transform desiredDirection;
        
        private void Update()
        {
            angleA = DoDirectionCheckLAT(sourceDirection);
            angleB = DoDirectionCheck2(sourceDirection, sourceDirection.up);
        }

        /// <summary>
        /// TODO: @PE --> This is the version i use in LAT to check the direction of the userhand palmdirection
        /// facing a certain direction. Maybe its working and we just need to switch the check method. :-)
        /// </summary>
        /// <param name="instrumentDirection"></param>
        /// <returns></returns>
        private float DoDirectionCheckLAT(Transform instrumentDirection)
        {
            Vector3 up = Vector3.up;
            Vector3 sourceForward = instrumentDirection.forward;
            Vector3.OrthoNormalize(ref up, ref sourceForward);
            Vector3 targetForward = desiredDirection.forward;
            Vector3.OrthoNormalize(ref up, ref targetForward);

            // get the angle between both transforms around the Y axis
            float dot = Vector3.Dot(sourceForward, targetForward);
            float angleRad = Mathf.Acos(dot);
            float angleDeg = angleRad * Mathf.Rad2Deg;

            // see if the angle is clockwise or counter-clockwise
            Vector3 up2 = Vector3.Cross(sourceForward, targetForward);
            angleDeg = up2.y < 0 ? -angleDeg : +angleDeg;
            float angleDegAbs = Mathf.Abs(angleDeg);
            return angleDegAbs;
        }

      

        /// <summary>
        /// Returns the angle between the forward direction of the instrument and the forward direction of the placement volume.
        /// This one seems to work more reliably than the one based on Quaternion.FromToRotation.
        /// </summary>
        /// <param name="instrumentDirection"></param>
        /// <param name="up"></param>
        /// <returns></returns>
        private float DoDirectionCheck2(Transform instrumentDirection, Vector3 up)
        {
            // float angle = Vector3.Angle(instrumentDirection.forward, forwardDirection.forward);
            float angle = Vector3.SignedAngle(instrumentDirection.forward, desiredDirection.forward, up);
            return angle;
            // var q = Quaternion.FromToRotation(placeableInstrument.ForwardDirection.forward, forwardDirection.forward);
            // float yRot = q.eulerAngles.z;
            // if (yRot > 180f)
            //     yRot -= 360f;
            // isInstrumentDirectionValid = Mathf.Abs(yRot) <= maxForwardDeltaDegree;
            // return yRot;
        }
    }
}

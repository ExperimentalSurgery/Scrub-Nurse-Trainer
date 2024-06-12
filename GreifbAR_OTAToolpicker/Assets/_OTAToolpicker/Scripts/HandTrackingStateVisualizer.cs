using System;
using System.Collections;
using System.Collections.Generic;
using Leap.Unity;
using UnityEngine;
using UnityEngine.XR.Hands;

namespace DFKI.NMY
{
    public class HandTrackingStateVisualizer : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private XRHandTrackingEvents left;
        [SerializeField] private XRHandTrackingEvents right;

        [SerializeField] private GameObject leftTrackedVisual;
        [SerializeField] private GameObject leftLostVisual;
        [SerializeField] private GameObject rightTrackedVisual;
        [SerializeField] private GameObject rightLostVisual;

        private bool lastStateLeft = true;
        private bool lastStateRight = true;
        
        void Update()
        {
	        if (lastStateLeft != left.handIsTracked)
	        {

		        lastStateLeft = left.handIsTracked;
				leftLostVisual.SetActive(!left.handIsTracked);
		        leftTrackedVisual.SetActive(left.handIsTracked);
	        }

	        if (lastStateRight != right.handIsTracked)
	        {
		        lastStateRight = right.handIsTracked;
		        rightLostVisual.SetActive(!right.handIsTracked);
		        rightTrackedVisual.SetActive(right.handIsTracked);
	        }

        }
    }
}

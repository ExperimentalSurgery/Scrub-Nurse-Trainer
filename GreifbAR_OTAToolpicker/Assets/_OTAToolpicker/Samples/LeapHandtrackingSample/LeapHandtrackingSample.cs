using System;
using System.Collections.Generic;
using System.Threading;
using Leap.Unity;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Serialization;
using Leap;

namespace NMY.OTAToolpicker
{
    public class LeapHandtrackingSample : MonoBehaviour
    {
        [FormerlySerializedAs("leapServiceProvider")]
        [SerializeField] private LeapServiceProvider leapProvider;
        [SerializeField] private float forceTrackingModeDelayS = 1f;

        // [SerializeField] private HandBinder leftHandBinder;
        // [SerializeField] private HandBinder rightHandBinder;

        public bool IsLeftHandDetected { get; private set;}
        public bool IsRightHandDetected { get; private set;}

        async void Start()
        {
            await UniTask.WaitForSeconds(forceTrackingModeDelayS);
            leapProvider.ChangeTrackingMode(LeapServiceProvider.TrackingOptimizationMode.Desktop);

            await UniTask.WaitUntil(() => IsRightHandDetected);
            Debug.Log("Right hand detected");
        }

        private void OnEnable()
        {
            leapProvider.OnUpdateFrame += OnUpdateFrame;
        }
        private void OnDisable()
        {
            leapProvider.OnUpdateFrame -= OnUpdateFrame;
        }

        void OnUpdateFrame(Frame frame)
        {
            Hand leftHand = frame.GetHand(Chirality.Left);
            Hand rightHand = frame.GetHand(Chirality.Right);
            IsLeftHandDetected = leftHand != null;
            IsRightHandDetected = rightHand != null;
        }

        // async public UniTask WaitForHand(LeapServiceProvider leapProvider, Chirality chirality, CancellationToken cancellationToken = default(CancellationToken))
        // {
        //     try
        //     {
        //         Frame frame = leapProvider.CurrentFrame;
        //         int handCount = frame.Hands.Count;
        //         Hand hand = frame.GetHand(chirality);
        //         await UniTask.WaitUntil(() => handCount>0 && hand!=null, cancellationToken: cancellationToken);
        //     }
        //     catch (OperationCanceledException)
        //     {
        //         Debug.Log("Hand tracking was cancelled");
        //     }

        // }

    }
}

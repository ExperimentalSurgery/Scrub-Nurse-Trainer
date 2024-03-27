using Cysharp.Threading.Tasks;
using Leap.Unity;
using Leap.Unity.Examples;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace NMY.OTAToolpicker
{
    public class LeapPoseRecorderDesktop : MonoBehaviour
    {
        //[SerializeField] private LeapServiceProvider leapProvider;
        //[SerializeField] private float forceTrackingModeDelayS = 1f;

        //[SerializeField] private PoseRecorderExampleManager poseRecorderManager;
        //[SerializeField] private KeyCode startRecordingKey = KeyCode.Space;

        //public bool IsLeftHandDetected { get; private set;}
        //public bool IsRightHandDetected { get; private set;}

        //async void Start()
        //{
        //    await UniTask.WaitForSeconds(forceTrackingModeDelayS);
        //    leapProvider.ChangeTrackingMode(LeapServiceProvider.TrackingOptimizationMode.Desktop);
        //}

        //void Update()
        //{
        //    if (Input.GetKeyDown(startRecordingKey))
        //    {
        //        Debug.Log("Start recording");
        //        poseRecorderManager.BeginRecording();
        //        // leapProvider.ChangeTrackingMode(LeapServiceProvider.TrackingOptimizationMode.Desktop);
        //    }
        //}
    }
}

using DFKI.NMY;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using Vuforia;

namespace NMY.OTAToolpicker
{
    /// <summary>
    /// Table marker implementation that supports Vuforia and Varjo tracking hardware.
    /// </summary>
    public class TableMarker : MonoBehaviour, IMarker
    {
        [SerializeField] private TrackingHardware trackingHardware = TrackingHardware.Vuforia;
        public TrackingHardware TrackingHardware {
            get => trackingHardware;
            set => trackingHardware = value;
        }

        #region Vuforia components
        [SerializeField] private ImageTargetBehaviour imageTargetBehaviour;
        [SerializeField] private DefaultObserverEventHandler observerEventHandler;
        #endregion

        #region Varjo Components
        [SerializeField] private OTAVarjoTableMarker varjoMarker;
        public OTAVarjoTableMarker VarjoMarker => varjoMarker;
        #endregion

        public UnityEvent OnMarkerFound => onMarkerFound;
        public UnityEvent OnMarkerLost => onMarkerLost;

        private readonly UnityEvent onMarkerFound = new();
        private readonly UnityEvent onMarkerLost = new();

        public void EnableTracking()
        {
            Debug.Log("TableMarker::EnableTracking: Table marker hardware: " +  trackingHardware);
            switch (trackingHardware)
            {
                case TrackingHardware.Vuforia:
                    EnableTrackingVuforia();
                    break;
                case TrackingHardware.Varjo:
                    EnableTrackingVarjo();
                    DisableTrackingVuforia();
                    break;
            }
        }

        public void DisableTracking()
        {
            switch (trackingHardware)
            {
                case TrackingHardware.Vuforia:
                    DisableTrackingVuforia();
                    break;
                case TrackingHardware.Varjo:
                    DisableTrackingVarjo();
                    break;
            }
        }

        public bool IsTrackingEnabled {
            get
            {
                return trackingHardware switch
                {
                    TrackingHardware.Vuforia => IsTrackingEnabledVuforia,
                    TrackingHardware.Varjo => IsTrackingEnabledVarjo,
                    _ => false,
                };
            }
        }

        #region Vuforia components
        public void EnableTrackingVuforia()
        {
            imageTargetBehaviour.enabled = true;
            observerEventHandler.enabled = true;

            observerEventHandler.OnTargetFound.AddListener(OnMarkerFound.Invoke);
            observerEventHandler.OnTargetLost.AddListener(OnMarkerLost.Invoke);
        }

        public void DisableTrackingVuforia()
        {
            imageTargetBehaviour.enabled = false;
            observerEventHandler.enabled = false;

            observerEventHandler.OnTargetFound.RemoveListener(OnMarkerFound.Invoke);
            observerEventHandler.OnTargetLost.RemoveListener(OnMarkerLost.Invoke);
        }

        public bool IsTrackingEnabledVuforia => imageTargetBehaviour.enabled && observerEventHandler.enabled;
        #endregion

        #region Varjo components
        public void EnableTrackingVarjo()
        {
            if (varjoMarker==null)
            {
                Debug.LogError("Varjo Marker on TableMarker is not assigned");
                return;
            }
            varjoMarker.SetTrackingEnabled(true);
            varjoMarker.enabled = true;
            varjoMarker.MarkerFound.AddListener((marker) => {                
                OnMarkerFound.Invoke();
            });
            varjoMarker.MarkerLost.AddListener((instrumentMarker) => {
                OnMarkerLost.Invoke();
            });
        }

        public void DisableTrackingVarjo()
        {            
            // Debug.Log("Marker disabled: " + name);
            if (varjoMarker == null)
            {
                Debug.LogError($"Varjo Marker on {name} is not assigned");
                return;
            }
            varjoMarker.SetTrackingEnabled(false);
            varjoMarker.MarkerFound.RemoveAllListeners();
            varjoMarker.MarkerLost.RemoveAllListeners();
            varjoMarker.enabled = false;
            varjoMarker.isTracked = false;
        }

        public bool IsTrackingEnabledVarjo => varjoMarker.shouldTrack;
        #endregion
    }
}

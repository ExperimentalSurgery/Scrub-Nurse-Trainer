using System.Collections.Generic;
using DFKI.NMY;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Events;
using Varjo.XR;
using Vuforia;

namespace NMY.OTAToolpicker
{
    /// <summary>
    /// Instrument marker implementation which can be configured to use different tracking hardware.
    /// </summary>
    public class InstrumentMarker : MonoBehaviour, IInstrumentMarker
    {
        [SerializeField] private TrackingHardware trackingHardware = TrackingHardware.Vuforia;
        public TrackingHardware TrackingHardware {
            get => trackingHardware;
            set => trackingHardware = value;
        }

        public InstrumentData Instrument => placeableInstrument.InstrumentData;

        [SerializeField] private PlaceableInstrument placeableInstrument;
        public PlaceableInstrument PlaceableInstrument => placeableInstrument;

        [SerializeField] private PlaceableInstrumentElement instrumentElementsDisplayed = PlaceableInstrumentElement.All;
        public PlaceableInstrumentElement ElementsDisplayed {
            get => instrumentElementsDisplayed;
            set => instrumentElementsDisplayed = value;
        }

        public bool alwaysShowName = false;

        #region Vuforia components
        [SerializeField] private ImageTargetBehaviour imageTargetBehaviour;
        [SerializeField] private InstrumentObserverEventHandler observerEventHandler;
        #endregion

        #region Varjo Components
        [SerializeField] private OTAVarjoMarker varjoMarker;
        public OTAVarjoMarker VarjoMarker => varjoMarker;
        #endregion

        public UnityEvent<InstrumentMarker> OnInstrumentFound => onInstrumentFound;
        public UnityEvent<InstrumentMarker> OnInstrumentLost => onInstrumentLost;
        public UnityEvent<InstrumentMarker> OnInstrumentDropped => onInstrumentDropped;

        private readonly UnityEvent<InstrumentMarker> onInstrumentFound = new();
        private readonly UnityEvent<InstrumentMarker> onInstrumentLost = new();
        private readonly UnityEvent<InstrumentMarker> onInstrumentDropped = new();

        /// <summary>
        /// Determines if this marker is currently the nearest. Updated by InstrumentMarkerController.Update()
        /// </summary>
        public bool isNearest = false;


        void Awake()
        {
            if (placeableInstrument == null)
                placeableInstrument = GetComponentInChildren<PlaceableInstrument>();

            Assert.IsNotNull(placeableInstrument, "PlaceableInstrument must not be null! Please assign a PlaceableInstrument to the VuforiaInstrumentMarker or add a PlaceableInstrument as a child of the VuforiaInstrumentMarker.");
            Assert.IsNotNull(imageTargetBehaviour, "ImageTargetBehaviour must not be null! Please assign an ImageTargetBehaviour to the VuforiaInstrumentMarker.");
            Assert.IsNotNull(observerEventHandler, "InstrumentObserverEventHandler must not be null! Please assign an InstrumentObserverEventHandler to the VuforiaInstrumentMarker.");

            // TODO: PE --> check what needs to be done in Varjo case over here?
            if (trackingHardware == TrackingHardware.Vuforia) {
                observerEventHandler.InstrumentMarker = this;
            }
        }

        public void EnableTracking()
        {
            switch (trackingHardware)
            {
                case TrackingHardware.Vuforia:
                    EnableTrackingVuforia();
                    DisableTrackingVarjo();
                    break;
                case TrackingHardware.Varjo:
                    EnableTrackingVarjo();
                    DisableTrackingVuforia();
                    break;
                default:
                    throw new System.NotImplementedException($"Tracking hardware {trackingHardware} is not implemented yet.");
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
                default:
                    throw new System.NotImplementedException($"Tracking hardware {trackingHardware} is not implemented yet.");
            }
        }

        public bool IsTrackingEnabled {
            get
            {
                switch (trackingHardware)
                {
                    case TrackingHardware.Vuforia:
                        return IsTrackingEnabledVuforia;
                    case TrackingHardware.Varjo:
                        return IsTrackingEnabledVarjo;
                    default:
                        throw new System.NotImplementedException($"Tracking hardware {trackingHardware} is not implemented yet.");
                }
            }
        }

        public void UpdateInstrumentElementVisibility(PlaceableInstrumentElement elementsDisplayed)
        {
            if (placeableInstrument == null)
                return;

            if(alwaysShowName)
                elementsDisplayed = elementsDisplayed | PlaceableInstrumentElement.Name;

            placeableInstrument.UpdateInstrumentElementVisibility(elementsDisplayed);
        }

        public void HideAllInstrumentElements()
        {
            UpdateInstrumentElementVisibility(PlaceableInstrumentElement.None);
        }

        #region Vuforia specific methods
        public void EnableTrackingVuforia()
        {
            imageTargetBehaviour.enabled = true;
            observerEventHandler.enabled = true;

            observerEventHandler.OnInstrumentFound.AddListener((instrumentMarker) => {
                UpdateInstrumentElementVisibility(instrumentElementsDisplayed);
                OnInstrumentFound.Invoke(instrumentMarker);
            });
            observerEventHandler.OnInstrumentLost.AddListener((instrumentMarker) => {
                HideAllInstrumentElements();
                OnInstrumentLost.Invoke(instrumentMarker);
            });
            observerEventHandler.OnInstrumentDropped.AddListener(OnInstrumentDropped.Invoke);
        }

        public void DisableTrackingVuforia()
        {
            imageTargetBehaviour.enabled = false;
            observerEventHandler.enabled = false;

            observerEventHandler.OnInstrumentFound.RemoveAllListeners();
            observerEventHandler.OnInstrumentLost.RemoveAllListeners();
            observerEventHandler.OnInstrumentDropped.RemoveListener(OnInstrumentDropped.Invoke);
        }

        public bool IsTrackingEnabledVuforia => imageTargetBehaviour.enabled && observerEventHandler.enabled;
        #endregion

        #region Varjo specific methods
        public void EnableTrackingVarjo()
        {
            if (varjoMarker==null)
            {
                Debug.LogError($"Varjo Marker on {name} is not assigned");
                return;
            }

            varjoMarker.enabled = true;
            varjoMarker.MarkerFound.AddListener((instrumentMarker) => {
                UpdateInstrumentElementVisibility(instrumentElementsDisplayed);
                OnInstrumentFound.Invoke(instrumentMarker);
            });
            varjoMarker.MarkerLost.AddListener((instrumentMarker) => {
                HideAllInstrumentElements();
                OnInstrumentLost.Invoke(instrumentMarker);
                OnInstrumentDropped.Invoke(instrumentMarker);
            });
            varjoMarker.SetTrackingEnabled(true);
        }

        public void DisableTrackingVarjo()
        {
            Debug.Log("Marker disabled: " + name);
            if (varjoMarker==null)
            {
                Debug.LogError($"Varjo Marker on {name} is not assigned");
                return;
            }
            varjoMarker.SetTrackingEnabled(false);
            varjoMarker.MarkerFound.RemoveAllListeners();
            varjoMarker.MarkerLost.RemoveAllListeners();
            varjoMarker.enabled = false;
        }

        public bool IsTrackingEnabledVarjo => varjoMarker.shouldTrack;

        #endregion
    }
}

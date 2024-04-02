using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NMY.OTAToolpicker.UI;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;

namespace NMY.OTAToolpicker
{
    public class InstrumentMarkerController : MonoBehaviour, IInstrumentMarkerController
    {
        [SerializeField] private bool isTrackingEnabledInitially = true;

        [SerializeField] private List<InstrumentMarker> instrumentMarkers = new();
        public IEnumerable<InstrumentMarker> InstrumentMarkers => instrumentMarkers;

        [SerializeField] private InstrumentDetailsUI instrumentDetailsUI;
        [Tooltip("If <b>true</b>, the instrument details UI will be shown when an instrument is found.")]
        [SerializeField] private bool isShowingInstrumentDetails = true;
        public bool IsShowingInstrumentDetails {
            get => isShowingInstrumentDetails;
            set => isShowingInstrumentDetails = value;
        }

        [Header("Audio")]
        [Tooltip("The audio SFX to play when an instrument was found.")]
        [SerializeField] private AudioClip instrumentFoundAudioClip;
        [Tooltip("If true, an audio SFX will be played when an instrument was found.")]
        [SerializeField] private bool isPlayingAudioOnInstrumentFound = true;
        public bool IsPlayingAudioOnInstrumentFound {
            get => isPlayingAudioOnInstrumentFound;
            set => isPlayingAudioOnInstrumentFound = value;
        }
        [Tooltip("The volume of the audio SFX to play when an instrument was found.")]
        [SerializeField] private float instrumentFoundAudioClipVolume = 1f;
        [SerializeField] private float instrumentFoundAudioClipCooldown = 2f;

        [Tooltip("The audio SFX to play when an instrument was lost.")]
        [SerializeField] private AudioClip instrumentLostAudioClip;
        [Tooltip("If true, an audio SFX will be played when an instrument was lost.")]
        [SerializeField] private bool isPlayingAudioOnInstrumentLost = true;
        public bool IsPlayingAudioOnInstrumentLost {
            get => isPlayingAudioOnInstrumentLost;
            set => isPlayingAudioOnInstrumentLost = value;
        }
        [Tooltip("The volume of the audio SFX to play when an instrument was lost.")]
        [SerializeField] private float instrumentLostAudioClipVolume = 1f;
        [SerializeField] private float instrumentLostAudioClipCooldown = 2f;

        private UnityEvent<InstrumentMarker> instrumentFound = new();
        public UnityEvent<InstrumentMarker> InstrumentFound => instrumentFound;
        private UnityEvent<InstrumentMarker> instrumentLost = new();
        public UnityEvent<InstrumentMarker> InstrumentLost => instrumentLost;
        private UnityEvent<InstrumentMarker> instrumentDropped = new();
        public UnityEvent<InstrumentMarker> InstrumentDropped => instrumentDropped;

        private int nrOfVisibleInstruments = 0;

        private bool hasFoundInstrumentOnce = false;
        public bool HasFoundInstrumentOnce => hasFoundInstrumentOnce;
        private float lastInstrumentSeenTime = 0f;
        private float lastInstrumentLostTime = 0f;

        private List<InstrumentData> currentlyTrackedInstruments = new List<InstrumentData>();
        public bool IsCurrentlyTracked(InstrumentData instrumentData) => currentlyTrackedInstruments.Contains(instrumentData);

        void Awake()
        {
            instrumentMarkers = FindInstrumentMarkers();
            if (isTrackingEnabledInitially)
                EnableAllInstrumentMarkers();
            else
                DisableAllInstrumentMarkers();

            Debug.Log($"Found {instrumentMarkers.Count} instrument markers. Tracking initially enabled: {isTrackingEnabledInitially}");
        }

        private void Start()
        {
            if (instrumentDetailsUI)
            {
                instrumentDetailsUI.Data = null;
                instrumentDetailsUI.gameObject.SetActive(false);
            }
        }

        private void Update()
        {
            ShowNearestMarker();
        }

        private void ShowNearestMarker()
        {
            InstrumentData nearestInstrument = GetNearestInstrumentData();

            if(nearestInstrument != null)
            {
                if (isShowingInstrumentDetails && instrumentDetailsUI && !GetInstrumentMarker(nearestInstrument).PlaceableInstrument.IsWithinTable)
                {
                    instrumentDetailsUI.Data = nearestInstrument;
                    instrumentDetailsUI.gameObject.SetActive(true);
                }
                
                else
                   instrumentDetailsUI.gameObject.SetActive(false);

            }

            else
                instrumentDetailsUI.gameObject.SetActive(false);
        }

        public InstrumentData GetNearestInstrumentData()
        {
            InstrumentData nearestInstrument = null;
            float nearestDistance = float.MaxValue;

            foreach (InstrumentData trackedInstrument in currentlyTrackedInstruments)
            {
                Vector3 pos = GetInstrumentMarker(trackedInstrument).transform.position;
                float distance = Vector3.Distance(Camera.main.transform.position, pos);
                if (distance < nearestDistance)
                {
                    nearestDistance = distance;
                    nearestInstrument = trackedInstrument;
                }
            }

            return nearestInstrument;
        }

        public InstrumentMarker GetNearestInstrumentMarker()
        {
            return GetInstrumentMarker(GetNearestInstrumentData());
        }

        public void EnableAllInstrumentMarkers()
        {
            hasFoundInstrumentOnce = false;
            foreach (var marker in instrumentMarkers)
            {
                marker.EnableTracking();
                marker.OnInstrumentFound.AddListener(OnInstrumentFound);
                marker.OnInstrumentLost.AddListener(OnInstrumentLost);
                marker.OnInstrumentDropped.AddListener(OnInstrumentDropped);
            }
        }

        public void DisableAllInstrumentMarkers()
        {
            foreach (var marker in instrumentMarkers)
            {
                marker.DisableTracking();
                marker.OnInstrumentFound.RemoveListener(OnInstrumentFound);
                marker.OnInstrumentLost.RemoveListener(OnInstrumentLost);
                marker.OnInstrumentDropped.RemoveListener(OnInstrumentDropped);
            }

            if (instrumentDetailsUI)
            {
                instrumentDetailsUI.Data = null;
                instrumentDetailsUI.gameObject.SetActive(false);
            }

            currentlyTrackedInstruments.Clear();
        }

        public InstrumentMarker GetInstrumentMarker(InstrumentData instrumentData)
        {
            return instrumentMarkers.FirstOrDefault(marker => marker.PlaceableInstrument.InstrumentData == instrumentData);
        }

        public void EnableInstrumentMarker(InstrumentData instrumentData)
        {
            var marker = GetInstrumentMarker(instrumentData);
            if (marker == null) return;

            marker.EnableTracking();
            marker.OnInstrumentFound.AddListener(OnInstrumentFound);
            marker.OnInstrumentLost.AddListener(OnInstrumentLost);
            marker.OnInstrumentDropped.AddListener(OnInstrumentDropped);
        }

        public void DisableInstrumentMarker(InstrumentData instrumentData)
        {
            var marker = GetInstrumentMarker(instrumentData);
            if (marker == null) return;

            marker.DisableTracking();
            marker.OnInstrumentFound.RemoveListener(OnInstrumentFound);
            marker.OnInstrumentLost.RemoveListener(OnInstrumentLost);
            marker.OnInstrumentDropped.RemoveListener(OnInstrumentDropped);

            if (currentlyTrackedInstruments.Contains(instrumentData))
                currentlyTrackedInstruments.Remove(instrumentData);

            if (instrumentDetailsUI)
            {
                instrumentDetailsUI.Data = null;
                instrumentDetailsUI.gameObject.SetActive(false);
            }
        }

        public void OnInstrumentFound(InstrumentMarker instrumentMarker)
        {
            var instrumentData = instrumentMarker.Instrument;
            Debug.Log($"Instrument found: {instrumentData.name}");

            InstrumentFound?.Invoke(instrumentMarker);

            hasFoundInstrumentOnce = true;
            nrOfVisibleInstruments++;

            if (!currentlyTrackedInstruments.Contains(instrumentData))
                currentlyTrackedInstruments.Add(instrumentData);

            if (!hasFoundInstrumentOnce || (isPlayingAudioOnInstrumentFound && Time.time - lastInstrumentSeenTime > instrumentFoundAudioClipCooldown))
            {
                AudioSource.PlayClipAtPoint(instrumentFoundAudioClip, Camera.main.transform.position, instrumentFoundAudioClipVolume);
            }
            lastInstrumentSeenTime = Time.time;
        }

        public void OnInstrumentLost(InstrumentMarker instrumentMarker)
        {
            var instrumentData = instrumentMarker.Instrument;
            Debug.Log($"Instrument lost: {instrumentData.name}");

            InstrumentLost?.Invoke(instrumentMarker);

            if (currentlyTrackedInstruments.Contains(instrumentData))
                currentlyTrackedInstruments.Remove(instrumentData);

            nrOfVisibleInstruments--;
            if (nrOfVisibleInstruments < 0)
                nrOfVisibleInstruments = 0;

            if (isPlayingAudioOnInstrumentLost && Time.time - lastInstrumentLostTime > instrumentLostAudioClipCooldown)
            {
                AudioSource.PlayClipAtPoint(instrumentLostAudioClip, Camera.main.transform.position, instrumentLostAudioClipVolume);
            }

            // lastInstrumentSeenTime = Time.time;
            lastInstrumentLostTime = Time.time;
        }

        public void OnInstrumentDropped(InstrumentMarker instrumentMarker)
        {
            var instrumentData = instrumentMarker.Instrument;
            Debug.Log($"Instrument dropped: {instrumentData.name}");

            InstrumentDropped?.Invoke(instrumentMarker);

            if (instrumentDetailsUI && nrOfVisibleInstruments == 0) {
                instrumentDetailsUI.Data = null;
                instrumentDetailsUI.gameObject.SetActive(false);
            }
        }

        public List<InstrumentMarker> FindInstrumentMarkers()
        {
            return GetComponentsInChildren<InstrumentMarker>(includeInactive:true).ToList();
        }

        public void SetInstrumentElementsDisplayed(PlaceableInstrumentElement elementsDisplayed)
        {
            foreach (var marker in instrumentMarkers)
            {
                marker.ElementsDisplayed = elementsDisplayed;
            }
        }
    }
}

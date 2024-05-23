using System;
using System.Threading;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using NMY.OTAToolpicker.UI;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace NMY.OTAToolpicker
{
    public class Level1LearnModeController : MonoBehaviour, ILevelController
    {
        [SerializeField] private OTAAppController app;
        public InstrumentMarkerController MarkerController => app.InstrumentMarkerController;

        [SerializeField] private bool resetSessionEntriesOnStart = true;

        [Tooltip("If true, the instrument details UI will be shown when an instrument is found.")]
        [SerializeField] private bool isShowingInstrumentDetails = true;

        [SerializeField] private PlaceableInstrumentElement elementsDisplayed = PlaceableInstrumentElement.Infospots | PlaceableInstrumentElement.InstrumentRenderer;

        [Header("UI")]
        [FormerlySerializedAs("level1LearnModeInfoDialogUI")]
        [SerializeField] private DialogUI infoDialogUI;
        [SerializeField] private DialogUI waitDialogUI;
        [SerializeField] private Button mainMenuButton;

        [Header("Audio")]
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip introAudioClip;
        [SerializeField] private AudioClip pickupAudioClip;
        [SerializeField] private AudioClip pickupReminderAudioClip;
        [SerializeField] private float pickupReminderIntervalS = 12f;
        [SerializeField] private AudioClip pickupSuccessAudioClip;
        [SerializeField] private AudioClip pickupRepeatAudioClip;

        private CancellationTokenSource levelPlayingCts;
        private CancellationToken ct;

        public bool IsLevelStopRequested { get; set; } = false;
        private bool IsFirstTimeDroppingInstrument { get; set; } = true;
        private bool HasFoundInstrumentOnce { get; set; } = false;

        private OTAMarkerManager markerManager;

        private InstrumentMarker lastInstrumentFound;

        private void Start()
        {
            markerManager = FindObjectOfType<OTAMarkerManager>();
        }

        public async UniTask StartLevelAsync()
        {
            if (levelPlayingCts != null) {
                Debug.LogError("Level1LearnModeController: Already running! This should not happen.");
                return;
            }

            ResetLevel();

            levelPlayingCts = new CancellationTokenSource();
            ct = levelPlayingCts.Token;

            // In Level1 learn mode the marker controller should
            // play an audio SFX when an instrument was found or lost.
            MarkerController.IsPlayingAudioOnInstrumentFound = false;
            MarkerController.IsPlayingAudioOnInstrumentLost = false;
            MarkerController.IsShowingInstrumentDetails = isShowingInstrumentDetails;
            MarkerController.SetInstrumentElementsDisplayed(elementsDisplayed);

            // play audio intro and show info dialog. wait for either to finish
            await HelperTasks.ShowDialogWithAudio(infoDialogUI, audioSource, introAudioClip, shouldWaitForClick: true, ct: ct);

            audioSource.clip = pickupAudioClip;
            audioSource.PlayDelayed(1f);

            // level can only be stopped by pressing the main menu button from this point on.
            // prior to this, cancellation token should not be used.
            mainMenuButton.onClick.AddListener(StopLevel);
            mainMenuButton.gameObject.SetActive(true);

            MarkerController.EnableAllInstrumentMarkers();

            waitDialogUI.Show(ct).Forget();

            // Start monitoring all instruments for tracking.
            // If an instrument is found, call OnInstrumentFound. If an instrument is dropped, call OnInstrumentDropped.
            // When cancellation is requested, stops monitoring the instruments and clears up any connected events.
            // HelperTasks.MonitorInstrumentTracking(MarkerController, OnInstrumentFound, OnInstrumentDropped, 30, ct).Forget();
            HelperTasks.Level1LearnWaitForMarkerAboveThreshold(MarkerController, OnInstrumentFound, ct).Forget();

            Func<InstrumentMarker> lastInstrumentFunc = () => lastInstrumentFound;

            HelperTasks.Level1LearnWaitForMarkerBelowThreshold(MarkerController, lastInstrumentFunc, OnInstrumentDropped, ct).Forget();

            // If no instrument has been inspected for a while, play reminder audio clip.
            // When an instrument was inspected once, stop playing the reminder audio clip.
            HelperTasks.PlayAudioClipIntervalUntil(() => HasFoundInstrumentOnce,
                initialDelayS: pickupAudioClip.length,
                audioSource: audioSource,
                audioClip: pickupReminderAudioClip,
                intervalS: pickupReminderIntervalS,
                ct
            ).Forget();

            // if we do not wait here, flow will instantly continue with the menu again
            await UniTask.WaitUntil(() => IsLevelStopRequested);
        }

        public void StopLevel()
        {
            levelPlayingCts.Cancel();
            levelPlayingCts.Dispose();
            levelPlayingCts = null;

            audioSource.Stop();

            infoDialogUI.Hide();
            waitDialogUI.Hide();

            mainMenuButton.onClick.RemoveListener(StopLevel);
            mainMenuButton.gameObject.SetActive(false);

            MarkerController.DisableAllInstrumentMarkers();

            if (app.Session.Level1Learn.NrOfIdentifiedInstruments>0)
                app.Session.AddCompletedLevel(LevelMode.Level1LearnMode);

            IsLevelStopRequested = true;
            Debug.Log("Level1LearnModeController::StopLevel: Level stopped.");
        }

        public void ResetLevel()
        {
            IsLevelStopRequested = false;
            IsFirstTimeDroppingInstrument = true;
            HasFoundInstrumentOnce = false;
            if (resetSessionEntriesOnStart) app.Session.Level1Learn.Reset();

            mainMenuButton.gameObject.SetActive(false);
        }

        private void OnInstrumentFound(InstrumentMarker instrumentMarker)
        {
            // the first time an instrument is found, play the pickup audio clip
            if (!HasFoundInstrumentOnce)
            {
                HasFoundInstrumentOnce = true;
                HelperTasks.PlayOneShot(audioSource, pickupSuccessAudioClip);
            }

            

            if (lastInstrumentFound!=null) return;

            lastInstrumentFound = instrumentMarker;


            Debug.Log($"Level1 OnInstrumentFound: instrumentMarker={instrumentMarker.gameObject.name}, lastInstrumentIdentified={lastInstrumentFound?.gameObject.name}");

            waitDialogUI.Hide();

            instrumentMarker.UpdateInstrumentElementVisibility(PlaceableInstrumentElement.Infospots | PlaceableInstrumentElement.InstrumentRenderer);
            MarkerController.InstrumentDetailsUI.Data = instrumentMarker.Instrument;
            MarkerController.InstrumentDetailsUI.gameObject.SetActive(true);

            app.Session.Level1Learn.AddIdentifiedInstrument(instrumentMarker.Instrument);
        }

        private void OnInstrumentDropped(InstrumentMarker instrumentMarker)
        {
            if (markerManager.currentTrackedObjectsCount == 0)
                waitDialogUI.Show(ct).Forget();

            
            if (instrumentMarker != lastInstrumentFound) return;

            lastInstrumentFound = null;

            Debug.Log($"Level1 OnInstrumentDropped: instrumentMarker={instrumentMarker.gameObject.name}, lastInstrumentFound={lastInstrumentFound?.gameObject.name}");

            // instrumentMarker.UpdateInstrumentElementVisibility(PlaceableInstrumentElement.None);
            foreach(InstrumentMarker marker in MarkerController.InstrumentMarkers)
                marker.UpdateInstrumentElementVisibility(PlaceableInstrumentElement.None);

            MarkerController.InstrumentDetailsUI.gameObject.SetActive(false);

            if (IsFirstTimeDroppingInstrument)
            {
                IsFirstTimeDroppingInstrument = false;
                HelperTasks.PlayOneShot(audioSource, pickupRepeatAudioClip);
            }
        }      

    }
}

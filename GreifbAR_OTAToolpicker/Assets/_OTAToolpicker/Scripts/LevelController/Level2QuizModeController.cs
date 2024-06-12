using Cysharp.Threading.Tasks;
using NMY.OTAToolpicker.UI;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Animations;

namespace NMY.OTAToolpicker
{
    public class Level2QuizModeController : MonoBehaviour, ILevelController
    {
        [SerializeField] private OTAAppController app;
        public InstrumentMarkerController MarkerController => app.InstrumentMarkerController;

        // [SerializeField] private bool resetSessionEntriesOnStart = true;
        private bool isLevelStopRequested = false;
        public bool IsLevelStopRequested
        {
            get => isLevelStopRequested;
            set => isLevelStopRequested = value;
        }

        [Header("Quiz settings")]
        [SerializeField] private List<InstrumentData> instrumentsRequired = new();
        [SerializeField] private int nrOfErrorsAllowed = 1;

        [SerializeField] private PlaceableInstrumentElement elementsDisplayed = PlaceableInstrumentElement.Collider;

        // [Header("Tracking")]
        // [SerializeField] private TableMarker tableMarker;
        // [Tooltip("If <b>true</b>, the user has to click the button in the table marker calibration dialog to accept the calibration (Vuforia). If <b>false</b>, the calibration will be accepted when the table marker is found (Varjo).")]
        // [SerializeField] private bool shouldWaitForTableCalibrationClick = true;

        [Header("UI")]
        [Tooltip("If true, the instrument details UI will be shown when an instrument is found.")]
        [SerializeField] private bool isShowingInstrumentDetails = true;
        [SerializeField] private DialogUI welcomeDialogUI;
        [SerializeField] private DialogUI warningDialogUI;
        [SerializeField] private DialogUI confirmationDialogUI;
        [Tooltip("The position offset of the confirmation dialog from the instrument marker.")]
        [SerializeField] private Vector3 confirmationDialogPositionOffset = new Vector3(1f, 0.1f, 0);
        [SerializeField] private DialogUI infoDialogUI;
        [SerializeField] private DialogUI errorDialogUI;
        [SerializeField] private Button backButton;

        // [SerializeField] private bool isConfirmationDialogAttached = true;
        // public bool IsConfirmationDialogAttached => isConfirmationDialogAttached;
        // [SerializeField] private Vector3 confirmationDialogAttachDelta = new Vector3(1f, 0.1f, 0);
        // public Vector3 ConfirmationDialogAttachDelta => confirmationDialogAttachDelta;

        [Header("Audio source")]
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip introAudioClip;
        [SerializeField] private AudioClip warningAudioClip;
        [SerializeField] private AudioClip explanationAudioClip;
        [SerializeField] private AudioClip reminderAudioClip;
        [SerializeField] private float reminderIntervalS = 10f;
        [SerializeField] private AudioClip taskAudioClip;
        [SerializeField] private AudioClip nextInstrumentAudioClip;
        [SerializeField] private AudioClip resultsAudioClip;
        [SerializeField] private AudioClip successAudioClip;
        [SerializeField] private AudioClip failureAudioClip;

        [Header("Debug")]
        [SerializeField] private bool shouldSkipIntro = false;

        private CancellationTokenSource levelPlayingCts;

        private List<InstrumentData> instrumentsIdentified = new();
        public List<PlacementResult> placementResults = new();

        private InstrumentTable instrumentTable;

        private PositionConstraint positionConstraint;

        void Awake()
        {
            instrumentTable = app.InstrumentTable;
        }

        async public UniTask StartLevelAsync()
        {
            if (levelPlayingCts != null) {
                Debug.LogError("Level2QuizModeController: Already running!");
                return;
            }

            IsLevelStopRequested = false;

            levelPlayingCts = new CancellationTokenSource();
            CancellationToken ct = levelPlayingCts.Token;

            // Level 2 quiz does not have any Session entries
            // if (resetSessionEntriesOnStart) app.Session.Level2Quiz.Reset();

            if (!shouldSkipIntro)
            {
                // play audio intro and show info dialog. wait for any of them to finish first.
                await HelperTasks.ShowDialogWithAudio(welcomeDialogUI, audioSource, introAudioClip, ct: ct);

                // if the user has not completed the learn mode yet, show warning dialog
                if (app.Session.AppMode==AppMode.TeachMode && !app.Session.CompletedLevels.Contains(LevelMode.Level2LearnMode))
                {
                    // play warning audio and show warning dialog. wait for either to finish
                    await UniTask.Delay(500, cancellationToken: ct);
                    DialogButton button = await HelperTasks.ShowDialogWithAudio(warningDialogUI, audioSource, warningAudioClip, shouldWaitForClick: true, ct: ct);

                    // if the user selects "continue" flow will continue, otherwise stop the level
                    if (button == DialogButton.Secondary) {
                        StopLevel();
                        return;
                    }
                    audioSource.Stop();
                }
            }

            ShowBackButton();

            // --- Table calibration ---
            instrumentTable.SetPlacementVolumeDisplayedElements(PlacementVolumeElement.Collider);
            instrumentTable.DisableAllPlacementVolumes();
            instrumentTable.gameObject.SetActive(true);

            if (!app.IsTableCalibrated)
            {
                await HelperTasks.CalibrateTable(app.TableMarker, instrumentTable, app.TableCalibrationDialogUI, app.ShouldWaitForTableCalibrationClick, ct);
                if (ct.IsCancellationRequested) return;
            }

            if (!shouldSkipIntro) {
                await HelperTasks.PlayAndWaitForAudioClip(audioSource, explanationAudioClip, ct);
                if (ct.IsCancellationRequested) return;
            }

            MarkerController.gameObject.SetActive(true);
            MarkerController.IsShowingInstrumentDetails = isShowingInstrumentDetails;
            MarkerController.SetInstrumentElementsDisplayed(elementsDisplayed);
            MarkerController.EnableAllInstrumentMarkers();

            AddPositionConstraintToConfirmationDialog(MarkerController.transform);

            while (!AreAllRequiredInstrumentsIdentified() && !ct.IsCancellationRequested)
            {
                MarkerController.IsPlayingAudioOnInstrumentLost = true;

                InstrumentMarker instrumentMarker = null;
                if (app.TrackingHardware == TrackingHardware.Vuforia) {
                    instrumentMarker = await HelperTasks.WaitForAnyInstrumentIdentification(
                        markerController: MarkerController,
                        audioSource: audioSource,
                        reminderAudioClip: reminderAudioClip,
                        reminderIntervalS: reminderIntervalS,
                        ct: ct
                    );
                }
                else if (app.TrackingHardware == TrackingHardware.Varjo) {
                    instrumentMarker = await HelperTasks.WaitForAnyCloseInstrumentIdentification(
                        markerController: MarkerController,
                        audioSource: audioSource,
                        reminderAudioClip: reminderAudioClip,
                        reminderIntervalS: reminderIntervalS,
                        ct: ct
                    );
                }
                audioSource.Stop();
                if (ct.IsCancellationRequested) return;

                SetConfirmationDialogPositionConstraintSource(instrumentMarker.transform);

                string instrumentTitle = instrumentMarker.Instrument.Title.GetLocalizedString();

                // if the instrument is not configured for placement (has no PlacementVolume on the table), ignore it
                if (!instrumentTable.GetPlacementVolume(instrumentMarker.PlaceableInstrument))
                {
                    Debug.LogWarning($"Instrument <b>{instrumentTitle}</b> has no PlacementVolume on the table. Ignoring it.");
                    await errorDialogUI.Show("Fehler", $"Das Instrument <b>{instrumentTitle}</b> ist nicht für die Ablage konfiguriert. Fahre mit einem anderen Instrument fort.", ct);
                    continue;
                }

                if (!instrumentsRequired.Contains(instrumentMarker.Instrument))
                {
                    Debug.LogWarning($"Instrument <b>{instrumentTitle}</b> is not required for this level. Ignoring it.");
                    await errorDialogUI.Show("Fehler", $"Das Instrument <b>{instrumentTitle}</b> ist nicht für dieses Level erforderlich. Fahre mit einem anderen Instrument fort oder füge das Instrument im Unity Editor zu Level 2 hinzu.", ct);
                    continue;
                }

                using CancellationTokenSource instrumentMonitorCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
                HelperTasks.MonitorInstrumentTracking(instrumentMarker.Instrument, MarkerController,
                    onTracked: () => {
                        confirmationDialogUI.SetPrimaryButtonInteractable(true);
                        confirmationDialogUI.SetPrimaryButtonText($"Weiter mit <b>{instrumentTitle}</b>");
                        instrumentTable.EnablePlacementVolume(instrumentMarker.PlaceableInstrument);
                    },
                    onNotTracked: () => {
                        confirmationDialogUI.SetPrimaryButtonInteractable(false);
                        confirmationDialogUI.SetPrimaryButtonText($"Warte auf <b>{instrumentTitle}</b>...");
                        instrumentTable.DisablePlacementVolume(instrumentMarker.PlaceableInstrument);
                    },
                    ct: instrumentMonitorCts.Token
                ).Forget();

                // the following reminder audio plays every reminderIntervalS until
                // the user clicks the primary button in the confirmation dialog.
                // It can also be cancelled by the reminderMonitorCts.
                using CancellationTokenSource reminderCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
                DialogButton dialogButton = DialogButton.None;
                HelperTasks.PlayAudioClipIntervalUntil(() => dialogButton==DialogButton.Primary,
                    initialDelayS: 0f,
                    audioSource: audioSource,
                    audioClip: taskAudioClip,
                    intervalS: reminderIntervalS,
                    ct: reminderCts.Token
                ).Forget();

                // show the placement confirmation dialog
                dialogButton = await HelperTasks.ShowDialogWithAudio(confirmationDialogUI, audioSource, taskAudioClip, shouldWaitForClick: true, ct: ct);
                if (ct.IsCancellationRequested) return;

                instrumentTable.CheckPlacement();
                PlacementVolume placementVolume = instrumentTable.GetPlacementVolume(instrumentMarker.PlaceableInstrument);

                if (!instrumentsIdentified.Contains(instrumentMarker.Instrument))
                {
                    instrumentsIdentified.Add(instrumentMarker.Instrument);

                    Debug.Log($"Identified: {instrumentMarker}, position: {instrumentMarker.PlaceableInstrument.IsWithinPlacementVolume}, direction: {instrumentMarker.PlaceableInstrument.IsDirectionValid}");
                    PlacementResult placementResult = new PlacementResult(
                        instrumentMarker.PlaceableInstrument,
                        isIntersectingTableBorder: instrumentMarker.PlaceableInstrument.IsIntersectingTableBorder,
                        isPositionValid: instrumentMarker.PlaceableInstrument.IsWithinPlacementVolume,
                        isDirectionValid: placementVolume.IsDirectionCheckEnabled ? instrumentMarker.PlaceableInstrument.IsDirectionValid : true
                    );
                    // play "get next instrument" audio
                    if (!AreAllRequiredInstrumentsIdentified())
                        audioSource.PlayOneShot(nextInstrumentAudioClip);

                    // store the result (instrument identified and placed correctly or not)
                    placementResults.Add(placementResult);
                    // app.Session.Level2Quiz.AddPlacementResult(placementResult);
                }

                MarkerController.IsPlayingAudioOnInstrumentLost = false;
                MarkerController.DisableInstrumentMarker(instrumentMarker.Instrument);
                instrumentTable.DisablePlacementVolume(instrumentMarker.PlaceableInstrument);

                reminderCts.Cancel();
                instrumentMonitorCts.Cancel();
            }

            Debug.Log("All required instruments identified! instrumentIdentified.Count=" + instrumentsIdentified.Count);

            foreach(var instrument in instrumentsIdentified)
            {
                PlacementVolume placementVolume = instrumentTable.GetPlacementVolume(instrument);
                PlaceableInstrument placeableInstrument = placementVolume.PlaceableInstrument;
                PlacementResult result = placementResults.Find(pr => pr.placeableInstrument == placeableInstrument);
                placeableInstrument.ResultView.SetResult(result);
                placeableInstrument.ResultView.Show();
            }

            audioSource.PlayOneShot(resultsAudioClip);

            // if we do not wait here, flow will instantly continue with the menu again
            await UniTask.WaitUntil(() => IsLevelStopRequested);
        }

        public void StopLevel()
        {
            levelPlayingCts?.Cancel();
            levelPlayingCts?.Dispose();
            levelPlayingCts = null;

            audioSource.Stop();

            MarkerController.DisableAllInstrumentMarkers();

            // explicitly hide all result views of the identified instruments
            foreach(var instrument in instrumentsIdentified)
            {
                PlaceableInstrument placeableInstrument = instrumentTable.GetPlacementVolume(instrument).PlaceableInstrument;
                placeableInstrument.ResultView.Hide();
            }

            instrumentTable.DisableAllPlacementVolumes();
            instrumentTable.gameObject.SetActive(false);

            infoDialogUI.Hide();
            welcomeDialogUI.Hide();
            confirmationDialogUI?.Hide();
            warningDialogUI.Hide();
            errorDialogUI.Hide();
            HideBackButton();

            RemovePositionConstraintFromConfirmationDialog();

            placementResults.Clear();

            IsLevelStopRequested = true;
        }

        public void ResetLevel()
        {
            MarkerController.DisableAllInstrumentMarkers();

            instrumentTable.DisableAllPlacementVolumes();
            instrumentTable.gameObject.SetActive(false);

            welcomeDialogUI.Hide();
            confirmationDialogUI?.Hide();
            warningDialogUI.Hide();
            HideBackButton();

            placementResults.Clear();
        }

        private void ShowBackButton()
        {
            backButton.onClick.AddListener(StopLevel);
            backButton.gameObject.SetActive(true);
        }

        private void HideBackButton()
        {
            backButton.onClick.RemoveListener(StopLevel);
            backButton.gameObject.SetActive(false);
        }

        public bool AreAllRequiredInstrumentsIdentified()
        {
            // This checks whether there are any elements in b which aren't in a - and then inverts the result.
            // https://stackoverflow.com/questions/1520642/does-net-have-a-way-to-check-if-list-a-contains-all-items-in-list-b
            return !instrumentsRequired.Except(instrumentsIdentified).Any();
        }

        public void AddPositionConstraintToConfirmationDialog(Transform sourceTransform)
        {
            positionConstraint = confirmationDialogUI.gameObject.AddComponent<PositionConstraint>();
            positionConstraint.locked = false;
            positionConstraint.translationOffset = confirmationDialogPositionOffset;
            positionConstraint.AddSource(new ConstraintSource { sourceTransform = sourceTransform, weight = 1 });
            positionConstraint.constraintActive = true;
        }

        public void RemovePositionConstraintFromConfirmationDialog()
        {
            if (positionConstraint != null)
            {
                Destroy(positionConstraint);
                positionConstraint = null;
            }
        }

        public void SetConfirmationDialogPositionConstraintSource(Transform sourceTransform)
        {
            if (positionConstraint != null)
            {
                ConstraintSource constraintSource = positionConstraint.GetSource(0);
                constraintSource.sourceTransform = sourceTransform;
                positionConstraint.SetSource(0, constraintSource);

                positionConstraint.translationOffset = confirmationDialogPositionOffset;
            }
            else
            {
                Debug.LogWarning("PositionConstraint is null. Cannot set source.");
            }
        }
    }
}

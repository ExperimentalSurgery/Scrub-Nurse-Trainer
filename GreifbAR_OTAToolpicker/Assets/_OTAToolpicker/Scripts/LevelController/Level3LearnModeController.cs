using Cysharp.Threading.Tasks;
using Leap.Unity;
using NMY.OTAToolpicker.UI;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using UnityEngine;
using UnityEngine.Localization.SmartFormat.PersistentVariables;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace NMY.OTAToolpicker
{
    public class Level3LearnModeController : MonoBehaviour, ILevelController
    {
        [SerializeField] private OTAAppController app;
        public InstrumentMarkerController MarkerController => app.InstrumentMarkerController;

        [SerializeField] private HandPoseDetector handPoseDetector;
        [FormerlySerializedAs("holdDuration")]
        [SerializeField] private float requiredHoldDurationS = 3f;

        // [SerializeField] private bool resetSessionEntriesOnStart = true;
        [Tooltip("If true, the instrument details UI will be shown when an instrument is found.")]
        [SerializeField] private bool isShowingInstrumentDetails = false;

        [Tooltip("The instruments required to be handed over in this level.")]
        [SerializeField] private List<InstrumentData> handoversRequired = new();

        [Header("UI")]
        [SerializeField] private DialogUI infoDialogUI;
        [SerializeField] private DialogUI placeholderDialogUI;
        [SerializeField] private DialogUI waitingDialogUI;
        [SerializeField] private DialogUI readyDialogUI;
        [SerializeField] private DialogUI resultDialogUI;
        [SerializeField] private Button mainMenuButton;
        [SerializeField] private ProgressDialog progressDialogUI;
        [SerializeField] private UnityEngine.UI.Image exampleHandPoseImage;

        [Header("Audio")]
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip introAudioClip;
        [SerializeField] private AudioClip explanationAudioClip;
        [SerializeField] private AudioClip pickupAudioClip;
        [SerializeField] private AudioClip pickupReminderAudioClip;
        [SerializeField] private float pickupReminderIntervalS = 12f;
        [SerializeField] private AudioClip scanAudioClip;
        [SerializeField] private AudioClip scanReminderAudioClip;
        [SerializeField] private float scanReminderIntervalS = 12f;
        [SerializeField] private AudioClip taskAudioClip;
        [SerializeField] private AudioClip taskReminderAudioClip;
        [SerializeField] private float taskReminderIntervalS = 12f;
        [SerializeField] private AudioClip poseHoldAudioClip;
        [FormerlySerializedAs("dropHoldAudioClip")]
        [SerializeField] private AudioClip dropAudioClip;
        [FormerlySerializedAs("repeatHoldAudioClip")]
        [SerializeField] private AudioClip repeatAudioClip;

        private CancellationTokenSource levelPlayingCts;
        private CancellationToken ct;

        // public bool IsLevelStopRequested { get; set; } = false;
        private bool isLevelStopRequested = false;

        private List<InstrumentData> handoversCompleted = new();

        public async UniTask StartLevelAsync()
        {
            if (levelPlayingCts != null) {
                Debug.LogError("Level3LearnModeController: Already running! This should not happen.");
                return;
            }

            ResetLevel();

            levelPlayingCts = new CancellationTokenSource();
            ct = levelPlayingCts.Token;

            MarkerController.gameObject.SetActive(true);
            MarkerController.IsPlayingAudioOnInstrumentFound = true;
            MarkerController.IsPlayingAudioOnInstrumentLost = true;
            MarkerController.IsShowingInstrumentDetails = isShowingInstrumentDetails;
            MarkerController.SetInstrumentElementsDisplayed(PlaceableInstrumentElement.Infospots);

            // "Willkommen in Level 3.1:Anreichen - Lernmodus"
            await HelperTasks.ShowDialogWithAudio(infoDialogUI, audioSource, introAudioClip, ct: ct);

            // level can only be stopped by pressing the main menu button from this point on.
            // prior to this, cancellation token should not be used or will have no effect.
            mainMenuButton.onClick.AddListener(StopLevel);
            mainMenuButton.gameObject.SetActive(true);

            // "Gut, jetzt beginnt der Chirurg, Instrumente anzufordern"
            await HelperTasks.PlayAndWaitForAudioClip(audioSource, explanationAudioClip, ct: ct);
            if (ct.IsCancellationRequested) return;

            // "Nimm dir ein beliebiges Instrument, um zu lernen, wie man es korrekt anreicht"
            audioSource.PlayOneShot(pickupAudioClip);

            MarkerController.EnableAllInstrumentMarkers();

            while (!AreAllRequiredInstrumentsHandedOver() && !ct.IsCancellationRequested)
            {
                CancellationTokenSource waitingCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
                waitingDialogUI.Show(waitingCts.Token).Forget();

                // "Reminder: Hebe einen beliebigen Gegenstand auf"
                InstrumentMarker instrumentMarkerFound = await HelperTasks.WaitForAnyInstrumentIdentification(MarkerController, audioSource, pickupReminderAudioClip, pickupReminderIntervalS, ct);
                waitingCts.Cancel();
                if (ct.IsCancellationRequested) return;

                HandoverData handoverData = instrumentMarkerFound.Instrument.HandoverData;
                if (handoverData == null) {
                    Debug.LogError("Level3LearnModeController::StartLevelAsync: Handover data not found for instrument.");
                    continue;
                }

                // "Reiche das Instrument jetzt so an, wie es die Beispielhand vormacht. Halte deine Hand dann kurz in der richtigen Endpose"
                HelperTasks.PlayOneShot(audioSource, taskAudioClip);

                ShowDemoHand(handoverData);
                ShowSurgeon(handoverData);

                // due to the using keyword, the cancellation token source will be disposed when scope is left (while loop in this case)
                using CancellationTokenSource holdReminderCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
                // "Reminder: Halte das Instrument eine kurze Zeit lang in die Hand des Chirurgen, wie es die Demo-Hand vormacht"
                HelperTasks.PlayReminderAudioClip(taskAudioClip.length+taskReminderIntervalS, taskReminderIntervalS, audioSource, taskReminderAudioClip, ct: holdReminderCts.Token).Forget();

                app.StartLeapHandTracking();
                handPoseDetector.enabled = true;

                (progressDialogUI.Title["duration"] as FloatVariable).Value = requiredHoldDurationS;
                (progressDialogUI.HoldHandPoseString["duration"] as FloatVariable).Value = requiredHoldDurationS;
                progressDialogUI.Show(requiredHoldDurationS, ct).Forget();

                // await WaitForHandPose(handPoseDetector, handoverData, ct);
                float poseDurationS = await HelperTasks.MonitorHandPoseHold(app.LeapProvider,
                    handPoseDetector,
                    handoverData: handoverData,
                    durationS: 0.1f,
                    onPoseDetected: () => { progressDialogUI.StartTimer(); progressDialogUI.Status = ProgressDialogStatus.PoseDetected; },
                    onPoseLost: () => { progressDialogUI.StopTimer(); progressDialogUI.ResetTimer(); },
                    onHandLost: () => progressDialogUI.Status = ProgressDialogStatus.HandNotVisible,
                    onHandVisible: () => progressDialogUI.Status = ProgressDialogStatus.HandVisible,
                    ct: ct
                );

                holdReminderCts.Cancel();
                Debug.Log("Level3LearnModeController::StartLevelAsync: Initial hand pose detected.");

                // "Halte diese Pose kurz"
                HelperTasks.PlayOneShot(audioSource, poseHoldAudioClip);

                poseDurationS = await HelperTasks.MonitorHandPoseHold(app.LeapProvider,
                    handPoseDetector,
                    handoverData: handoverData,
                    durationS: requiredHoldDurationS,
                    onPoseDetected: () => { progressDialogUI.StartTimer(); progressDialogUI.Status = ProgressDialogStatus.PoseDetected; },
                    onPoseLost: () => { progressDialogUI.StopTimer(); progressDialogUI.ResetTimer(); },
                    onHandLost: () => progressDialogUI.Status = ProgressDialogStatus.HandNotVisible,
                    onHandVisible: () => progressDialogUI.Status = ProgressDialogStatus.HandVisible,
                    ct: ct
                );

                HideDemoHand();
                HideSurgeon();

                if (ct.IsCancellationRequested) return;

                if (MarkerController.IsCurrentlyTracked(instrumentMarkerFound.Instrument))
                {
                    // "Gut, das hat geklappt. Lege das Instrument jetzt zurück."
                    HelperTasks.PlayOneShot(audioSource, dropAudioClip);

                    (readyDialogUI.Message["instrumentTitle"] as StringVariable).Value = instrumentMarkerFound.Instrument.Title.GetLocalizedString();
                    UniTask showReadyDialogTask = readyDialogUI.Show(ct);
                    UniTask droppedInstrumentTask = HelperTasks.WaitForInstrumentDropped(MarkerController, instrumentMarkerFound, readyDialogUI, ct);
                    await UniTask.WhenAll(droppedInstrumentTask, showReadyDialogTask);
                    if (ct.IsCancellationRequested) return;
                }

                handoversCompleted.Add(instrumentMarkerFound.Instrument);

                // "Gut, du kannst dir jetzt gerne noch weitere Instrumente ausprobieren..."
                if (!AreAllRequiredInstrumentsHandedOver()) {
                    HelperTasks.PlayOneShot(audioSource, repeatAudioClip);
                }
            }

            MarkerController.DisableAllInstrumentMarkers();

            mainMenuButton.gameObject.SetActive(false);

            DialogButton dialogButton = await HelperTasks.ShowDialog(resultDialogUI);
            if (dialogButton == DialogButton.Primary)
                StopLevel();
            else {
                // restart the level
                StopLevel();
                ResetLevel();
                await StartLevelAsync();
            }

            // if we do not wait here, flow will instantly return to the menu again
            await UniTask.WaitUntil(() => isLevelStopRequested);
        }

        public void StopLevel()
        {
            levelPlayingCts.Cancel();
            levelPlayingCts.Dispose();
            levelPlayingCts = null;

            mainMenuButton.onClick.RemoveListener(StopLevel);
            mainMenuButton.gameObject.SetActive(false);

            audioSource.Stop();

            infoDialogUI.Hide();
            placeholderDialogUI.Hide();
            readyDialogUI.Hide();
            waitingDialogUI.Hide();
            resultDialogUI.Hide();
            progressDialogUI.Hide();

            MarkerController.IsPlayingAudioOnInstrumentLost = false;
            MarkerController.DisableAllInstrumentMarkers();

            handPoseDetector.enabled = false;
            app.StopLeapHandTracking();

            HideDemoHand();
            HideSurgeon();

            isLevelStopRequested = true;
            Debug.Log("Level3LearnModeController::StopLevel: Level stop requested.");
        }

        public void ResetLevel()
        {
            isLevelStopRequested = false;
            handoversCompleted.Clear();

            infoDialogUI.Hide();
            placeholderDialogUI.Hide();
            readyDialogUI.Hide();
        }

        private void ShowDemoHand(HandoverData handoverData)
        {
            Debug.Log("Level3LearnModeController::ShowDemoHand: Showing demo hand.");
            exampleHandPoseImage.gameObject.SetActive(true);
            exampleHandPoseImage.sprite = handoverData.ExampleImage;
        }

        private void HideDemoHand()
        {
            Debug.Log("Level3LearnModeController::HideDemoHand: Hiding demo hand.");
            exampleHandPoseImage.gameObject.SetActive(false);
        }

        private void ShowSurgeon(HandoverData handoverData)
        {
            Debug.Log("Level3LearnModeController::ShowSurgeon: Showing surgeon.");
        }

        private void HideSurgeon()
        {
            Debug.Log("Level3LearnModeController::HideSurgeon: Hiding surgeon.");
        }

        private bool AreAllRequiredInstrumentsHandedOver()
        {
            // This checks whether there are any elements in b which aren't in a - and then inverts the result.
            // https://stackoverflow.com/questions/1520642/does-net-have-a-way-to-check-if-list-a-contains-all-items-in-list-b
            return !handoversRequired.Except(handoversCompleted).Any();
        }

    }
}

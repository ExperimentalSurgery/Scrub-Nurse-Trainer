using Cysharp.Threading.Tasks;
using Leap;
using Leap.Unity;
using NMY.OTAToolpicker.UI;
using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Localization.SmartFormat.PersistentVariables;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace NMY.OTAToolpicker
{
    public class Level3QuizModeController : MonoBehaviour, ILevelController
    {
        [SerializeField] private OTAAppController app;
        public InstrumentMarkerController MarkerController => app.InstrumentMarkerController;

        [SerializeField] private HandPoseDetector handPoseDetector;
        [SerializeField] private float requiredHoldDurationS = 3f;

        // [SerializeField] private bool resetSessionEntriesOnStart = true;
        [Tooltip("If true, the instrument details UI will be shown when an instrument is found.")]
        [SerializeField] private bool isShowingInstrumentDetails = false;

        [Tooltip("The instruments required to be handed over in this level.")]
        [SerializeField] private List<InstrumentData> handoversRequired = new();

        [SerializeField] private int nrOfRequiredCorrectHandovers = 1;

        [Header("UI")]
        [FormerlySerializedAs("infoDialogUI")]
        [SerializeField] private DialogUI introDialogUI;
        [SerializeField] private DialogUI warningDialogUI;
        [SerializeField] private DialogUI waitDialogUI;
        [SerializeField] private DialogUI placeholderDialogUI;
        [SerializeField] private DialogUI readyDialogUI;
        [SerializeField] private DialogUI resultsDialogUI;
        [SerializeField] private Button mainMenuButton;
        [SerializeField] private ProgressDialog progressDialogUI;
        [SerializeField] private UnityEngine.UI.Image exampleHandPoseImage;

        [Header("Audio")]
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip introAudioClip;
        [SerializeField] private AudioClip warningAudioClip;
        [SerializeField] private AudioClip explanationAudioClip;
        [SerializeField] private AudioClip scanAudioClip;
        [SerializeField] private AudioClip scanReminderAudioClip;
        [SerializeField] private float scanReminderIntervalS = 12f;
        [SerializeField] private AudioClip taskAudioClip;
        [SerializeField] private AudioClip taskReminderAudioClip;
        [SerializeField] private float taskReminderIntervalS = 12f;
        [SerializeField] private AudioClip poseHoldAudioClip;
        [SerializeField] private AudioClip wrongInstrumentAudioClip;
        [SerializeField] private AudioClip wrongAudioClip;
        [SerializeField] private AudioClip correctAudioClip;
        [SerializeField] private AudioClip nextAudioClip;
        [SerializeField] private AudioClip resultsAudioClip;
        [SerializeField] private AudioClip successAudioClip;
        [SerializeField] private AudioClip failureAudioClip;
        [SerializeField] private AudioClip dropAudioClip;

        private CancellationTokenSource levelPlayingCts;
        private CancellationToken ct;

        private int nrOfCorrectHandovers = 0;

        public bool IsLevelStopRequested { get; set; } = false;

        private List<InstrumentData> handoversCompleted = new();

        public async UniTask StartLevelAsync()
        {
            if (levelPlayingCts != null) {
                Debug.LogError("Level3QuizModeController: Already running! This should not happen.");
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

            // "Willkommen in Level 3.2:Anreichen - Quizmodus"
            await HelperTasks.ShowDialogWithAudio(introDialogUI, audioSource, introAudioClip, ct: ct);

            // if the user has not completed the learn mode yet, show warning dialog
            if (app.Session.AppMode==AppMode.TeachMode && !app.Session.CompletedLevels.Contains(LevelMode.Level3LearnMode))
            {
                // play warning audio and show warning dialog. wait for either to finish
                await UniTask.Delay(500, cancellationToken: ct);
                // "Hinweis: Quizmodus erfordert Vorkenntnisse. Du solltest den Lernmodus vorher einmal gespielt haben. Fortfahren?"
                DialogButton button = await HelperTasks.ShowDialogWithAudio(warningDialogUI, audioSource, warningAudioClip, shouldWaitForClick: true, ct: ct);

                // if the user selects "continue" flow will continue, otherwise stop the level
                if (button == DialogButton.Secondary) {
                    StopLevel();
                    return;
                }
                audioSource.Stop();
            }

            // level can only be stopped by pressing the main menu button from this point on.
            // prior to this, cancellation token should not be used.
            mainMenuButton.onClick.AddListener(StopLevel);
            mainMenuButton.gameObject.SetActive(true);

            // "Gut, jetzt beginnt der Chirurg, bestimmte Instrumente anzufordern"
            await HelperTasks.PlayAndWaitForAudioClip(audioSource, explanationAudioClip, ct: ct);
            if (ct.IsCancellationRequested) return;

            MarkerController.EnableAllInstrumentMarkers();

            foreach(InstrumentData instrumentData in handoversRequired)
            {
                if (ct.IsCancellationRequested) break;

                // "Nimm dir das angezeigte Instrument und halte es in die Kamera, um es zu scannen."
                HelperTasks.PlayOneShot(audioSource, scanAudioClip);

                (waitDialogUI.Message["instrumentTitle"] as StringVariable).Value = instrumentData.Title.GetLocalizedString();
                using CancellationTokenSource waitingCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
                HelperTasks.ShowDialog(waitDialogUI, ct: waitingCts.Token).Forget();

                InstrumentMarker instrumentMarkerFound = await HelperTasks.WaitForSpecificInstrument(MarkerController, instrumentData, audioSource, wrongInstrumentAudioClip, scanReminderAudioClip, ct: ct);
                waitingCts.Cancel();

                if (ct.IsCancellationRequested) return;

                HandoverData handoverData = instrumentMarkerFound.Instrument.HandoverData;
                if (handoverData == null) {
                    Debug.LogError("Level3LearnModeController::StartLevelAsync: Handover data not found for instrument.");
                    continue;
                }

                // "Reiche das Instrument jetzt korrekt an. Halte deine Hand dann kurz in der richtigen Endpose"
                HelperTasks.PlayOneShot(audioSource, taskAudioClip);

                ShowDemoHand(handoverData);
                ShowSurgeon();

                // due to the using keyword, the cancellation token source will be disposed when scope is left (while loop in this case)
                using CancellationTokenSource holdReminderCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
                // "Reminder: Halte das Instrument eine kurze Zeit lang in die Hand des Chirurgen, wie es die Demo-Hand vormacht"
                HelperTasks.PlayReminderAudioClip(taskAudioClip.length+taskReminderIntervalS, taskReminderIntervalS, audioSource, taskReminderAudioClip, ct: holdReminderCts.Token).Forget();

                app.StartLeapHandTracking();
                handPoseDetector.enabled = true;

                (progressDialogUI.Title["duration"] as FloatVariable).Value = requiredHoldDurationS;
                (progressDialogUI.HoldHandPoseString["duration"] as FloatVariable).Value = requiredHoldDurationS;
                progressDialogUI.Show(requiredHoldDurationS, ct).Forget();

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

                // this is a placeholder dialog, it will be replaced by the hand/pose detection
                // DialogButton button = await placeholderDialogUI.Show(ct);
                // if (button == DialogButton.Primary)

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
            }

            MarkerController.DisableAllInstrumentMarkers();

            // deactivate the back button, as there will be one in the result dialog
            // which is displayed next
            mainMenuButton.gameObject.SetActive(false);

            // while the audio clip is playing, we cannot cancel the level or return to the main menu
            audioSource.Stop();
            await HelperTasks.PlayAndWaitForAudioClip(audioSource, resultsAudioClip, ct: ct);
            if (ct.IsCancellationRequested) return;

            // play "passed" or "failed" audio clip
            audioSource.Stop();
            if (nrOfCorrectHandovers >= nrOfRequiredCorrectHandovers)
                audioSource.PlayOneShot(successAudioClip);
            else
                audioSource.PlayOneShot(failureAudioClip);

            // store the level completion in the session
            app.Session.AddCompletedLevel(LevelMode.Level3QuizMode);

            // we do not use a CancellationToken here, as we want to wait for the user to click a button
            (resultsDialogUI.Message["nrOfCorrectHandovers"] as IntVariable).Value = nrOfCorrectHandovers;
            (resultsDialogUI.Message["nrOfHandoversRequired"] as IntVariable).Value = handoversRequired.Count;
            DialogButton btn = await resultsDialogUI.Show();

            if (btn == DialogButton.Primary)
                StopLevel();
            else {
                StopLevel();
                ResetLevel();
                await StartLevelAsync();
            }

            // if we do not wait here, flow will instantly continue with the menu again
            await UniTask.WaitUntil(() => IsLevelStopRequested);
        }

        public void StopLevel()
        {
            levelPlayingCts.Cancel();
            levelPlayingCts.Dispose();
            levelPlayingCts = null;

            mainMenuButton.onClick.RemoveListener(StopLevel);
            mainMenuButton.gameObject.SetActive(false);

            audioSource.Stop();

            introDialogUI.Hide();
            placeholderDialogUI.Hide();
            readyDialogUI.Hide();
            warningDialogUI.Hide();
            waitDialogUI.Hide();
            progressDialogUI.Hide();

            MarkerController.IsPlayingAudioOnInstrumentLost = false;
            MarkerController.DisableAllInstrumentMarkers();

            handPoseDetector.enabled = false;
            app.StopLeapHandTracking();

            IsLevelStopRequested = true;
            Debug.Log("Level3QuizModeController::StopLevel: Level stopped.");
        }

        public void ResetLevel()
        {
            nrOfCorrectHandovers = 0;

            audioSource.Stop();

            introDialogUI.Hide();
            warningDialogUI.Hide();

            MarkerController.DisableAllInstrumentMarkers();

            IsLevelStopRequested = false;
        }

        private void ShowDemoHand(HandoverData handoverData)
        {
            exampleHandPoseImage.gameObject.SetActive(true);
            exampleHandPoseImage.sprite = handoverData.ExampleImage;
        }

        private void HideDemoHand()
        {
            exampleHandPoseImage.gameObject.SetActive(false);
        }

        private void ShowSurgeon()
        {
            Debug.Log("Level3QuizModeController::ShowSurgeon: Showing surgeon.");
        }

        private void HideSurgeon()
        {
            Debug.Log("Level3QuizModeController::HideSurgeon: Hiding surgeon.");
        }

    }
}

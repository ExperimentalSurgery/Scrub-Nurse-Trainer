using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Assertions;
using UnityEngine.UI;
using NMY.OTAToolpicker.UI;
using UnityEngine.Localization;
using UnityEngine.Serialization;
using System;

namespace NMY.OTAToolpicker
{
    public class Level2LearnModeController : MonoBehaviour, ILevelController
    {
        [SerializeField] private OTAAppController app;
        public InstrumentMarkerController MarkerController => app.InstrumentMarkerController;

        [SerializeField] private bool resetSessionEntriesOnStart = true;

        [Tooltip("If true, the instrument details UI will be shown when an instrument is found.")]
        [SerializeField] private bool isShowingInstrumentDetails = true;

        [SerializeField] private PlaceableInstrumentElement elementsDisplayed = PlaceableInstrumentElement.OutlineRenderer | PlaceableInstrumentElement.Collider;


        private bool isLevelStopRequested = false;
        public bool IsLevelStopRequested
        {
            get => isLevelStopRequested;
            set => isLevelStopRequested = value;
        }

        // [Header("Tracking")]
        // [SerializeField] private TableMarker tableMarker;

        [Header("UI")]
        [SerializeField] private DialogUI infoDialogUI;
        [SerializeField] private DialogUI welcomeDialogUI;
        [SerializeField] private DialogUI ruleDialogUI;
        [SerializeField] private Button mainMenuButton;

        [SerializeField] private Transform initialRuleDialogPosition;

        [Header("Audio")]
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip welcomeAudioClip;
        [SerializeField] private AudioClip placementReminderAudioClip;
        [SerializeField] private AudioClip borderWarningAudioClip;

        [Header("Generic warnings")]
        [SerializeField] private LocalizedString placementWarningText;
        [SerializeField] private LocalizedString borderWarningText;
        [SerializeField] private LocalizedString directionWarningText;

        [Header("Rule data")]
        public RuleData rule1;
        public RuleData rule2;
        public RuleData rule3;
        public RuleData rule4;
        public RuleData rule5;
        public RuleData rule7;
        public RuleData rule8;
        public RuleData rule9;

        [Header("Debug")]
        [SerializeField] private bool skipIntro = false;

        private CancellationTokenSource levelPlayingCts;

        private InstrumentTable instrumentTable;

        void Awake()
        {
            instrumentTable = app.InstrumentTable;
        }

        void Start()
        {
            InitLevel();
        }

        public void InitLevel()
        {
            instrumentTable.gameObject.SetActive(false);
            Assert.IsNotNull(app.TableMarker, "Level2LearnModeController: table marker is required!");
            // tableMarker.DisableTracking();
        }

        public void ResetLevel()
        {
            // instrumentTable.gameObject.SetActive(false);
        }

        async public UniTask StartLevelAsync()
        {
            if (levelPlayingCts != null) {
                Debug.LogError("Level2LearnModeController: Already running!");
                return;
            }

            IsLevelStopRequested = false;

            levelPlayingCts = new CancellationTokenSource();
            CancellationToken ct = levelPlayingCts.Token;

            MarkerController.IsShowingInstrumentDetails = isShowingInstrumentDetails;
            MarkerController.SetInstrumentElementsDisplayed(elementsDisplayed);
            MarkerController.DisableAllInstrumentMarkers();

            HideMainMenuButton();

            if (!skipIntro)
            {
                await HelperTasks.ShowDialogWithAudio(welcomeDialogUI, audioSource, welcomeAudioClip, ct: ct);
            }
            ShowMainMenuButton();

            instrumentTable.gameObject.SetActive(true);
            instrumentTable.SetPlacementVolumeDisplayedElements(PlacementVolumeElement.All);
            instrumentTable.DisableAllPlacementVolumes();

            // --- Table calibration ---
            if (!app.IsTableCalibrated)
            {
                await HelperTasks.CalibrateTable(app.TableMarker, instrumentTable, app.TableCalibrationDialogUI, app.ShouldWaitForTableCalibrationClick, ct);
            }

            // --- Rule checks ---
            if (rule1.isEnabled)
                await PlayRule1(rule1, ct);
            if (rule2.isEnabled)
                await PlayRule2(rule2, ct);
            if (rule3.isEnabled)
                await PlayRule3(rule3, ct);
            if (rule4.isEnabled)
                await PlayRule4(rule4, ct);
            if (rule5.isEnabled)
                await PlayRule5(rule5, ct);
            if (rule7.isEnabled)
                await PlayRule7(rule7, ct);
            if (rule8.isEnabled)
                await PlayRule8(rule8, ct);
            if (rule9.isEnabled)
                await PlayRule9(rule9, ct);

            app.Session.AddCompletedLevel(LevelMode.Level2LearnMode);

            await UniTask.WaitUntil(() => IsLevelStopRequested);
        }

        public void StopLevel()
        {
            levelPlayingCts.Cancel();
            levelPlayingCts.Dispose();
            levelPlayingCts = null;

            audioSource.Stop();

            welcomeDialogUI.Hide();
            HideMainMenuButton();
            MarkerController.DisableAllInstrumentMarkers();
            app.InstrumentTable.gameObject.SetActive(false);

            IsLevelStopRequested = true;
        }

        async private UniTask PlayRule1(RuleData rule, CancellationToken ct)
        {
            if (ct.IsCancellationRequested) return;

            audioSource.PlayOneShot(rule.audioClip);
            instrumentTable.ShowTableBorder();

            await WaitForInstrumentPlacement(
                instrument: rule.instruments[0],
                rule: rule,
                alwaysShowPlacementVolume: true,
                ct: ct
            );

            audioSource.Stop();
            instrumentTable.HideTableBorder();
        }

        async private UniTask PlayRule2(RuleData rule, CancellationToken ct)
        {
            if (ct.IsCancellationRequested) return;

            audioSource.PlayOneShot(rule.audioClip);

            CancellationTokenSource arrowsCts = new();
            instrumentTable.ShowLevel2Rule2Arrows(delayS: 13.5f, arrowsCts.Token).Forget();

            await WaitForInstrumentPlacement(
                instrument: rule.instruments[0],
                rule: rule,
                alwaysShowPlacementVolume: true,
                ct: ct,
                onSuccess: () => arrowsCts.Cancel(),
                onIntersectingTableBorder: () => arrowsCts.Cancel(),
                onWrongPosition: () => arrowsCts.Cancel(),
                onDirectionInvalid: () => arrowsCts.Cancel()
            );
        }

        async private UniTask PlayRule3(RuleData rule, CancellationToken ct)
        {
            if (ct.IsCancellationRequested) return;

            audioSource.PlayOneShot(rule.audioClip);
            instrumentTable.ShowLevel3Rule3Area(delayS: 5f, ct).Forget();

            // Wait for first instrument
            await WaitForInstrumentPlacement(
                instrument: rule.instruments[0],
                rule: rule,
                alwaysShowPlacementVolume: true,
                ct: ct,
                shouldPlaySuccessMsg: false,
                onSuccess: () => instrumentTable.HideLevel3Rule3Area(),
                onIntersectingTableBorder: () => instrumentTable.HideLevel3Rule3Area(),
                onWrongPosition: () => instrumentTable.HideLevel3Rule3Area(),
                onDirectionInvalid: () => instrumentTable.HideLevel3Rule3Area()
            );

            // Wait for second instrument.
            // This will play the success audio clip and show the success dialog
            // when the instrument is placed correctly.
            await WaitForInstrumentPlacement(
                instrument: rule.instruments[1],
                rule: rule,
                alwaysShowPlacementVolume: true,
                ct: ct,
                shouldPlaySuccessMsg: true
            );
        }

        async private UniTask PlayRule4(RuleData rule, CancellationToken ct)
        {
            if (ct.IsCancellationRequested) return;

            audioSource.PlayOneShot(rule.audioClip);

            await WaitForInstrumentPlacement(
                instrument: rule.instruments[0],
                rule: rule,
                alwaysShowPlacementVolume: true,
                shouldPlaySuccessMsg: false,
                ct: ct
            );

            // TODO: Add Chirurgische Schere here. 3D-Model not yet available.
            if (rule.instruments.Count > 1)
            {
                await WaitForInstrumentPlacement(
                    instrument: rule.instruments[1],
                    rule: rule,
                    alwaysShowPlacementVolume: true,
                    shouldPlaySuccessMsg: true,
                    ct: ct
                );
            }
        }

        async private UniTask PlayRule5(RuleData rule, CancellationToken ct)
        {
            if (ct.IsCancellationRequested) return;

            audioSource.PlayOneShot(rule.audioClip);

            await WaitForInstrumentPlacement(
                instrument: rule.instruments[0],
                rule: rule,
                alwaysShowPlacementVolume: true,
                ct: ct
            );
        }

        async private UniTask PlayRule7(RuleData rule, CancellationToken ct)
        {
            if (ct.IsCancellationRequested) return;

            audioSource.PlayOneShot(rule.audioClip);

            // Wait for first instrument
            await WaitForInstrumentPlacement(
                instrument: rule.instruments[0],
                rule: rule,
                shouldPlaySuccessMsg: false,
                alwaysShowPlacementVolume: true,
                ct: ct
            );

            // Wait for second instrument.
            await WaitForInstrumentPlacement(
                instrument: rule.instruments[1],
                rule: rule,
                shouldPlaySuccessMsg: false,
                alwaysShowPlacementVolume: true,
                ct: ct
            );
        }

        async private UniTask PlayRule8(RuleData rule, CancellationToken ct)
        {
            if (ct.IsCancellationRequested) return;

            audioSource.PlayOneShot(rule.audioClip);

            await WaitForInstrumentPlacement(
                instrument: rule.instruments[0],
                rule: rule,
                shouldPlaySuccessMsg: true,
                alwaysShowPlacementVolume: true,
                ct: ct
            );
        }

        async private UniTask PlayRule9(RuleData rule, CancellationToken ct)
        {
            if (ct.IsCancellationRequested) return;

            PlacementVolume volume = instrumentTable.GetPlacementVolume(rule.instruments[0]);
            volume.ElementsDisplayed = PlacementVolumeElement.Collider;

            audioSource.PlayOneShot(rule.audioClip);

            await WaitForInstrumentPlacement(
                instrument: rule.instruments[0],
                rule: rule,
                shouldPlaySuccessMsg: true,
                alwaysShowPlacementVolume: true,
                ct: ct
            );

            volume.ElementsDisplayed = PlacementVolumeElement.All;
        }

        /// <summary>
        /// Wait for the user to place the instrument and press the continue button in the rule dialog.
        /// </summary>
        /// <param name="instrument">The instrument to wait for.</param>
        /// <param name="rule">Rule data used for waiting.</param>
        /// <param name="ct"></param>
        /// <param name="shouldPlaySuccessMsg">If <c>true</c> the success audio clip will be played and the success dialog will be shown.</param>
        /// <param name="onSuccess">Optional action to be executed on success.</param>
        /// <param name="onIntersectingTableBorder">Optional action to be executed when instrument is intersecting the table border.</param>
        /// <param name="onWrongPosition">Optional action to be executed when the instrument is placed at the wrong position.</param>
        /// <param name="onDirectionInvalid">Optional action to be executed when the instrument is placed in the wrong direction.</param>
        /// <param name="alwaysShowPlacementVolume">If <c>true</c> the placement volume will always be shown, if <c>false</c> it will only be shown when the marker is currently tracked.</param>
        /// <returns>Returns when the instrument has been correctly placed (position + direction).</returns>
        async private UniTask WaitForInstrumentPlacement(InstrumentData instrument, RuleData rule, bool shouldPlaySuccessMsg=true,
                Action onSuccess=null, Action onIntersectingTableBorder=null, Action onWrongPosition=null, Action onDirectionInvalid=null, bool alwaysShowPlacementVolume=false, CancellationToken ct=default)
        {
            // when entering this function with the CancellationToken ct already cancelled, we return immediately.
            if (ct.IsCancellationRequested) return;

            IInstrumentMarker ruleInstrumentMarker = MarkerController.GetInstrumentMarker(instrument);
            Assert.IsNotNull(ruleInstrumentMarker, "Level2LearnModeController: Instrument marker is missing!");
            PlaceableInstrument placeableInstrument = ruleInstrumentMarker.PlaceableInstrument;
            Assert.IsNotNull(placeableInstrument, "Level2LearnModeController: PlaceableInstrument component is missing!");

            MarkerController.IsPlayingAudioOnInstrumentFound = true;
            MarkerController.IsPlayingAudioOnInstrumentLost = true;
            MarkerController.EnableInstrumentMarker(instrument);

            if (alwaysShowPlacementVolume)
                instrumentTable.EnablePlacementVolume(placeableInstrument);

            // monitor tracking of the instrument marker. The monitoring function
            // will enable the primary button of the rule dialog when the instrument is visible
            // and disable it when the instrument is not visible.
            // CancellationTokenSource ruleInstrumentMonitorCts = new CancellationTokenSource();
            CancellationTokenSource ruleInstrumentMonitorCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            // UIHelper.MonitorInstrumentTracking(instrument, ruleDialogUI, markerController, instrumentTable, ruleInstrumentMonitorCts.Token).Forget();

            HelperTasks.MonitorInstrumentTracking(instrument, MarkerController,
                onTracked: () => {
                    ruleDialogUI.SetPrimaryButtonInteractable(true);
                    ruleDialogUI.SetPrimaryButtonText($"Weiter mit <b>{placeableInstrument.InstrumentData.Title.GetLocalizedString()}</b>");
                    if (!alwaysShowPlacementVolume)
                        instrumentTable.EnablePlacementVolume(placeableInstrument);
                },
                onNotTracked: () => {
                    ruleDialogUI.SetPrimaryButtonInteractable(false);
                    ruleDialogUI.SetPrimaryButtonText($"Warte auf <b>{placeableInstrument.InstrumentData.Title.GetLocalizedString()}</b>...");
                    if (!alwaysShowPlacementVolume)
                        instrumentTable.DisablePlacementVolume(placeableInstrument);
                },
                ct: ruleInstrumentMonitorCts.Token
            ).Forget();

            // move the rule dialog to the position specified in the rule data
            ruleDialogUI.transform.position = rule.ruleDialogPosition.position;
            ruleDialogUI.transform.rotation = rule.ruleDialogPosition.rotation;

            bool hasCorrectPosition = false;
            bool hasCorrectDirection = false;
            while ((!hasCorrectPosition || (rule.isDirectionCheckEnabled && !hasCorrectDirection)) && !ct.IsCancellationRequested)
            {
                UniTask ruleDialogTask = HelperTasks.ShowDialog(ruleDialogUI, rule.dialogTitle.GetLocalizedString(), rule.dialogMessage.GetLocalizedString(), ct);
                // wait for the user to press the button in the rule dialog
                await ruleDialogTask;
                // flow will continue here, when the user pressed the button in the rule dialog.
                audioSource.Stop();

                instrumentTable.CheckPlacement();
                bool hasWrongPosition =  placeableInstrument.IsWithinTable && !placeableInstrument.IsWithinPlacementVolume;
                bool isIntersectingTableBorder = placeableInstrument.IsIntersectingTableBorder || !placeableInstrument.IsWithinTable;
                hasCorrectDirection = placeableInstrument.IsDirectionValid;
                hasCorrectPosition = placeableInstrument.IsWithinPlacementVolume;

                if (isIntersectingTableBorder)
                {
                    onIntersectingTableBorder?.Invoke();
                    // the instrument is not within the table
                    audioSource.PlayOneShot(borderWarningAudioClip);
                    await HelperTasks.ShowDialog(infoDialogUI, "Steht über", borderWarningText.GetLocalizedString(), ct: ct);
                }
                else if (hasWrongPosition)
                {
                    onWrongPosition?.Invoke();
                    // the instrument is within the table but not within the placement volume
                    // basically it is placed at the wrong position.
                    audioSource.PlayOneShot(placementReminderAudioClip);
                    await HelperTasks.ShowDialog(infoDialogUI, "Falsche Position", placementWarningText.GetLocalizedString(), ct: ct);
                }
                else if (rule.isDirectionCheckEnabled && !hasCorrectDirection)
                {
                    onDirectionInvalid?.Invoke();
                    if (rule.directionCheckAudioClip)
                        audioSource.PlayOneShot(rule.directionCheckAudioClip);
                    await HelperTasks.ShowDialog(infoDialogUI, "Falsche Richtung", rule.directionCheckText.GetLocalizedString(), ct: ct);
                }
                else if (hasCorrectPosition && rule.successAudioClip)
                {
                    // the instrument is within the placement volume
                    onSuccess?.Invoke();
                    if (shouldPlaySuccessMsg)
                    {
                        audioSource.PlayOneShot(rule.successAudioClip);
                        await HelperTasks.ShowDialog(infoDialogUI, "Richtig", rule.successText.GetLocalizedString(), ct: ct);
                    }
                }
                audioSource.Stop();
            }

            // the marker controller will receive a lost event when the marker is disabled.
            // we dont want to the "lost" audio SFX to be played in this case so we turn it off.
            MarkerController.IsPlayingAudioOnInstrumentLost = false;

            instrumentTable.DisablePlacementVolume(placeableInstrument);
            MarkerController.DisableInstrumentMarker(instrument);
            ruleInstrumentMonitorCts.Cancel();
            ruleInstrumentMonitorCts.Dispose();
        }

        private void ShowMainMenuButton()
        {
            mainMenuButton.onClick.AddListener(StopLevel);
            mainMenuButton.gameObject.SetActive(true);
        }

        private void HideMainMenuButton()
        {
            mainMenuButton.onClick.RemoveListener(StopLevel);
            mainMenuButton.gameObject.SetActive(false);
        }

    }
}

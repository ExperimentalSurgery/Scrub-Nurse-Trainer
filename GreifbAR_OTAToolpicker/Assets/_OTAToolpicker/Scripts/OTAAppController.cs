using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using Leap.Unity;
using NMY.OTAToolpicker.UI;
using UnityEngine;
using UnityEngine.UI;

namespace NMY.OTAToolpicker
{
    public class OTAAppController : MonoBehaviour
    {
        // [SerializeField] private InstrumentMarkerController instrumentMarkerController;
        public InstrumentMarkerController InstrumentMarkerController {
            get {
                if(trackingHardware == TrackingHardware.Vuforia)
                    return vuforiaController.InstrumentMarkerController;
                else if(trackingHardware == TrackingHardware.Varjo)
                    return varjoController.InstrumentMarkerController;
                else
                    throw new NotImplementedException();
            }
        }

        [SerializeField] private TableMarker tableMarker;
        public TableMarker TableMarker => tableMarker;

        [SerializeField] private InstrumentTable instrumentTable;
        public InstrumentTable InstrumentTable => instrumentTable;

        [SerializeField] private bool shouldCalibrateTableAtStart = false;
        public bool ShouldCalibrateTableAtStart => shouldCalibrateTableAtStart;
        private bool isTableCalibrated = false;
        public bool IsTableCalibrated {
            get => isTableCalibrated;
            set => isTableCalibrated = value;
        }

        [SerializeField] private KeyCode recalibrateKey = KeyCode.R;
        [SerializeField] private KeyCode recenterTableKey = KeyCode.T;

        [Header("Surgeon")]
        [SerializeField] private bool isSurgeonVisible = false;
        [SerializeField] private KeyCode toggleSurgeonKey = KeyCode.S;
        [SerializeField] private KeyCode surgeonUpKey = KeyCode.UpArrow;
        [SerializeField] private KeyCode surgeonDownKey = KeyCode.DownArrow;
        [SerializeField] private Vector3 surgeonMoveDistance = new Vector3(0f, 0f, 0.1f);

        [Header("Tracking hardware")]
        [SerializeField] private TrackingHardware trackingHardware = TrackingHardware.Vuforia;
        public TrackingHardware TrackingHardware => trackingHardware;

        [SerializeField] private VuforiaTrackingController vuforiaController;
        public VuforiaTrackingController VuforiaController => vuforiaController;

        [SerializeField] private VarjoTrackingController varjoController;
        public VarjoTrackingController VarjoController => varjoController;

        [Header("Leap Motion")]
        [SerializeField] private LeapServiceProvider leapProvider;
        public LeapServiceProvider LeapProvider => leapProvider;

        [SerializeField] private LeapServiceProvider.TrackingOptimizationMode leapTrackingMode = LeapServiceProvider.TrackingOptimizationMode.Desktop;

        [Header("App settings")]
        [SerializeField] private AppMode appMode = AppMode.DemoMode;
        public AppMode AppMode => appMode;
        public void SetAppMode(AppMode mode)
        {
            appMode = mode;
        }

        [SerializeField] private bool startOnMainMenuTeachMode = true;
        [SerializeField] private bool startOnMainMenuDemoMode = true;

        [Header("Session")]
        [SerializeField] private Session session;
        public Session Session => session;
        [SerializeField] private bool resetSessionOnStart = true;

        [Header("UI")]
        [SerializeField] private DialogUI tableCalibrationDialogUI;
        public DialogUI TableCalibrationDialogUI => tableCalibrationDialogUI;
        [SerializeField] private AppModeSelectionUI appModeSelectionUI;
        public AppModeSelectionUI AppModeSelectionUI => appModeSelectionUI;
        [SerializeField] private LevelModeSelectionUI levelModeSelectionUI;
        public LevelModeSelectionUI LevelModeSelectionUI => levelModeSelectionUI;

        [Header("Game objects")]
        [SerializeField] private GameObject surgeonGO;

        [Header("Level 1")]
        [SerializeField] private Level1LearnModeController level1LearnModeController;
        [SerializeField] private Level1QuizModeController level1QuizModeController;

        [Header("Level 2")]
        [SerializeField] private Level2LearnModeController level2LearnModeController;
        [SerializeField] private Level2QuizModeController level2QuizModeController;

        [Header("Level 3")]
        [SerializeField] private Level3LearnModeController level3LearnModeController;
        [SerializeField] private Level3QuizModeController level3QuizModeController;

        void Awake()
        {
            ApplyTrackingHardwareToMarkers();
        }

        async void Start()
        {
            ResetApp();

            if (resetSessionOnStart)
                session.Reset();

            await InitializeTracking();
            await StartTrackingAsync();

            if (isSurgeonVisible)
                ShowSurgeon();
            else
                HideSurgeon();

            if (shouldCalibrateTableAtStart)
            {
                // instrumentTable.gameObject.SetActive(true);
                await HelperTasks.CalibrateTable(tableMarker, instrumentTable, tableCalibrationDialogUI, ShouldWaitForTableCalibrationClick, default(CancellationToken));
                isTableCalibrated = true;
            }

            if (startOnMainMenuTeachMode)
            {
                session.AppMode = AppMode.TeachMode;
                appModeSelectionUI.gameObject.SetActive(false);
            }
            else if (startOnMainMenuDemoMode) {
                session.AppMode = AppMode.DemoMode;
                appModeSelectionUI.gameObject.SetActive(false);
            }
            else {
                session.AppMode = await appModeSelectionUI.SelectAppMode(destroyCancellationToken);
                if (session.AppMode == AppMode.DemoMode)
                    session.AddAllLevelsAsCompleted();
            }

            try {
                await ShowMainMenuAsync(destroyCancellationToken);
            }
            catch (OperationCanceledException) {
                Debug.Log("ShowMainMenuAsync was cancelled");
            }

            await StopTrackingAsync();
        }

        public void ResetApp()
        {
            appMode = AppMode.Initial;
            // appModeSelectionUI.gameObject.SetActive(false);
            levelModeSelectionUI.gameObject.SetActive(false);
            level1LearnModeController.ResetLevel();
            level2QuizModeController.ResetLevel();
        }

        public async UniTask ShowMainMenuAsync(CancellationToken ct)
        {
            while (!ct.IsCancellationRequested)
            {
                LevelMode levelMode = await levelModeSelectionUI.SelectLevelMode(session, ct);
                Debug.Log($"Level mode selected: {levelMode}");
                if (levelMode == LevelMode.Level1LearnMode)
                    await level1LearnModeController.StartLevelAsync();
                else if(levelMode == LevelMode.Level1QuizMode)
                    await level1QuizModeController.StartLevelAsync();
                else if(levelMode == LevelMode.Level2LearnMode)
                    await level2LearnModeController.StartLevelAsync();
                else if(levelMode == LevelMode.Level2QuizMode)
                    await level2QuizModeController.StartLevelAsync();
                else if(levelMode == LevelMode.Level3LearnMode)
                    await level3LearnModeController.StartLevelAsync();
                else if(levelMode == LevelMode.Level3QuizMode)
                    await level3QuizModeController.StartLevelAsync();
            }
        }

        public async UniTask InitializeTracking()
        {
            if (trackingHardware == TrackingHardware.Vuforia)
                await vuforiaController.InitializeTrackingAsync();
            else if(trackingHardware == TrackingHardware.Varjo)
                await varjoController.InitializeTrackingAsync();
        }

        public async UniTask StartTrackingAsync()
        {
            if (trackingHardware == TrackingHardware.Vuforia)
                await vuforiaController.StartTrackingAsync();
            else if(trackingHardware == TrackingHardware.Varjo)
                await varjoController.StartTrackingAsync();
        }

        public async UniTask StopTrackingAsync()
        {
            if (trackingHardware == TrackingHardware.Vuforia)
                await vuforiaController.StopTrackingAsync();
            else if(trackingHardware == TrackingHardware.Varjo)
                await varjoController.StopTrackingAsync();
        }

        private void ApplyTrackingHardwareToMarkers()
        {
            foreach (var marker in InstrumentMarkerController.FindInstrumentMarkers())
            {
                marker.TrackingHardware = trackingHardware;
            }

            tableMarker.TrackingHardware = trackingHardware;
        }

        async public void StartLeapHandTracking()
        {
            leapProvider.gameObject.SetActive(true);
            leapProvider.enabled = true;
            await UniTask.WaitForSeconds(0.5f);
            leapProvider.ChangeTrackingMode(leapTrackingMode);
        }

        public void StopLeapHandTracking()
        {
            leapProvider.enabled = false;
            leapProvider.gameObject.SetActive(false);
        }

        // [Tooltip("If <b>true</b>, the user has to click the button in the table marker calibration dialog to accept the calibration (Vuforia). If <b>false</b>, the calibration will be accepted when the table marker is found (Varjo).")]
        public bool ShouldWaitForTableCalibrationClick => trackingHardware == TrackingHardware.Vuforia;

        async void Update()
        {
            if (Input.GetKeyDown(recalibrateKey))
            {
                await HelperTasks.CalibrateTable(tableMarker, instrumentTable, tableCalibrationDialogUI, ShouldWaitForTableCalibrationClick, default(CancellationToken));
            }

            if (Input.GetKeyDown(toggleSurgeonKey))
            {
                ToggleSurgeon();
            }

            if (Input.GetKeyDown(surgeonUpKey))
            {
                surgeonGO.transform.position += surgeonMoveDistance;
            }

            if (Input.GetKeyDown(surgeonDownKey))
            {
                surgeonGO.transform.position -= surgeonMoveDistance;
            }

            if (Input.GetKey(recenterTableKey))
            {
                tableMarker.VarjoMarker.enabled = true;
                tableMarker.VarjoMarker.shouldTrack = true;
            }
            if (Input.GetKeyUp(recenterTableKey))
            {
                tableMarker.VarjoMarker.enabled = false;
                tableMarker.VarjoMarker.shouldTrack = false;
                tableMarker.transform.rotation = Quaternion.Euler(0, tableMarker.transform.rotation.eulerAngles.y, 0);
            }
        }

        private void ShowSurgeon() => surgeonGO.SetActive(true);
        private void HideSurgeon() => surgeonGO.SetActive(false);

        private void ToggleSurgeon()
        {
            if (surgeonGO.activeSelf)
                HideSurgeon();
            else
                ShowSurgeon();
        }

    }
}

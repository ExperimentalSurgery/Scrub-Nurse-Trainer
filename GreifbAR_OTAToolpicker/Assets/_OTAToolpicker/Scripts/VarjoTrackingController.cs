using Cysharp.Threading.Tasks;
using DFKI.NMY;
using Unity.VisualScripting;
using UnityEngine;

namespace NMY.OTAToolpicker
{
    public class VarjoTrackingController : MonoBehaviour, ITrackingHardwareController
    {
        [Tooltip("If true, the tracking will be initialized on app start. Do not use this until you want to debug and know what you are doing.")]
        [SerializeField] private bool initTrackingOnStart = false;
        [Tooltip("If true, the tracking will be started on app start. Do not use this until you want to debug and know what you are doing.")]
        [SerializeField] private bool startTrackingOnStart = false;

        [SerializeField] private GreifbARVarjoManager varjoManager;
        [SerializeField] private InstrumentMarkerController instrumentMarkerController;
        public InstrumentMarkerController InstrumentMarkerController => instrumentMarkerController;
        [SerializeField] private OTAMarkerManager markerManager;
        [SerializeField] private GameObject varjoCameraGO;

        void Awake()
        {
            // DO NOT deactivate the camera in Awake() as this will result in offsetted hands
            //DeactivateVarjoCamera();

            instrumentMarkerController.gameObject.SetActive(false);
        }

        async void Start()
        {
            if(initTrackingOnStart)
                await InitializeTrackingAsync();

            if(startTrackingOnStart)
                await StartTrackingAsync();
        }

        public UniTask InitializeTrackingAsync() {
            ActivateVarjoCamera();
            instrumentMarkerController.gameObject.SetActive(true);

            CopyInstrumentMarkersToMarkerManager(instrumentMarkerController);
            varjoManager.Initialize();
            return UniTask.CompletedTask;
        }

        public UniTask StartTrackingAsync() {
            varjoManager.EnableVarjo();
            return UniTask.CompletedTask;
        }

        public UniTask StopTrackingAsync() {
            varjoManager.DisableVarjo();
            return UniTask.CompletedTask;
        }

        private void CopyInstrumentMarkersToMarkerManager(InstrumentMarkerController instrumentMarkerController)
        {
            foreach(InstrumentMarker instrumentMarker in instrumentMarkerController.InstrumentMarkers)
            {
                markerManager.trackedObjects.Add(new OTAMarkerManager.OTATrackedObject() { visualizer = instrumentMarker.GetComponent<OTAVarjoMarker>() });
            }
        }

        public void ActivateVarjoCamera()
        {
            varjoCameraGO.SetActive(true);
        }

        public void DeactivateVarjoCamera()
        {
            varjoCameraGO.SetActive(false);
        }
    }
}

using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Serialization;
using Vuforia;

namespace NMY.OTAToolpicker
{
    public class VuforiaTrackingController : MonoBehaviour, ITrackingHardwareController
    {
        [Tooltip("If true, the tracking will be initialized on app start. Do not use this until you want to debug and know what you are doing.")]
        [SerializeField] private bool initTrackingOnStart = false;
        [Tooltip("If true, the tracking will be started on app start. Do not use this until you want to debug and know what you are doing.")]
        [SerializeField] private bool startTrackingOnStart = false;
        [SerializeField] private GameObject vuforiaCameraGO;
        [SerializeField] private InstrumentMarkerController instrumentMarkerController;
        public InstrumentMarkerController InstrumentMarkerController => instrumentMarkerController;

        private bool isAppQuitting = false;

        async void Start()
        {
            // by default, the Vuforia camera is deactivated. Will be activated in InitializeTrackingAsync()
            // when Vuforia tracking is used
            DeactivateVuforiaCamera();

            instrumentMarkerController.gameObject.SetActive(false);

            if(initTrackingOnStart)
                await InitializeTrackingAsync();

            if(startTrackingOnStart)
                await StartTrackingAsync();

        }

        public async UniTask InitializeTrackingAsync()
        {
            ActivateVuforiaCamera();
            instrumentMarkerController.gameObject.SetActive(true);

            VuforiaConfiguration.Instance.VideoBackground.VideoBackgroundEnabled = false;
            // Assert.IsFalse(VuforiaConfiguration.Instance.VideoBackground.VideoBackgroundEnabled, "Vuforia video background should be disabled in the Vuforia configuration file.");

            VuforiaApplication vuforia = VuforiaApplication.Instance;
            if(!vuforia.IsInitialized)
                vuforia.Initialize();

            await UniTask.WaitUntil(() => vuforia.IsInitialized, cancellationToken: destroyCancellationToken);
        }

        public async UniTask StartTrackingAsync()
        {
            await InitializeTrackingAsync();

            // check if the main camera has a VuforiaBehaviour component
            VuforiaBehaviour vuforiaBehaviour = Camera.main.GetComponent<VuforiaBehaviour>();
            if (vuforiaBehaviour == null)
            {
                Debug.LogError("The main camera does not have a VuforiaBehaviour component. Cannot start Vuforia.");
                return;
            }

            // make sure to activate the video background added by Vuforia
            vuforiaBehaviour.VideoBackground?.StartVideoBackgroundRendering();

            vuforiaBehaviour.enabled = true;

            await UniTask.WaitUntil(() => VuforiaApplication.Instance.IsRunning, cancellationToken: destroyCancellationToken);

            Debug.Log("Vuforia tracking started.");
        }

        public async UniTask StopTrackingAsync()
        {
             // check if the main camera has a VuforiaBehaviour component
            VuforiaBehaviour vuforiaBehaviour = Camera.main.GetComponent<VuforiaBehaviour>();
            if (vuforiaBehaviour == null)
            {
                Debug.LogError("The main camera does not have a VuforiaBehaviour component. Cannot stop Vuforia.");
                return;
            }

            // Make sure to deactivate the video background added by Vuforia, but only if
            // the application is not quitting (play mode is stopped). In that case the video
            // background will be deactivated/cleaned up by Vuforia itself and if we do it
            // here again, next time we enter play mode, Vuforia will throw an error which can
            // only be fixed by restarting the Unity editor. Fun!
            if (!isAppQuitting) {
                vuforiaBehaviour.VideoBackground?.StopVideoBackgroundRendering();
                vuforiaBehaviour.enabled = false;
            }

            await UniTask.WaitUntil(() => !VuforiaApplication.Instance.IsRunning);

            Debug.Log("Vuforia tracking stopped.");

            return;
        }

        public void ActivateVuforiaCamera()
        {
            vuforiaCameraGO.SetActive(true);
        }

        public void DeactivateVuforiaCamera()
        {
            vuforiaCameraGO.SetActive(false);
        }


        private void OnApplicationQuit()
        {
            isAppQuitting = true;
        }
    }
}

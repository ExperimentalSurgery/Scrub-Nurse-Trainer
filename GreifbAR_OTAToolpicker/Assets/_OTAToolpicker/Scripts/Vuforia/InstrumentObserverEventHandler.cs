using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using UnityEngine.Events;

namespace NMY.OTAToolpicker
{
    public class InstrumentObserverEventHandler : DefaultObserverEventHandler
    {
        public UnityEvent<InstrumentMarker> OnInstrumentFound = new();
        public UnityEvent<InstrumentMarker> OnInstrumentLost = new();
        public UnityEvent<InstrumentMarker> OnInstrumentDropped = new();

        [Tooltip("The instrument marker this observer is observing. If not specified explicitly, it will be set to the InstrumentMarker component of the GameObject.")]
        [SerializeField] private InstrumentMarker instrumentMarker;
        public InstrumentMarker InstrumentMarker {
            get => instrumentMarker;
            set => instrumentMarker = value;
        }

        [SerializeField] private float dropTimeoutS = 2f;

        private float lastInstrumentLostTime = 0f;
        private bool isDropQueued = false;

        private CancellationTokenSource cts = new();

        private bool isFirstTimeTrackingLost = true;

        void Awake()
        {
            if (instrumentMarker == null)
                instrumentMarker = GetComponent<InstrumentMarker>();
        }

        protected override void OnTrackingFound()
        {
            cts.Cancel();
            isDropQueued = false;

            OnTargetFound?.Invoke();
            OnInstrumentFound?.Invoke(instrumentMarker);
        }

        protected override void OnTrackingLost()
        {
            // Vuforia fires OnTrackingLost event for every observable when the
            // app starts, so we ignore the first one otherwise our logic will be broken.
            if (isFirstTimeTrackingLost) {
                isFirstTimeTrackingLost = false;
                return;
            }

            OnTargetLost?.Invoke();
            OnInstrumentLost?.Invoke(instrumentMarker);

            if (!isDropQueued) {
                lastInstrumentLostTime = Time.time;
                isDropQueued = true;
                CheckForDrop(cts.Token).Forget();
            }
        }

        protected async UniTask CheckForDrop(CancellationToken ct)
        {
            // Debug.Log($"CheckForDrop: waiting");
            await UniTask.WaitUntil(() => (Time.time - lastInstrumentLostTime) > dropTimeoutS || !isDropQueued, PlayerLoopTiming.TimeUpdate);
            // Debug.Log($"CheckForDrop: waiting over");
            if (isDropQueued) {
                // Debug.Log($"CheckForDrop: invoking event");
                OnInstrumentDropped?.Invoke(instrumentMarker);
                isDropQueued = false;
            }
        }
    }
}
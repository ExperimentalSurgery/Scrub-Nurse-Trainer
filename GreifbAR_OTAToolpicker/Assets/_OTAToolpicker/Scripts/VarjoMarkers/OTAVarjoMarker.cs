using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace NMY.OTAToolpicker
{
    public class OTAVarjoMarker : MonoBehaviour
    {
        public UnityEvent<InstrumentMarker> MarkerFound;
        public UnityEvent<InstrumentMarker> MarkerLost;

        public int markerId = 200;
        public bool shouldTrack;
        public bool isDynamic = false;
        public bool isTracked;
        public long timeout = 1000;

        private InstrumentMarker instrumentMarker;

        private bool lastIsTracked = false;

        private void Awake()
        {
            instrumentMarker = GetComponent<InstrumentMarker>();
            if (instrumentMarker == null )
            {
                Debug.LogError($"OTAVarjoMarker[{name}]: no InstrumentMarker on GameObject!");
            }
            lastIsTracked = isTracked;
        }

        private void Update()
        {
            if (isTracked != lastIsTracked)
            {
                if (isTracked)
                {
                    MarkerFound.Invoke(instrumentMarker);
                }
                else
                {
                    MarkerLost.Invoke(instrumentMarker);
                }
            }
            lastIsTracked = isTracked;
        }

        public void SetTrackingEnabled(bool enable)
        {
            shouldTrack = enable;
            if (shouldTrack)
                MarkerFound.Invoke(instrumentMarker);
            else
                MarkerLost.Invoke(instrumentMarker);
        }
    }
}

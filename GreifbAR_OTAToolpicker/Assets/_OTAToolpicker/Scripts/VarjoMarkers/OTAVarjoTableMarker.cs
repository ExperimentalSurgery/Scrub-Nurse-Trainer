using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace NMY.OTAToolpicker
{
    public class OTAVarjoTableMarker : MonoBehaviour
    {
        public UnityEvent<TableMarker> MarkerFound;
        public UnityEvent<TableMarker> MarkerLost;

        public int markerId = 300;
        public bool shouldTrack;
        public bool isDynamic = false;
        public bool isTracked;
        public long timeout = 1000;
        public void SetTrackingEnabled(bool enable) => shouldTrack = enable;

        private TableMarker tableMarker;

        private bool lastIsTracked = false;

        public float initialDelay = 2;

        private void Start()
        {
            tableMarker = GetComponent<TableMarker>();
            if (tableMarker == null )
            {
                Debug.LogError($"OTAVarjoTableMarker[{name}]: no TableMarker on GameObject!");
            }
            lastIsTracked = isTracked;
        }

        private void Update()
        {
            if ( shouldTrack )
            {
                if (isTracked != lastIsTracked)
                {

                    if(isTracked && initialDelay >0){

                        initialDelay-=Time.deltaTime;
                        return;
                    }

                    if (isTracked){
                        MarkerFound.Invoke(tableMarker);
                    }
                    else
                    {
                        MarkerLost.Invoke(tableMarker);
                    }
                    lastIsTracked = isTracked;
                }
            }
        }
    }
}

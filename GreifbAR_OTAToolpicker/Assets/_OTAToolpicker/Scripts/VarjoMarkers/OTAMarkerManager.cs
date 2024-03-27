using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Varjo.XR;

namespace NMY.OTAToolpicker
{
    public class OTAMarkerManager : MonoBehaviour
    {
        [Serializable]
        public struct OTATrackedObject
        {
            public OTAVarjoMarker visualizer;
        }

        public TableMarker tableMarker;

        // A public array for all the tracked objects        
        public List<OTATrackedObject> trackedObjects = new();

        // A list for found markers
        private List<VarjoMarker> markers = new(); // List<VarjoMarker>();

        // A list for IDs of removed markers
        private List<long> removedMarkerIds = new List<long>();
        private void OnEnable()=>VarjoMarkers.EnableVarjoMarkers(true);
        private void OnDisable()=>VarjoMarkers.EnableVarjoMarkers(false);


        void Update()
        {
            // Check if Varjo Marker tracking is enabled and functional
            if (VarjoMarkers.IsVarjoMarkersEnabled())
            {
                // Get a list of markers with up-to-date data
                VarjoMarkers.GetVarjoMarkers(out markers);

                // Debug.Log("Markers.Count=" + markers.Count);

                   List<long> ids = new List<long>();

                // Loop through found markers and update gameobjects matching the marker ID in the array
                foreach (var marker in markers)
                {
                    ids.Add(marker.id);
                    for (var i = 0; i < trackedObjects.Count; i++)
                    {
                        if (trackedObjects[i].visualizer.markerId == marker.id && trackedObjects[i].visualizer.shouldTrack)
                        {
                            if (trackedObjects[i].visualizer.isTracked == false) {

                                if (trackedObjects[i].visualizer.isDynamic) {
                                    VarjoMarkers.AddVarjoMarkerFlags(marker.id, VarjoMarkerFlags.DoPrediction);
                                }

                                if (trackedObjects[i].visualizer.timeout > 0)
                                {
                                    VarjoMarkers.SetVarjoMarkerTimeout(marker.id, trackedObjects[i].visualizer.timeout);
                                }
                            }

                            trackedObjects[i].visualizer.isTracked = true;
                            trackedObjects[i].visualizer.gameObject.transform.localPosition = marker.pose.position;
                            trackedObjects[i].visualizer.gameObject.transform.localRotation = marker.pose.rotation;
                        }
                    }

                    if (tableMarker != null && marker.id == tableMarker.VarjoMarker.markerId && tableMarker.VarjoMarker.shouldTrack)
                    {
                        if (tableMarker.VarjoMarker.isDynamic)
                        {
                            VarjoMarkers.AddVarjoMarkerFlags(marker.id, VarjoMarkerFlags.DoPrediction);
                        }

                        if (tableMarker.VarjoMarker.timeout > 0)
                        {
                            VarjoMarkers.SetVarjoMarkerTimeout(marker.id, tableMarker.VarjoMarker.timeout);
                        }

                        tableMarker.transform.localPosition = marker.pose.position;
                        tableMarker.transform.localRotation = marker.pose.rotation;
                        
                    }
                }


                foreach(var tobj in trackedObjects)
                {
                    if (tobj.visualizer.shouldTrack)
                        tobj.visualizer.isTracked = ids.Contains(tobj.visualizer.markerId);
                    else
                        tobj.visualizer.isTracked = false;
                }

                // Table marker 
                if(tableMarker != null)
                    tableMarker.VarjoMarker.isTracked = ids.Contains(tableMarker.VarjoMarker.markerId);

                /*
                // Get a list of IDs of removed markers
                VarjoMarkers.GetRemovedVarjoMarkerIds(out removedMarkerIds);

                Debug.Log("removedMarkerIds=" +  removedMarkerIds.Count);
                // Loop through removed marker IDs and deactivate gameobjects matching the marker IDs in the array
                foreach (var id in removedMarkerIds)
                {
                     for (var i = 0; i < trackedObjects.Length; i++) {
                        if (trackedObjects[i].visualizer.markerId == id) {
                            trackedObjects[i].visualizer.isTracked = false;
                        }
                    }
                }
                */
            }
        }
    }
}

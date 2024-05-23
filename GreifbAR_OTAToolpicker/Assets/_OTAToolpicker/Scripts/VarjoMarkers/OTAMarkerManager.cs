using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Leap.Unity;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
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

        public int currentMarkerCount {get; private set;}
        public int currentTrackedObjectsCount {get; private set;}

        // A public array for all the tracked objects        
        public List<OTATrackedObject> trackedObjects = new();

        // A list for found markers
        public List<VarjoMarker> markers = new(); // List<VarjoMarker>();
        // public Transform uiParent;
        // public TextMeshProUGUI markerCounterText;
        // public Color deactivatedColor;
        // public Color activatedColor;
        // public Color trackedColor;        

        // A list for IDs of removed markers
        //private List<long> removedMarkerIds = new List<long>();
        private long currentTrackedMarker;
        private void OnEnable()=>VarjoMarkers.EnableVarjoMarkers(true);
        private void OnDisable()=>VarjoMarkers.EnableVarjoMarkers(false);

        void FixedUpdate()
        {
            // Check if Varjo Marker tracking is enabled and functional
            if (VarjoMarkers.IsVarjoMarkersEnabled())
            {
                // Get a list of markers with up-to-date data
                VarjoMarkers.GetVarjoMarkers(out markers);
                currentMarkerCount = markers.Count;
                //markerCounterText.text = currentMarkerCount.ToString();
                //Debug.Log("Markers.Count=" + markers.Count);

                List<long> ids = new List<long>();
                currentTrackedMarker = 0;
                currentTrackedObjectsCount = 0;

                // reset tracked info
                 for (var i = 0; i < trackedObjects.Count; i++){
                        trackedObjects[i].visualizer.ResetIsTracked();
                }



                // Loop through found markers and update gameobjects matching the marker ID in the array
                foreach (var marker in markers)
                {
                    //VarjoMarkers.AddVarjoMarkerFlags(marker.id, VarjoMarkerFlags.DoPrediction);
                    ids.Add(marker.id);
                    for (var i = 0; i < trackedObjects.Count; i++)
                    {
                        
                        if(trackedObjects[i].visualizer.HasMarker(marker.id) == false)
                            continue;

                        if (trackedObjects[i].visualizer.shouldTrack)
                        {
                            trackedObjects[i].visualizer.SetConfidenceLevel(marker.id, marker.confidence);
                            trackedObjects[i].visualizer.SetPose(marker.id,marker.pose);
                            trackedObjects[i].visualizer.SetIsTracked(marker.id, true);
                            trackedObjects[i].visualizer.UpdatePrimaryMarker();

                            if (trackedObjects[i].visualizer.IsTracked == false) {

                                if (trackedObjects[i].visualizer.isDynamic) {
                                    //Debug.Log($"set marker {marker.id} to dynamic");
                                    VarjoMarkers.AddVarjoMarkerFlags(marker.id, VarjoMarkerFlags.DoPrediction);
                                }

                                if (trackedObjects[i].visualizer.timeout > 0)
                                {
                                    VarjoMarkers.SetVarjoMarkerTimeout(marker.id, trackedObjects[i].visualizer.timeout);
                                }
                            }
                            
                            if(marker.id != trackedObjects[i].visualizer.markers[0].id)
                                continue;

                            Transform currentTransform = trackedObjects[i].visualizer.gameObject.transform;
                            currentTransform.rotation = marker.pose.rotation;
                            currentTransform.position = marker.pose.position + currentTransform.TransformDirection(trackedObjects[i].visualizer.GetPosOffset(marker.id));
                            currentTransform.transform.Rotate(trackedObjects[i].visualizer.GetRotOffset(marker.id).eulerAngles);

                            currentTransform.position += trackedObjects[i].visualizer.markerAnkerPosOffset;

                            //currentTransform.localRotation = marker.pose.rotation * trackedObjects[i].visualizer.GetRotOffset(marker.id); 
                            currentTrackedMarker = marker.id;
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


                
                for (var i = 0; i < trackedObjects.Count; i++){
                        if(trackedObjects[i].visualizer.IsTracked)
                            currentTrackedObjectsCount++;
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

                // for(int i = 1; i < 7; i++)
                // {
                    
                //    try{
                //     Transform currentMarkerUI = uiParent.GetChild(i);
                //     currentMarkerUI.GetComponent<Image>().color = deactivatedColor;
                //     currentMarkerUI.GetChild(1).GetComponent<TextMeshProUGUI>().text = "0.0000";
                //    }
                //    catch(Exception e){
                //     // Child index not found exception ...
                //    }
                // }

                // // Update Debug UI
                // for(int i = 0; i < markers.Count; i++)
                // {

                //     try{
                //     Transform currentMarkerUI = uiParent.GetChild((int)markers[i].id-201);
                //     currentMarkerUI.GetComponent<Image>().color = activatedColor;
                //     currentMarkerUI.GetChild(1).GetComponent<TextMeshProUGUI>().text = markers[i].confidence.ToString("F4");
                //     }catch(Exception e){
                //         // Child index not found exception ...
                //     }
                // }

                // if(currentTrackedMarker != 0)
                // {
                //      try{
                //     Transform currentMarkerUI = uiParent.GetChild((int)currentTrackedMarker-201);
                //     currentMarkerUI.GetComponent<Image>().color = trackedColor;
                //      }
                //      catch(Exception e){
                //         // Child index not found exception ...
                //     }
                // }
            }
        }
    }
}

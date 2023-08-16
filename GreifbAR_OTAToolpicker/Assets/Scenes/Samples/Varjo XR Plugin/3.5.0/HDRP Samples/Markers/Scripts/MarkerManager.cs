using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Varjo.XR;

public class MarkerManager : MonoBehaviour
{
    private List<VarjoMarker> markers;
    private List<long> markerIds;
    private List<long> absentIds;
    private Dictionary<long, MarkerVisualizer> markerVisualizers;

    public Transform xrRig;

    public GameObject defaultPrefab;

    public MarkerObject[] markerObjects;

    public bool markersEnabled = true;
    private bool _markersEnabled;

    public long markerTimeout = 3000;
    private long _markerTimeout;

    private Transform markerTransform;

    public event EventHandler<MarkerDetectedEventArgs> MarkerDetectedEventHandler;
    public delegate void MarkerDetectedEvent(object sender, MarkerDetectedEventArgs e);

    private MarkerDetectedEvent markerEvent;

    [System.Serializable]
    public struct MarkerObject
    {
        public int id;
        public GameObject prefab;
        public bool isDynamic;        
    }

    void Start()
    {
        markers = new List<VarjoMarker>();
        markerIds = new List<long>();
        absentIds = new List<long>();
        markerVisualizers = new Dictionary<long, MarkerVisualizer>();
    }

    void Update()
    {
        if (markersEnabled != _markersEnabled)
        {
            markersEnabled = VarjoMarkers.EnableVarjoMarkers(markersEnabled);
            _markersEnabled = markersEnabled;
        }

        if (VarjoMarkers.IsVarjoMarkersEnabled())
        {
            markers.Clear();
            markerIds.Clear();
            int foundMarkers = VarjoMarkers.GetVarjoMarkers(out markers);
            if (markers.Count > 0)
            {
                foreach (var marker in markers)
                {
                    markerIds.Add(marker.id);
                    if (markerVisualizers.ContainsKey(marker.id))
                    {
                        UpdateMarkerVisualizer(marker);
                    }
                    else
                    {
                        CreateMarkerVisualizer(marker);
                        VarjoMarkers.SetVarjoMarkerTimeout(marker.id, markerTimeout);
                    }
                }

                if (markerTimeout != _markerTimeout)
                {
                    SetMarkerTimeOuts();
                    _markerTimeout = markerTimeout;
                }
            }

            VarjoMarkers.GetRemovedVarjoMarkerIds(out absentIds);

            foreach (var id in absentIds)
            {
                if (markerVisualizers.ContainsKey(id))
                {
                    Destroy(markerVisualizers[id].gameObject);
                    markerVisualizers.Remove(id);
                }
                markerIds.Remove(id);
            }
            absentIds.Clear();
        }

        if (markerIds.Count == 0 && markerVisualizers.Count > 0)
        {
            var ids = markerVisualizers.Keys.ToArray();
            foreach (var id in ids)
            {
                Destroy(markerVisualizers[id].gameObject);
                markerVisualizers.Remove(id);
            }
        }
    }

    void CreateMarkerVisualizer(VarjoMarker marker)
    {
        GameObject objectToSpawn = defaultPrefab;
        var query = markerObjects.Where(x => x.id == marker.id);
        if (query.Count() > 0)
        {
            MarkerObject mo = query.ElementAt(0);
            if(mo.prefab != null)
                objectToSpawn = mo.prefab;
        }

        GameObject go = Instantiate(objectToSpawn);
        markerTransform = go.transform;
        go.name = marker.id.ToString();
        markerTransform.SetParent(xrRig);
        MarkerVisualizer visualizer = go.GetComponent<MarkerVisualizer>();
        markerVisualizers.Add(marker.id, visualizer);
        visualizer.SetMarkerData(marker);
        OnMarkerDetected(new MarkerDetectedEventArgs(marker));
    }

    void UpdateMarkerVisualizer(VarjoMarker marker)
    {
        markerVisualizers[marker.id].SetMarkerData(marker);
    }

    void SetMarkerTimeOuts()
    {
        for (var i = 0; i < markerIds.Count; i++)
        {
            VarjoMarkers.SetVarjoMarkerTimeout(markerIds[i], markerTimeout);
        }
    }

    protected virtual void OnMarkerDetected(MarkerDetectedEventArgs e)
    {
        EventHandler<MarkerDetectedEventArgs> handler = MarkerDetectedEventHandler;
        if (handler != null)
        {
            handler(this, e);
        }
    }
}

public class MarkerDetectedEventArgs : EventArgs
{
    public VarjoMarker marker { get; set; }

    public MarkerDetectedEventArgs(VarjoMarker _marker)
    {
        marker = _marker;
    }
}

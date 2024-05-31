using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using Varjo.XR;


namespace NMY.OTAToolpicker
{
    public class OTAVarjoMarker : MonoBehaviour
    {
        public UnityEvent<InstrumentMarker> MarkerFound;
        public UnityEvent<InstrumentMarker> MarkerLost;

        public Vector3 markerAnkerPosOffset;
        public Vector3 markerAnkerRotOffset;
        // public Transform confidenceLabel;
        // public Transform idLabel;
        // private TextMeshProUGUI confidenceText;
        // private TextMeshProUGUI idText;

        [System.Serializable]
        public struct MarkerObject
        {
            public long id;
            public Vector3 posOffset;
            public Vector3 rotOffset;
            public float confidence;
            public Pose pose;
            public float diffFromCamera;

            public bool isTracked;

            public bool isPrimary;
        }

        [SerializeField]
        public MarkerObject[] markers;
        public bool shouldTrack;
        public bool isDynamic = false;
        public bool IsTracked{

            get {
                isTracked = markers.ToList().FindAll( x => x.isTracked == true).ToList().Count() > 0;
                return isTracked;
            }
        }
        [SerializeField]
        private bool isTracked;

        public long timeout = 1000;

        private InstrumentMarker instrumentMarker;

        private bool lastIsTracked = false;

        private OTAMarkerManager otaMarkerManager;

        public enum PrimaryMode {Confidence, Angle}
        public PrimaryMode primaryMode;

         
        public Vector3 initialPosition;



        private void Awake()
        {
            instrumentMarker = GetComponent<InstrumentMarker>();
            initialPosition = this.transform.position;
            if (instrumentMarker == null )
            {
                Debug.LogError($"OTAVarjoMarker[{name}]: no InstrumentMarker on GameObject!");
            }
            lastIsTracked = IsTracked;

            otaMarkerManager = FindObjectOfType<OTAMarkerManager>(true);
            // confidenceText = confidenceLabel.GetComponentInChildren<TextMeshProUGUI>();
            // idText = idLabel.GetComponentInChildren<TextMeshProUGUI>();
        }

        private void Start() 
        {
            if(isDynamic)
            {
                for (int i = 0; i < markers.Length; i++)
                {
                    VarjoMarkers.AddVarjoMarkerFlags(markers[i].id, VarjoMarkerFlags.DoPrediction);
                }
            }
        }

        private void FixedUpdate()
        {
            for (int i = 0; i < markers.Length; i++){
                markers[i].confidence = 0;
            }

            if (IsTracked != lastIsTracked){
                if (IsTracked){
                    MarkerFound.Invoke(instrumentMarker);
                }
                else
                {
                    MarkerLost.Invoke(instrumentMarker);
                    for (int i = 0; i < markers.Length; i++)
                    {
                        markers[i].confidence = 0;
                    }
                }
            }
            lastIsTracked = IsTracked;
        }

        
        public bool HasMovedSinceBeginning(){
            return !Mathf.Approximately(initialPosition.y,transform.position.y);
        }

        public void SetTrackingEnabled(bool enable)
        {
            shouldTrack = enable;
            if (shouldTrack)
                MarkerFound.Invoke(instrumentMarker);
            else
                MarkerLost.Invoke(instrumentMarker);
        }


        public Vector3 GetPosOffset(long markerID)
        {
            return markers.Single(x => x.id == markerID).posOffset + markerAnkerPosOffset;
        }


        public Quaternion GetRotOffset(long markerID)
        {
            Vector3 tmpVec = markers.Single(x => x.id == markerID).rotOffset;
            return Quaternion.Euler(tmpVec.x, tmpVec.y, tmpVec.z) * Quaternion.Euler(markerAnkerRotOffset);                                                                                  
        }

        public bool HasMarker(long markerID){

            foreach(var marker in markers){
                if(marker.id == markerID){
                    return true;
                }
            }


            return false;


        }

        public MarkerObject UpdatePrimaryMarker(){

                switch(primaryMode){
                    case PrimaryMode.Confidence:
                        markers = markers.OrderByDescending(x => x.confidence).ToArray();
                        break;
                    case PrimaryMode.Angle:
                        markers = markers.OrderBy(x => x.diffFromCamera).ToArray();
                        break;
                }
                markers[0].isPrimary = true;
                for(int i=1; i<markers.Length;i++){
                    markers[i].isPrimary = false;
                }
               return markers[0];
        }


        public void SetConfidenceLevel(long markerID, float confidenceLevel)
        {
            markers = markers.Select(n => { if (n.id == markerID) {n.confidence = confidenceLevel;} return n;}).ToArray();
            // confidenceText.text = ((int)(markers[0].confidence * 100)).ToString();
            // idText.text = markers[0].id.ToString();
        }

        public void SetIsTracked(long markerID, bool isTracked){

            markers = markers.Select(n => { if (n.id == markerID) {n.isTracked = isTracked;} return n;}).ToArray();

            for (int i = 0; i < markers.Length; i++){
                
                if(markers[i].id == markerID){
                    markers[i].isTracked = isTracked;
                    break;
                }
            }
        }

        public void ResetIsTracked(){
             for (int i = 0; i < markers.Length; i++){
                markers[i].isTracked = false;
                markers[i].diffFromCamera = 1000;
             }
        }

        public void SetPose(long markerID, Pose pose)
        {
            markers = markers.Select(n => { if (n.id == markerID) {n.pose = pose;} return n;}).ToArray();

            for (int i = 0; i < markers.Length; i++){
                
                if(markers[i].id == markerID){

                    markers[i].pose = pose;
                    markers[i].diffFromCamera = Vector3.Angle(-markers[i].pose.up, Camera.main.transform.forward);
                    break;
                }
            }
        }


    }
}

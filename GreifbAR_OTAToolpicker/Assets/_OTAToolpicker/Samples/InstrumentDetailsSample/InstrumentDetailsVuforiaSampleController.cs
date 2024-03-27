using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using NMY.OTAToolpicker.UI;

namespace NMY.OTAToolpicker {

    public class InstrumentDetailsVuforiaSampleController : MonoBehaviour
    {
        [SerializeField] private PlacementResult placementResult;

        [SerializeField] private PlacementResultView placementResultView;

        public void PrintDebug()
        {
            Debug.Log("Hello from InstrumentDetailsVuforiaSampleController");
        }

        void Start()
        {
            // placementResultView.SetResult(placementResult);
            // placementResultView.Show();
        }
    }

}
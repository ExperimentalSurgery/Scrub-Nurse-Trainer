using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using NMY.OTAToolpicker.UI;
using TMPro;

namespace NMY.OTAToolpicker
{
    public class OrientationCheckVuforiaSample : MonoBehaviour
    {
        [SerializeField] private InstrumentMarkerController instrumentMarkerController;
        [SerializeField] private TableMarker tableMarker;
        public TableMarker TableMarker => tableMarker;

        [SerializeField] private InstrumentTable instrumentTable;
        public InstrumentTable InstrumentTable => instrumentTable;
        [SerializeField] private DialogUI tableCalibrationDialogUI;

        [SerializeField] private PlacementVolume skalpellPlacementVolume;

        public TMP_Text angleText;

        private bool isTableCalibrated = false;

        async void Start()
        {
            foreach(var instrumentMarker in instrumentMarkerController.InstrumentMarkers)
            {
                instrumentMarker.PlaceableInstrument.InstrumentTable = instrumentTable;
            }

            await HelperTasks.CalibrateTable(tableMarker, instrumentTable, tableCalibrationDialogUI, true, default(CancellationToken));
            isTableCalibrated = true;

            skalpellPlacementVolume.Enable();
        }

        void Update()
        {
            if (!isTableCalibrated)
                return;

            string angle = skalpellPlacementVolume.currentAngle.ToString("00");
            if (skalpellPlacementVolume.IsInstrumentDirectionValid)
                angle = $"<color=green>{angle}°</color>";
            else
                angle = $"<color=red>{angle}°</color>";
            angleText.text = angle;
        }
    }
}

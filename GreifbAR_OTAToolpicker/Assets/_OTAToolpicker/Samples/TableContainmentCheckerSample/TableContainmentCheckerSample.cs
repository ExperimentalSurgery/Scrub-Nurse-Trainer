using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace NMY.OTAToolpicker
{
    public class TableContainmentCheckerSample : MonoBehaviour
    {
        [SerializeField] private TableContainmentChecker tableContainmentChecker;
        [SerializeField] private List<PlaceableInstrument> placeableInstruments = new List<PlaceableInstrument>();

        [SerializeField] private bool tableCheckEnabled = true;

        [Header("Debug colors")]
        [SerializeField] private Color insideColor = Color.green;
        [SerializeField] private Color outsideColor = Color.red;
        [SerializeField] private Color intersectingColor = Color.magenta;

        // Update is called once per frame
        void FixedUpdate()
        {
            if (tableCheckEnabled) 
                DoTableContainmentCheck();            
        }

        void DoTableContainmentCheck()
        {
            foreach (var pi in placeableInstruments) 
            {
                if (tableContainmentChecker.IsInsideTable(pi)) {
                    pi.ApplyColorToRenderers(insideColor);
                }
                else if (tableContainmentChecker.IsOutsideTable(pi)) {
                    pi.ApplyColorToRenderers(outsideColor);
                }
                else if (tableContainmentChecker.IsOverlappingTable(pi)) {
                    pi.ApplyColorToRenderers(intersectingColor);
                }
            }
        }
    }
}

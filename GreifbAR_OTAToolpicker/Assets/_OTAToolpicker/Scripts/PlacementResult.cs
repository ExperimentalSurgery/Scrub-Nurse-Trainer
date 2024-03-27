using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace NMY.OTAToolpicker
{
    [System.Serializable]
    public class PlacementResult
    {
        public PlacementResult(PlaceableInstrument placeableInstrument, bool isIntersectingTableBorder, bool isPositionValid, bool isDirectionValid)
        {
            this.placeableInstrument = placeableInstrument;
            this.isIntersectingTableBorder = isIntersectingTableBorder;
            this.isPositionValid = isPositionValid;
            this.isDirectionValid = isDirectionValid;
        }

        public PlaceableInstrument placeableInstrument;
        public bool isIntersectingTableBorder;
        public bool isPositionValid;
        public bool isDirectionValid;
    }
}

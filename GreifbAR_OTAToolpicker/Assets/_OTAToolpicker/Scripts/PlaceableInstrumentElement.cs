using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace NMY.OTAToolpicker
{
    [System.Flags]
    public enum PlaceableInstrumentElement
    {
        None = 0,
        InstrumentRenderer = 1,
        OutlineRenderer = 2,
        Collider = 4,
        Infospots = 8,
        ResultView = 16,
        All = InstrumentRenderer | OutlineRenderer | Collider | Infospots | ResultView
    }
}

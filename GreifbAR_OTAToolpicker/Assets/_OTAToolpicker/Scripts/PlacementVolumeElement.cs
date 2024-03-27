using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace NMY.OTAToolpicker
{
    [System.Flags]
    public enum PlacementVolumeElement
    {
        None = 0,
        InstrumentRenderer = 1,
        AreaRenderer = 2,
        Collider = 4,
        All = InstrumentRenderer | AreaRenderer | Collider
    }
}

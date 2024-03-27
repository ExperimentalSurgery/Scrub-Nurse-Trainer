using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace NMY.OTAToolpicker {

[System.Flags]
public enum InstrumentState
{
    Keine = 0,
    Geöffnet = 1,
    Geschlossen = 2,
    Eingerastet3 = 4,
    Eingerastet9 = 8,
    Raster3 = 16
}

// Offen, Geschlossen, Eingerastet (3 Stufen)
// Geöffnet, Geschlossen, Raster (3 Stufen)
// Geöffnet, Geschlossen, Raster (3 Stufen)
// Geöffnet, Geschlossen, Eingerastet (3 Stufen)
// Geöffnet, Geschlossen, Eingerastet (9 Raster)

}
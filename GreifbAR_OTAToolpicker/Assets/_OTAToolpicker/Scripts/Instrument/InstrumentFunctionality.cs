using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace NMY.OTAToolpicker {

[System.Flags]
public enum InstrumentFunctionality
{
    Unbekannt = 0,
    Halten = 1,
    Aufspreizen = 2,
    Präparation = 4,
    AllgemeinesGreifen = 8,
    Gewebetrennend = 16,
    Abklemmen = 32,
    Nähen = 64,
    Gewebegreifend = 128,
    Gewebeweghaltend = 256,
    Dissektion = 512,
    Ligatur = 1024,
    AbklemmenDesPeritonealsacks = 2048
}

}

// Sehr vielseitig, Halten von Material, Aufspreizen, Präparation, Allg. Greifen
// Gewebetrennend
// Allgemein
// Abklemmen des Peritonealsacks
// Nähen
// Gewebegreifend, Gewebehaltend
// Abklemmen des Peritonealsacks
// Dissektion
// Ligatur
// Geöffnet, Geschlossen, Raster (3 Stufen)
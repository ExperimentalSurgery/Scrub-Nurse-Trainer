using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace NMY.OTAToolpicker {

// Breite anatomische Backen
// Scharnier
// Konische Branchen
// Raster
// Brillengriff
// Stumpfe Blätter
// Einfaches Scharnier
// Grade Branchen
// Blatt
// Griff
// Kleiner stumpfer Haken
// Schaft
// Großer stumpfer Haken
// Atraumatische Backen
// Schlichte Rasten
// Schale
// Gebogene atraumatische Maulflächen
// Gelenk
// Haltebacken
// Atraumatische Maulflächen
// Geriefte Grifflächen
// Spiegel
// Federteil
// Stumpfes Blatt
// Spitzes Blatt
// Chirugisches Maulteil
// Wundhaken
// Haltering
// Handgriff

/// <summary>
/// In der Instrumententabelle ist dies die Spalte "Aufbau".
/// Im Rahmen des Konzepts bzw der Fragen aus Level1 wird aktuell nur "Raster"
/// überprüft, daher wird hier nur dieser Wert definiert.
/// </summary>

[System.Flags]
public enum InstrumentStructure
{
    Nichts = 0,
    Raster = 1,
    Griff = 2
}

}
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace NMY.OTAToolpicker {

// Keine
// Spitze
// Blätter
// Sehr hohe Gefahr bei eingelegtem Skalpellblatt
// Bei eingelegter Nadel: Spitze Nadel
// Maulteil

/// <summary>
/// Bezeichnung in Instrumententabelle "Gefahr"
/// </summary>
/// 
[System.Flags]
public enum InstrumentRisk
{
    Keine = 0,
    Spitze = 1,
    Blaetter = 2,
    SehrHoheGefahrBeiEingelegtemSkalpellblatt = 4,
    BeiEingelegterNadelSpitzeNadel = 8,
    Maulteil = 16
}

}
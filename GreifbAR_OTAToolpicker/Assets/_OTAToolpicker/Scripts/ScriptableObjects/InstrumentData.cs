using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Localization;

namespace NMY.OTAToolpicker {

[CreateAssetMenu(fileName = "InstrumentData", menuName = "OTA/InstrumentData", order = 1)]
public class InstrumentData : ScriptableObject
{
    [Tooltip("The title of the instrument.")]
    [SerializeField] private LocalizedString title;
    public LocalizedString Title => title;

    [Tooltip("The scope of the instrument.")]
    [SerializeField] private InstrumentScope scopes;
    public InstrumentScope Scopes => scopes;

    [Tooltip("The type of the instrument.")]
    [SerializeField] private InstrumentType instrumentType;
    public InstrumentType InstrumentType => instrumentType;

    [Tooltip("The functionality of the instrument.")]
    [SerializeField] private InstrumentFunctionality functions;
    public InstrumentFunctionality Functions => functions;

    [SerializeField] private InstrumentRisk risks;
    public InstrumentRisk Risks => risks;

    [Tooltip("The length of the instrument in millimeters.")]
    [SerializeField] private int lengthMM;
    public int LengthMM => lengthMM;

    [SerializeField] private InstrumentMaterial material;
    public InstrumentMaterial Material => material;

    [Tooltip("If <b>true</b>, the instrument is traumatic.")]
    [SerializeField] private bool isTraumatic;
    public bool IsTraumatic => isTraumatic;

    [SerializeField] private InstrumentState states;
    public InstrumentState States => states;

    [SerializeField] private InstrumentStructure structures;
    public InstrumentStructure Structures => structures;

    [Tooltip("The data used to evaluate the handover operation.")]
    [SerializeField] private HandoverData handoverData;
    public HandoverData HandoverData => handoverData;
}

}
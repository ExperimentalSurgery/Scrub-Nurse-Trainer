using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Localization;

namespace NMY.OTAToolpicker
{
    [CreateAssetMenu(fileName = "IdentificationQuestion", menuName = "OTA/IdentificationQuestion", order = 0)]
    /// <summary>
    /// Stores the data for a single identification question used in Level 1 quiz mode.
    /// </summary>
    /// <remarks>
    /// An identification question consists of a question text and a set of conditions that an instrument must fulfill to be considered as the correct answer.
    /// All conditions are optional and can be combined to form a more complex question.
    /// If a boolean condition is set to true, the corresponding value must be fulfilled by the instrument to be considered as the correct answer.
    /// If an enum condition is set to a specific value, the corresponding value must be fulfilled by the instrument to be considered as the correct answer.
    /// If an enum condition is set to None, the corresponding value is ignored and the instrument is considered as the correct answer if all other conditions are fulfilled.
    /// </remarks>
    public class IdentificationQuestion : ScriptableObject
    {
        [SerializeField] private LocalizedString question;
        public LocalizedString Question => question;

        [Header("Explicit instrument condition")]
        [Tooltip("When set, the instrument must be exactly of this type to fulfill the question condition.")]
        [SerializeField] private bool checkForExplicitInstrument = false;
        [SerializeField] private InstrumentData explicitInstrument;
        public InstrumentData ExplicitInstrument => explicitInstrument;

        [Header("Instrument scope condition")]
        [SerializeField] private InstrumentScope scopes;
        public InstrumentScope Scopes => scopes;

        [Header("Instrument type condition")]
        [SerializeField] private InstrumentType instrumentType;
        public InstrumentType InstrumentType => instrumentType;

        [Header("Instrument functionality condition")]
        [SerializeField] private InstrumentFunctionality functions;
        public InstrumentFunctionality Functions => functions;

        [Header("Instrument risk condition")]
        [SerializeField] private InstrumentRisk risks;
        public InstrumentRisk Risks => risks;

        [Header("Instrument length condition")]
        [SerializeField] private bool checkLength = false;
        [SerializeField] private int lengthMM;
        public int LengthMM => lengthMM;

        [Header("Instrument traumatic condition")]
        [Tooltip("Specifies if an instrument must be traumatic to fulfill the question condition.")]
        [SerializeField] private bool checkForTraumatic = false;
        [SerializeField] private bool isTraumatic;
        public bool IsTraumatic => isTraumatic;

        [Header("Instrument material condition")]
        [Tooltip("The material(s) an instrument must contain to fulfill the question condition.")]
        [SerializeField] private InstrumentMaterial materials;
        public InstrumentMaterial Materials => materials;

        [Header("Instrument structure condition")]
        [SerializeField] private InstrumentStructure structures;
        public InstrumentStructure Structures => structures;

        public bool IsFulfilledBy(InstrumentData instrument)
        {
            bool isFulfilled = true;

            if (scopes != InstrumentScope.Unbekannt && (instrument.Scopes & scopes) == 0)
                isFulfilled = false;

            if (instrumentType != InstrumentType.Unbekannt && instrumentType != instrument.InstrumentType)
                isFulfilled = false;

            if (functions != InstrumentFunctionality.Unbekannt && (instrument.Functions & functions) == 0)
                isFulfilled = false;

            if (risks != InstrumentRisk.Keine && (instrument.Risks & risks) == 0)
                isFulfilled = false;

            if (checkLength && instrument.LengthMM != lengthMM)
                isFulfilled = false;

            if (checkForExplicitInstrument && instrument != explicitInstrument)
                isFulfilled = false;

            if (checkForTraumatic && instrument.IsTraumatic != isTraumatic)
                isFulfilled = false;

            if (materials != InstrumentMaterial.Keins && (instrument.Material & materials) == 0)
                isFulfilled = false;

            if (structures != InstrumentStructure.Nichts && (instrument.Structures & structures) == 0)
                isFulfilled = false;

            return isFulfilled;
        }
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace NMY.OTAToolpicker {

[CreateAssetMenu(fileName = "InstrumentTypeIcons", menuName = "OTA/InstrumentTypeIcons", order = 1)]
public class InstrumentTypeIcons : ScriptableObject
{
    [System.Serializable]
    public struct InstrumentIcon
    {
        public InstrumentType instrumentType;
        public Sprite sprite;
    }

    [SerializeField] private List<InstrumentIcon> icons;

    [SerializeField] private Sprite defaultIcon;

    public Sprite GetIcon(InstrumentType instrumentType)
    {
        foreach (var icon in icons)
        {
            if (icon.instrumentType == instrumentType)
            {
                return icon.sprite;
            }
        }

        return defaultIcon;
    }
}

}
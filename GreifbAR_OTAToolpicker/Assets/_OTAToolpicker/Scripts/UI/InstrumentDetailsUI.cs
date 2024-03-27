using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System;
using TMPro;

namespace NMY.OTAToolpicker.UI {
public class InstrumentDetailsUI : MonoBehaviour
{
    [SerializeField] private InstrumentData data;

    [SerializeField] private InstrumentTypeIcons instrumentTypeIcons;

    [Header("Risk icons")]
    [SerializeField] private Sprite traumaticIcon;
    [SerializeField] private Sprite atraumaticIcon;

    [Header("UI Elements")]
    [SerializeField] private TextMeshProUGUI instrumentTitleText;
    [SerializeField] private Image traumaticImage;
    [SerializeField] private Image typeImage;
    [SerializeField] private TMP_Text functionsText;
    [SerializeField] private TMP_Text propertiesText;
    [SerializeField] private TMP_Text scopeText;
    [SerializeField] private TMP_Text traumaticText;
    [SerializeField] private TMP_Text typeText;

    [Multiline(6)]
    [SerializeField] private string propertiesTemplate = $"<b>Größe:</b> {0}mm<br><b>Material:</b> {1}<br><b>Zustände:</b> Geöffnet, Geschlossen, Raster (3 Stufen)<br><b>Gewebearten:</b> ???";

    public InstrumentData Data {
        get => data;
        set {
            data = value;
            UpdateView();
        }
    }

    private void OnEnable()
    {
        UpdateView();
    }

    public void UpdateView()
    {
        if (data == null) return;

        instrumentTitleText.text = data.Title.GetLocalizedString();
        traumaticImage.sprite = data.IsTraumatic ? traumaticIcon : atraumaticIcon;
        traumaticText.text = data.IsTraumatic ? "Traumatisch!" : "Atraumatisch";
        typeImage.sprite = instrumentTypeIcons.GetIcon(data.InstrumentType);
        typeText.text = data.InstrumentType.ToString();
        propertiesText.text = string.Format(propertiesTemplate,
            data.LengthMM,
            GetEnumFlagsAsCommaSeparatedString(()=>data.Material),
            GetEnumFlagsAsCommaSeparatedString(()=>data.States));
        functionsText.text = GetEnumFlagsAsBulletString(() => data.Functions);
        scopeText.text = GetEnumFlagsAsBulletString(() => data.Scopes);
    }

    static IEnumerable<Enum> GetFlags(Enum input)
    {
        foreach (Enum value in Enum.GetValues(input.GetType()))
            if (input.HasFlag(value) && value.GetHashCode()!=0)
                yield return value;
    }

    static public string GetEnumFlagsAsCommaSeparatedString(Func<Enum> func)
    {
        var flags = GetFlags(func());
        string scopeString = "";
        foreach (var flag in flags)
        {
            scopeString += $"{flag}, ";
        }
        return scopeString.TrimEnd(',', ' ');
    }

    static public string GetEnumFlagsAsBulletString(Func<Enum> func)
    {
        var flags = GetFlags(func());
        string scopeString = "";
        foreach (var flag in flags)
        {
            scopeString += $"<indent=0em>\u2022<indent=1em>{flag}" + "<br>";
        }
        return scopeString.TrimEnd('\n', ' ');
    }
}

}
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using NMY.OTAToolpicker.UI;

namespace NMY.OTAToolpicker {

public class InstrumentDetailsSampleController : MonoBehaviour
{
    [SerializeField] private List<InstrumentData> instrumentList;

    [SerializeField] private InstrumentDetailsUI instrumentDetailsUI;

    [SerializeField] private Button nextButton;
    [SerializeField] private Button previousButton;

    [SerializeField] private int initialInstrumentIndex = 0;
    private int currentInstrumentIndex = 0;

    void Start()
    {
        nextButton.onClick.AddListener(NextInstrument);
        previousButton.onClick.AddListener(PreviousInstrument);

        currentInstrumentIndex = initialInstrumentIndex;

        instrumentDetailsUI.Data = instrumentList[currentInstrumentIndex];
    }

    private void NextInstrument()
    {
        currentInstrumentIndex++;
        if (currentInstrumentIndex >= instrumentList.Count)
        {
            currentInstrumentIndex = 0;
        }

        instrumentDetailsUI.Data = instrumentList[currentInstrumentIndex];
    }

    private void PreviousInstrument()
    {
        currentInstrumentIndex--;
        if (currentInstrumentIndex < 0)
        {
            currentInstrumentIndex = instrumentList.Count - 1;
        }

        instrumentDetailsUI.Data = instrumentList[currentInstrumentIndex];
    }
}

}
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

namespace NMY.OTAToolpicker
{
    public class PlaceableInstrumentSampleController : MonoBehaviour
    {
        [SerializeField] private List<PlaceableInstrument> instruments = new();
        [SerializeField] private int currentInstrumentIndex = 0;

        [SerializeField] private Button nextButton;
        [SerializeField] private Button previousButton;

        [SerializeField] private Button showOutlineButton;
        [SerializeField] private Button hideOutlineButton;

        [SerializeField] private Button showMeshButton;
        [SerializeField] private Button hideMeshButton;

        [SerializeField] private Button showInfoSpotsButton;
        [SerializeField] private Button hideInfoSpotsButton;

        void Start()
        {
            HideAllInstruments();
            if (currentInstrumentIndex >= instruments.Count)
                currentInstrumentIndex = 0;

            ShowInstrument(instruments[currentInstrumentIndex]);

            hideOutlineButton.onClick.AddListener(() => instruments[currentInstrumentIndex].HideOutline());
            showOutlineButton.onClick.AddListener(() => instruments[currentInstrumentIndex].ShowOutline());

            hideMeshButton.onClick.AddListener(() => instruments[currentInstrumentIndex].HideMesh());
            showMeshButton.onClick.AddListener(() => instruments[currentInstrumentIndex].ShowMesh());

            hideInfoSpotsButton.onClick.AddListener(() => instruments[currentInstrumentIndex].HideAllInformationSpots());
            showInfoSpotsButton.onClick.AddListener(() => instruments[currentInstrumentIndex].ShowAllInformationSpots());

            nextButton.onClick.AddListener(() => GotoNextInstrument());

            previousButton.onClick.AddListener(() => GotoPreviousInstrument());
        }

        public void HideInstrument(PlaceableInstrument instrument)
        {
            instrument.HideMesh();
            instrument.HideOutline();
            instrument.HideAllInformationSpots();
        }

        public void ShowInstrument(PlaceableInstrument instrument)
        {
            instrument.ShowMesh();
            instrument.ShowOutline();
            instrument.ShowAllInformationSpots();
        }

        public void HideAllInstruments()
        {
            foreach (var instrument in instruments)
                HideInstrument(instrument);
        }

        public void ShowAllInstruments()
        {
            foreach (var instrument in instruments)
                ShowInstrument(instrument);
        }

        public void GotoNextInstrument()
        {
            HideInstrument(instruments[currentInstrumentIndex]);

            currentInstrumentIndex++;
            if (currentInstrumentIndex >= instruments.Count)
                currentInstrumentIndex = 0;

            ShowInstrument(instruments[currentInstrumentIndex]);
        }

        public void GotoPreviousInstrument()
        {
            HideInstrument(instruments[currentInstrumentIndex]);

            currentInstrumentIndex--;
            if (currentInstrumentIndex < 0)
                currentInstrumentIndex = instruments.Count - 1;

            ShowInstrument(instruments[currentInstrumentIndex]);
        }
    }
}

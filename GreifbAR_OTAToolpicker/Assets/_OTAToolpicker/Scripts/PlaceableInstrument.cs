using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Serialization;
using EPOOutline;
using UnityEngine.Rendering;

namespace NMY.OTAToolpicker
{
    public enum PlacementVolumeType
    {
        OuterTable,
        InnerTable,
        PlacementVolume
    }

    public class PlaceableInstrument : MonoBehaviour
    {
        [Header("Instrument")]
        [SerializeField] private InstrumentData instrumentData;
        public InstrumentData InstrumentData => instrumentData;
        [SerializeField] private Collider instrumentCollider;
        [SerializeField] private PlacementResultView resultView;
        [SerializeField] private bool isResultViewInitiallyShown = false;
        public PlacementResultView ResultView => resultView;
        [Tooltip("The (optional) forward direction of the instrument. If null, direction check will be skipped.")]
        [SerializeField] private Transform forwardDirection;
        public Transform ForwardDirection => forwardDirection;
        // [SerializeField] private Outlinable outlinable;
        [SerializeField] private List<InformationSpot> informationSpots = new();
        [SerializeField] private List<Renderer> instrumentRenderers = new();

        [Header("Scene references")]
        [SerializeField] private InstrumentTable instrumentTable;
        public InstrumentTable InstrumentTable  {
            get => instrumentTable;
            set => instrumentTable = value;
        }

        /// <summary>
        /// <c>true</c> if the instrument is within the table, <c>false</c> otherwise.
        /// If the <see cref="InstrumentTable"/> is not set, this property will always return <c>false</c>.
        /// </summary>
        public bool IsWithinTable => instrumentTable!=null && instrumentTable.IsWithinTableBorder(this);
        /// <summary>
        /// <c>true</c> if the instrument is intersecting the table, <c>false</c> otherwise.
        /// If the <see cref="InstrumentTable"/> is not set, this property will always return <c>false</c>.
        /// </summary>
        public bool IsIntersectingTableBorder => instrumentTable!=null && instrumentTable.IsIntersectingTableBorder(this);
        /// <summary>
        /// <c>true</c> if the instrument is within the placement volume, <c>false</c> otherwise.
        /// If the <see cref="InstrumentTable"/> is not set, this property will always return <c>false</c>.
        /// </summary>
        public bool IsWithinPlacementVolume => instrumentTable!=null && instrumentTable.GetPlacementVolume(this).IsInstrumentWithin;
        /// <summary>
        /// <c>true</c> if the instrument is intersecting the placement volume, <c>false</c> otherwise.
        /// If the <see cref="InstrumentTable"/> is not set, this property will always return <c>false</c>.
        /// </summary>
        public bool IsIntersectingPlacementVolume => instrumentTable!=null && instrumentTable.GetPlacementVolume(this).IsInstrumentIntersecting;
        /// <summary>
        /// <c>true</c> if the instrument is within the placement volume and the direction is valid, <c>false</c> otherwise.
        /// If the <see cref="InstrumentTable"/> is not set, this property will always return <c>false</c>.
        /// </summary>
        public bool IsDirectionValid => instrumentTable!=null && instrumentTable.GetPlacementVolume(this).IsInstrumentDirectionValid;

        private List<LocalKeyword> shaderOutlineKeywords = new();

        void Awake()
        {


            if (resultView == null)
                resultView = GetComponentInChildren<PlacementResultView>();

            if (resultView != null)
            {
                if (isResultViewInitiallyShown)
                    resultView.Show();
                else
                    resultView.Hide();
            }

            if (informationSpots.Count == 0)
                informationSpots = FindInformationSpots();
        }

        void Start()
        {
            // Assert.IsNotNull(instrumentTable, "PlaceableInstrument: instrumentTable is null!");
            if (instrumentTable == null)
                Debug.LogWarning($"PlaceableInstrument: instrumentTable of '{InstrumentData.Title.GetLocalizedString()}' is null! Instrument will not be placeable.");
        }

        public void ApplyColorToRenderers(Color color)
        {
            color.a = 0f;
            foreach (var renderer in instrumentRenderers)
            {
                renderer.material.color = color;
                // renderer.materials[1].SetColor("_OutlineColor", color);
            }
            color.a = 1f;
        }

        public void ShowMesh()
        {
            foreach (var renderer in instrumentRenderers)
            {
                renderer.enabled = true;
                Color c = renderer.material.color;
                c.a = 1f;
                renderer.material.color = c;
            }
        }

        public void HideMesh()
        {
            foreach (var renderer in instrumentRenderers)
            {
                renderer.enabled = true;
                Color c = renderer.material.color;
                c.a = 0f;
                renderer.material.color = c;
            }
        }

        public void ShowOutline()
        {
            // if (outlinable != null)
            // {
            //     outlinable.enabled = true;
            // }

            foreach(var renderer in instrumentRenderers)
            {
                renderer.materials[1].EnableKeyword("OUTLINES_ON");
            }

        }
        public void HideOutline()
        {
            // if (outlinable != null)
            // {
            //     outlinable.enabled = false;
            // }

            foreach(var renderer in instrumentRenderers)
            {
                renderer.materials[1].DisableKeyword("OUTLINES_ON");
            }
        }
        public void SetOutlineColor(Color color)
        {
            // if (outlinable != null)
            // {
            //     outlinable.OutlineParameters.Color = color;
            // }

            foreach(var renderer in instrumentRenderers)
            {
                renderer.materials[1].SetColor("_OutlineColor", color);
            }
        }

        public void ShowAllInformationSpots()
        {
            foreach (var spot in informationSpots)
            {
                spot.gameObject.SetActive(true);
            }
        }

        public void EnableCollider()
        {
            if (instrumentCollider != null)
                instrumentCollider.enabled = true;
        }

        public void DisableCollider()
        {
            if (instrumentCollider != null)
                instrumentCollider.enabled = false;
        }

        public void HideAllInformationSpots()
        {
            foreach (var spot in informationSpots)
            {
                spot.gameObject.SetActive(false);
            }
        }

        private List<InformationSpot> FindInformationSpots()
        {
            var spots = new List<InformationSpot>();
            foreach (var spot in GetComponentsInChildren<InformationSpot>())
            {
                spots.Add(spot);
            }
            return spots;
        }

        public void UpdateInstrumentElementVisibility(PlaceableInstrumentElement elementsDisplayed)
        {
            if(elementsDisplayed.HasFlag(PlaceableInstrumentElement.InstrumentRenderer))
                ShowMesh();
            else
                HideMesh();

            if(elementsDisplayed.HasFlag(PlaceableInstrumentElement.OutlineRenderer))
                ShowOutline();
            else
                HideOutline();

            if(elementsDisplayed.HasFlag(PlaceableInstrumentElement.Infospots))
                ShowAllInformationSpots();
            else
                HideAllInformationSpots();

            if(elementsDisplayed.HasFlag(PlaceableInstrumentElement.Collider))
                EnableCollider();
            else
                DisableCollider();

            if(elementsDisplayed.HasFlag(PlaceableInstrumentElement.ResultView))
                ResultView.Show();
            else
                ResultView.Hide();
        }

        private void CreateShaderOutlineKeywords()
        {
            shaderOutlineKeywords.Clear();
            foreach (var renderer in instrumentRenderers)
            {
                var material = renderer.materials[1];
                if (material == null)
                    continue;

                var shader = material.shader;
                if (shader == null)
                    continue;

                shaderOutlineKeywords.Add(new LocalKeyword(shader, "OUTLINES_ON"));
            }
        }
    }
}

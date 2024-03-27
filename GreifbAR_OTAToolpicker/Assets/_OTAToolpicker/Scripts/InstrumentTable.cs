using System.Collections;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using System.Threading;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.Assertions;

namespace NMY.OTAToolpicker
{
    public class InstrumentTable : MonoBehaviour
    {
        [Tooltip("The transform of the table plane. Must have a BoxCollider and MeshRenderer component. The renderer is used during table calibration.")]
        [FormerlySerializedAs("tableMeshTransform")]
        [SerializeField] private Transform tablePlaneTransform;
        public Transform TablePlaneTransform => tablePlaneTransform;

        private MeshRenderer tablePlaneRenderer;
        public MeshRenderer TablePlaneRenderer => tablePlaneRenderer;

        private BoxCollider tablePlaneBoxCollider;
        public BoxCollider TablePlaneBoxCollider => tablePlaneBoxCollider;


        [SerializeField] private Transform zMinusBoxTrans;
        public Transform ZMinusBoxTrans => zMinusBoxTrans;
        [SerializeField] private Transform zPlusBoxTrans;
        public Transform ZPlusBoxTrans => zPlusBoxTrans;
        [SerializeField] private Transform xMinusBoxTrans;
        public Transform XMinusBoxTrans => xMinusBoxTrans;
        [SerializeField] private Transform xPlusBoxTrans;
        public Transform XPlusBoxTrans => xPlusBoxTrans;

        [Header("Table Border Visualization")]
        [FormerlySerializedAs("innerTableBorderVisGO")]
        [SerializeField] private GameObject tableBorderVisGO;
        [FormerlySerializedAs("showInnerTableBorder")]
        [SerializeField] private bool showTableBorder = false;

        [Header("Level 2 Rule 2 Arrows")]
        [SerializeField] private GameObject rightPointingArrow;
        [SerializeField] private GameObject forwardPointingArrow;

        [Header("Level 3 Rule 3 Area")]
        [SerializeField] private GameObject rule3Area;

        [Header("Table Containment Checker")]
        [SerializeField] private TableContainmentChecker tableContainmentChecker;

        [Header("Placement Volumes")]
        [SerializeField] private bool autoSearchPlacementVolumes = true;
        [SerializeField] private List<PlacementVolume> placementVolumes = new List<PlacementVolume>();

        private int currentPlacementVolumeIndex = 0;
        public int CurrentPlacementVolumeIndex => currentPlacementVolumeIndex;
        public PlacementVolume CurrentPlacementVolume => placementVolumes[currentPlacementVolumeIndex];

        void Awake()
        {
            Assert.IsNotNull(tablePlaneTransform, "InstrumentTable: tablePlaneTransform is null!");

            tablePlaneBoxCollider = tablePlaneTransform.GetComponent<BoxCollider>();
            Assert.IsNotNull(tablePlaneBoxCollider, "InstrumentTable: tablePlaneBoxCollider is null!");

            tablePlaneRenderer = tablePlaneTransform.GetComponent<MeshRenderer>();
            Assert.IsNotNull(tablePlaneRenderer, "InstrumentTable: tablePlaneRenderer is null!");

            if (autoSearchPlacementVolumes) GetPlacementVolumes();
        }

        void Start()
        {
            if (showTableBorder) ShowTableBorder();
            else HideTableBorder();
        }

        void AdjustBoxCollider(BoxCollider sourceBox, BoxCollider targetBox, float padding)
        {
            targetBox.center = sourceBox.center;
            targetBox.size = sourceBox.size - new Vector3(padding*2f, 0f, padding*2f);
        }

        public void ShowTableBorder()
        {
            tableBorderVisGO.SetActive(true);
            foreach(Renderer renderer in tableBorderVisGO.GetComponentsInChildren<Renderer>()) {
                renderer.enabled = true;
            }
        }

        public void HideTableBorder()
        {
            tableBorderVisGO.SetActive(false);
        }

        async public UniTask ShowLevel2Rule2Arrows(float delayS, CancellationToken ct)
        {
            try {
                await UniTask.Delay((int)(delayS*1000), cancellationToken: ct);
                rightPointingArrow.SetActive(true);
                rightPointingArrow.GetComponentInChildren<MeshRenderer>().enabled = true;
                await UniTask.Delay(8000, cancellationToken: ct);
                forwardPointingArrow.SetActive(true);
                forwardPointingArrow.GetComponentInChildren<MeshRenderer>().enabled = true;
                await UniTask.Delay(10000, cancellationToken: ct);
            }
            catch (System.OperationCanceledException) {}
            finally {
                HideLevel2Rule2Arrows();
            }
        }

        public void HideLevel2Rule2Arrows()
        {
            rightPointingArrow.SetActive(false);
            forwardPointingArrow.SetActive(false);
        }

        async public UniTask ShowLevel3Rule3Area(float delayS, CancellationToken ct)
        {
            try {
                await UniTask.Delay((int)(delayS*1000), cancellationToken: ct);
                rule3Area.SetActive(true);
                rule3Area.GetComponentInChildren<MeshRenderer>().enabled = true;
                await UniTask.Delay(10000, cancellationToken: ct);
            }
            catch (System.OperationCanceledException) {}
            finally {
                HideLevel3Rule3Area();
            }
        }

        public void HideLevel3Rule3Area()
        {
            rule3Area.SetActive(false);
        }

        public void SetPlacementVolumeDisplayedElements(PlacementVolumeElement elements)
        {
            foreach (PlacementVolume pv in placementVolumes) {
                pv.ElementsDisplayed = elements;
            }
        }

        public void DisableAllPlacementVolumes()
        {
            foreach (PlacementVolume pv in placementVolumes) {
                pv.Disable();
            }
        }

        public void EnablePlacementVolume(int index)
        {
            if (index == -1) return;

            DisableAllPlacementVolumes();
            // placementVolumes[index].gameObject.SetActive(true);
            // the placement volumes are a child of the Vuforia tracking observer which
            // disables all MeshRenderer and collider components when the tracking is disabled. When
            // the GameObject of the volume is activated, the MeshRenderer and BoxCollider components are
            // still disabled. Therefore, we need to enable these components manually
            placementVolumes[index].Enable();

            currentPlacementVolumeIndex = index;
        }

        public void EnablePlacementVolume(PlaceableInstrument placeableInstrument)
        {            
            EnablePlacementVolume(placementVolumes.FindIndex(pv => pv.PlaceableInstrument == placeableInstrument));
        }

        public void DisablePlacementVolume(PlaceableInstrument placeableInstrument)
        {
            PlacementVolume placementVolume = placementVolumes.Find(pv => pv.PlaceableInstrument == placeableInstrument);
            placementVolume?.Disable();
        }

        private List<PlacementVolume> GetPlacementVolumes()
        {
            placementVolumes.Clear();
            placementVolumes.AddRange(GetComponentsInChildren<PlacementVolume>());
            return placementVolumes;
        }

        public PlacementVolume GetPlacementVolume(InstrumentData instrumentData)
        {
            return placementVolumes.Find(pv => pv.PlaceableInstrument.InstrumentData == instrumentData);
        }

        public PlacementVolume GetPlacementVolume(PlaceableInstrument placeableInstrument)
        {
            return placementVolumes.Find(pv => pv.PlaceableInstrument == placeableInstrument);
        }

        public bool IsWithinTableBorder(PlaceableInstrument instrument)
        {
            return tableContainmentChecker.IsInsideTable(instrument);
        }

        public bool IsIntersectingTableBorder(PlaceableInstrument instrument)
        {
            return tableContainmentChecker.IsOverlappingTable(instrument);
        }

        public bool IsOutsideTableBorder(PlaceableInstrument instrument)
        {
            return tableContainmentChecker.IsOutsideTable(instrument);
        }

        public void CheckPlacement()
        {
            tableContainmentChecker.DoPhysicsOverlapCheck();
        }
    }
}

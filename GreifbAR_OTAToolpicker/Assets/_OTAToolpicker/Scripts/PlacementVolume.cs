using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

namespace NMY.OTAToolpicker
{
    public class PlacementVolume : MonoBehaviour
    {
        [SerializeField] private PlaceableInstrument placeableInstrument;
        public PlaceableInstrument PlaceableInstrument => placeableInstrument;

        [SerializeField] private PlacementVolumeElement elementsDisplayed = PlacementVolumeElement.All;
        public PlacementVolumeElement ElementsDisplayed {
            get => elementsDisplayed;
            set => elementsDisplayed = value;
        }

        [Header("Colors")]
        [SerializeField] private Color intersectingTableColor = Color.red;
        [SerializeField] private Color intersectingVolumeColor = Color.magenta;
        [SerializeField] private Color withinVolumeColor = Color.green;

        [Header("Collision")]
        [SerializeField] private BoxCollider volumeCollider;
        public BoxCollider VolumeCollider => volumeCollider;

        [SerializeField] private LayerMask layersToCheck = 1 << 0;

        private List<Renderer> renderers = new List<Renderer>();
        [Header("Renderer")]
        [SerializeField] private List<Renderer> instrumentRenderers;
        [SerializeField] private Renderer areaGuidancePlane;

        [Header("Forward direction")]
        [SerializeField] private bool isDirectionCheckEnabled = false;
        public bool IsDirectionCheckEnabled => isDirectionCheckEnabled;
        [SerializeField] private Transform forwardDirection;
        [SerializeField] private float maxForwardDeltaDegree = 10f;

        private Transform zMinusTrans;
        private Transform zPlusTrans;
        private Transform xMinusTrans;
        private Transform xPlusTrans;

        private BoxCollider zMinusCollider;
        private BoxCollider zPlusCollider;
        private BoxCollider xMinusCollider;
        private BoxCollider xPlusCollider;

        private bool isInstrumentWithin = false;
        public bool IsInstrumentWithin => isInstrumentWithin;
        private bool isInstrumentIntersecting = false;
        public bool IsInstrumentIntersecting => isInstrumentIntersecting;
        private bool isInstrumentDirectionValid = false;
        public bool IsInstrumentDirectionValid => isInstrumentDirectionValid;

        public float currentAngle = 0f;

        void Awake()
        {
           renderers.AddRange(GetComponentsInChildren<Renderer>());
           CreateSidePlanes();
        }

        void Start()
        {
            // make some tranform parameter checks to ensure correct behaviour
            Assert.IsTrue(transform.localScale == Vector3.one);
            Assert.IsTrue(volumeCollider.transform.localPosition.x==0f);
            Assert.IsTrue(volumeCollider.transform.localPosition.z==0f);
        }

        public void Enable()
        {
            enabled = true;

            if (elementsDisplayed.HasFlag(PlacementVolumeElement.AreaRenderer))
                EnableAreaGuidanceRenderer();
            if (elementsDisplayed.HasFlag(PlacementVolumeElement.InstrumentRenderer))
                EnableInstrumentRenderer();
            if (elementsDisplayed.HasFlag(PlacementVolumeElement.Collider))
                EnableCollider();
        }

        public void Disable()
        {
            enabled = false;

            DisableAreaGuidanceRenderer();
            DisableInstrumentRenderer();
            DisableCollider();
        }

        public void EnableAreaGuidanceRenderer() => areaGuidancePlane.enabled = true;
        public void DisableAreaGuidanceRenderer() => areaGuidancePlane.enabled = false;

        public void EnableInstrumentRenderer()
        {
            foreach (var renderer in instrumentRenderers)
                renderer.enabled = true;
        }
        public void DisableInstrumentRenderer()
        {
            foreach (var renderer in instrumentRenderers)
                renderer.enabled = false;
        }

        public void EnableCollider()
        {
            volumeCollider.enabled = true;
            zMinusCollider.enabled = true;
            zPlusCollider.enabled = true;
            xMinusCollider.enabled = true;
            xPlusCollider.enabled = true;
        }
        public void DisableCollider()
        {
            volumeCollider.enabled = false;
            zMinusCollider.enabled = false;
            zPlusCollider.enabled = false;
            xMinusCollider.enabled = false;
            xPlusCollider.enabled = false;
        }

        private void CreateSidePlanes()
        {
            Transform volumeColliderTrans = volumeCollider.transform;
            Vector3 volumeColliderPos = volumeColliderTrans.localPosition;
            // Vector3 volumeColliderLocalScale = volumeColliderTrans.localScale;

            zMinusTrans = new GameObject("ZMinus").transform;
            zMinusTrans.SetParent(transform);
            zMinusTrans.SetLocalPositionAndRotation(new Vector3(0, volumeColliderPos.y, -volumeColliderTrans.localScale.z / 2f), Quaternion.identity);
            zMinusCollider = zMinusTrans.gameObject.AddComponent<BoxCollider>();
            zMinusCollider.size = new Vector3(volumeColliderTrans.localScale.x, volumeColliderTrans.localScale.y, 0.001f);

            zPlusTrans = new GameObject("ZPlus").transform;
            zPlusTrans.SetParent(transform);
            zPlusTrans.SetLocalPositionAndRotation(new Vector3(0, volumeColliderPos.y, volumeColliderTrans.localScale.z / 2f), Quaternion.identity);
            zPlusCollider = zPlusTrans.gameObject.AddComponent<BoxCollider>();
            zPlusCollider.size = new Vector3(volumeColliderTrans.localScale.x, volumeColliderTrans.localScale.y, 0.001f);

            xMinusTrans = new GameObject("XMinus").transform;
            xMinusTrans.SetParent(transform);
            xMinusTrans.SetLocalPositionAndRotation(new Vector3(-volumeColliderTrans.localScale.x / 2f, volumeColliderPos.y, 0f), Quaternion.identity);
            xMinusCollider = xMinusTrans.gameObject.AddComponent<BoxCollider>();
            xMinusCollider.size = new Vector3(0.001f, volumeColliderTrans.localScale.y, volumeColliderTrans.localScale.z);

            xPlusTrans = new GameObject("XPlus").transform;
            xPlusTrans.SetParent(transform);
            xPlusTrans.SetLocalPositionAndRotation(new Vector3(volumeColliderTrans.localScale.x / 2f, volumeColliderPos.y, 0f), Quaternion.identity);
            xPlusCollider = xPlusTrans.gameObject.AddComponent<BoxCollider>();
            xPlusCollider.size = new Vector3(0.001f, volumeColliderTrans.localScale.y, volumeColliderTrans.localScale.z);
        }

        void FixedUpdate()
        {
            DoPhysicsOverlapCheck();

            if (isDirectionCheckEnabled)
            {
                currentAngle = DoDirectionCheck2(placeableInstrument.ForwardDirection, transform.up);
            }
        }

        private float DoDirectionCheck(Transform instrumentDirection)
        {
            // float angle = Vector3.Angle(instrumentDirection.forward, forwardDirection.forward);
            // float angle = Vector3.SignedAngle(instrumentDirection.forward, forwardDirection.forward, Vector3.up);
            var q = Quaternion.FromToRotation(placeableInstrument.ForwardDirection.forward, forwardDirection.forward);
            float yRot = q.eulerAngles.z;
            if (yRot > 180f)
                yRot -= 360f;
            isInstrumentDirectionValid = Mathf.Abs(yRot) <= maxForwardDeltaDegree;
            return yRot;
        }

        /// <summary>
        /// Returns the angle between the forward direction of the instrument and the forward direction of the placement volume.
        /// This one seems to work more reliably than the one based on Quaternion.FromToRotation.
        /// </summary>
        /// <param name="instrumentDirection"></param>
        /// <param name="up"></param>
        /// <returns></returns>
        private float DoDirectionCheck2(Transform instrumentDirection, Vector3 up)
        {
            // float angle = Vector3.Angle(instrumentDirection.forward, forwardDirection.forward);
            float angle = Vector3.SignedAngle(instrumentDirection.forward, forwardDirection.forward, up);
            isInstrumentDirectionValid = Mathf.Abs(angle) <= maxForwardDeltaDegree;
            return angle;
            // var q = Quaternion.FromToRotation(placeableInstrument.ForwardDirection.forward, forwardDirection.forward);
            // float yRot = q.eulerAngles.z;
            // if (yRot > 180f)
            //     yRot -= 360f;
            // isInstrumentDirectionValid = Mathf.Abs(yRot) <= maxForwardDeltaDegree;
            // return yRot;
        }

        private void DoPhysicsOverlapCheck()
        {
            if (placeableInstrument == null)
                return;

            // placeableInstrument.ApplyColorToRenderers(intersectingTableColor);
            placeableInstrument.SetOutlineColor(intersectingTableColor);

            Collider[] collidersHittingBox = Physics.OverlapBox(volumeCollider.transform.position, volumeCollider.transform.localScale*0.5f, transform.rotation, layersToCheck);

            Collider[] collidersHittingZMinus = Physics.OverlapBox(zMinusTrans.position, zMinusCollider.size*0.5f, zMinusTrans.rotation, layersToCheck);
            Collider[] collidersHittingZPlus = Physics.OverlapBox(zPlusTrans.position, zPlusCollider.size*0.5f, zPlusTrans.rotation, layersToCheck);
            Collider[] collidersHittingXMinus = Physics.OverlapBox(xMinusTrans.position, xMinusCollider.size*0.5f, xMinusTrans.rotation, layersToCheck);
            Collider[] collidersHittingXPlus = Physics.OverlapBox(xPlusTrans.position, xPlusCollider.size*0.5f, xPlusTrans.rotation, layersToCheck);

            isInstrumentWithin = false;
            isInstrumentIntersecting = false;

            if (collidersHittingBox.Length > 0)
            {
                foreach(Collider c in collidersHittingBox)
                {
                    PlaceableInstrument pi = c.GetComponentInParent<PlaceableInstrument>();
                    Assert.IsNotNull(pi, "PlacementVolume: Instrument is not found in the hierarchy!");
                    if (pi && pi == placeableInstrument)
                    {
                        // pi.ApplyColorToRenderers(withinVolumeColor);
                        pi.SetOutlineColor(withinVolumeColor);
                        isInstrumentWithin = true;
                        continue;
                    }
                }
            }

            bool isIntersectingZMinus = IsIntersectingSide(collidersHittingZMinus);
            bool isIntersectingZPlus = IsIntersectingSide(collidersHittingZPlus);
            bool isIntersectingXMinus = IsIntersectingSide(collidersHittingXMinus);
            bool isIntersectingXPlus = IsIntersectingSide(collidersHittingXPlus);

            isInstrumentIntersecting = isIntersectingZMinus || isIntersectingZPlus || isIntersectingXMinus || isIntersectingXPlus;
            isInstrumentWithin = isInstrumentWithin && !isInstrumentIntersecting;

            if (isInstrumentIntersecting) {
                // placeableInstrument.ApplyColorToRenderers(intersectingVolumeColor);
                placeableInstrument.SetOutlineColor(intersectingVolumeColor);
            }
        }

        private bool IsIntersectingSide(Collider[] colliders)
        {
            foreach(Collider c in colliders)
            {
                PlaceableInstrument pi = c.GetComponentInParent<PlaceableInstrument>();
                if (pi && pi == placeableInstrument)
                {
                    return true;
                }
            }
            return false;
        }

        [ContextMenu("Copy Collider Scale to Area Guidance Plane")]
        public void CopyColliderScaleToAreaGuidancePlane()
        {
            SpriteRenderer areaSpriteRenderer = areaGuidancePlane as SpriteRenderer;
            if (areaSpriteRenderer)
            {
                areaSpriteRenderer.size = new Vector2(volumeCollider.transform.localScale.x, volumeCollider.transform.localScale.z);
            }
        }

    }
}

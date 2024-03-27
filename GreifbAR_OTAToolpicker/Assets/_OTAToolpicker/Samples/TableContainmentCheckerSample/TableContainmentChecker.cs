using System.Collections.Generic;
using UnityEngine;

namespace NMY.OTAToolpicker
{
    public class TableContainmentChecker : MonoBehaviour
    {
        [SerializeField] private InstrumentTable instrumentTable;

        [SerializeField] private LayerMask layersToCheck = 1 << 0;

        private Bounds initialTableBoxBounds;

        // cached extents of the table's box colliders
        private Vector3 zMinusBoxExtents;
        private Vector3 zPlusBoxExtents;
        private Vector3 xMinusBoxExtents;
        private Vector3 xPlusBoxExtents;

        private Transform tableMeshTransform;

        // cached transforms of the table's box colliders
        private Transform zMinusBoxTrans;
        private Transform zPlusBoxTrans;
        private Transform xMinusBoxTrans;
        private Transform xPlusBoxTrans;

        public List<PlaceableInstrument> instrumentsHittingTable = new List<PlaceableInstrument>();
        public List<PlaceableInstrument> instrumentsOverlappingTable = new List<PlaceableInstrument>();

        public bool IsOutsideTable(PlaceableInstrument pi) => !instrumentsHittingTable.Contains(pi) && !instrumentsOverlappingTable.Contains(pi);
        public bool IsInsideTable(PlaceableInstrument pi) => instrumentsHittingTable.Contains(pi) && !instrumentsOverlappingTable.Contains(pi);
        public bool IsOverlappingTable(PlaceableInstrument pi) => instrumentsOverlappingTable.Contains(pi);

        void Start()
        {
            initialTableBoxBounds = instrumentTable.TablePlaneBoxCollider.bounds;

            // cache the transforms from the InstrumentTable
            zMinusBoxTrans = instrumentTable.ZMinusBoxTrans;
            zPlusBoxTrans = instrumentTable.ZPlusBoxTrans;
            xMinusBoxTrans = instrumentTable.XMinusBoxTrans;
            xPlusBoxTrans = instrumentTable.XPlusBoxTrans;

            tableMeshTransform = instrumentTable.TablePlaneTransform;

            // the table uses a Plane mesh which has a size of 10x10 (x,z)
            // we only want the "extent" which is half the size, therefore we multiply by 0.5
            // last we scale the extents by the table's scale
            zMinusBoxExtents = zMinusBoxTrans.localScale*0.5f;
            zMinusBoxExtents.Scale(tableMeshTransform.localScale);

            zPlusBoxExtents = zPlusBoxTrans.localScale*0.5f;
            zPlusBoxExtents.Scale(tableMeshTransform.localScale);

            xMinusBoxExtents = xMinusBoxTrans.localScale*0.5f;
            xMinusBoxExtents.Scale(tableMeshTransform.localScale);

            xPlusBoxExtents = xPlusBoxTrans.localScale*0.5f;
            xPlusBoxExtents.Scale(tableMeshTransform.localScale);
        }

        void FixedUpdate()
        {
            // DoPhysicsOverlapCheck();
        }

        public void DoPhysicsOverlapCheck()
        {
            Collider[] collidersHittingTable = Physics.OverlapBox(tableMeshTransform.position, initialTableBoxBounds.extents, tableMeshTransform.rotation, layersToCheck);

            Collider[] collidersHittingZMinus = Physics.OverlapBox(zMinusBoxTrans.position, zMinusBoxExtents, zMinusBoxTrans.rotation, layersToCheck);
            Collider[] collidersHittingZPlus = Physics.OverlapBox(zPlusBoxTrans.position, zPlusBoxExtents, zPlusBoxTrans.rotation, layersToCheck);
            Collider[] collidersHittingXMinus = Physics.OverlapBox(xMinusBoxTrans.position, xMinusBoxExtents, xMinusBoxTrans.rotation, layersToCheck);
            Collider[] collidersHittingXPlus = Physics.OverlapBox(xPlusBoxTrans.position, xPlusBoxExtents, xPlusBoxTrans.rotation, layersToCheck);

            instrumentsHittingTable.Clear();
            foreach(Collider c in collidersHittingTable) {
                PlaceableInstrument pi = c.GetComponentInParent<PlaceableInstrument>();
                if (pi) instrumentsHittingTable.Add(pi);
            }

            instrumentsOverlappingTable.Clear();
            foreach(Collider c in collidersHittingZMinus) {
                PlaceableInstrument pi = c.GetComponentInParent<PlaceableInstrument>();
                if (pi) instrumentsOverlappingTable.Add(pi);
            }
            foreach(Collider c in collidersHittingZPlus) {
                PlaceableInstrument pi = c.GetComponentInParent<PlaceableInstrument>();
                if (pi && !instrumentsOverlappingTable.Contains(pi)) instrumentsOverlappingTable.Add(pi);
            }
            foreach(Collider c in collidersHittingXMinus) {
                PlaceableInstrument pi = c.GetComponentInParent<PlaceableInstrument>();
                if (pi && !instrumentsOverlappingTable.Contains(pi)) instrumentsOverlappingTable.Add(pi);
            }
            foreach(Collider c in collidersHittingXPlus) {
                PlaceableInstrument pi = c.GetComponentInParent<PlaceableInstrument>();
                if (pi && !instrumentsOverlappingTable.Contains(pi)) instrumentsOverlappingTable.Add(pi);
            }

            // Debug.Log("Got " + instrumentsHittingTable.Count + " instruments hitting the table, " + instrumentsOverlappingTable.Count + " instruments overlapping.");
        }

        // public void OnDrawGizmos()
        // {
        //     // var collider = GetComponent<Collider>();
        //     // if (!tableBoxCollider)
        //     // {
        //     //     return; // nothing to do without a collider
        //     // }
        //     // Vector3 closestPoint = tableBoxCollider.ClosestPoint(convexHullMeshCollider.transform.position);
        //     // Gizmos.DrawSphere(convexHullMeshCollider.transform.position, 0.01f);
        //     // Gizmos.DrawWireSphere(closestPoint, 0.01f);
        //     // Vector3 closestPointInstrument = convexHullMeshCollider.ClosestPoint(closestPoint);
        //     // Gizmos.DrawWireSphere(closestPointInstrument, 0.01f);
        //     // Gizmos.DrawWireCube(initialTableBoxBounds.center, initialTableBoxBounds.size);
        // }

    }
}

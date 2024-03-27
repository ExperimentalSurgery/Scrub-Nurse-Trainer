using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace NMY.OTAToolpicker
{
    /// <summary>
    /// Specialized Vufira observer for the table marker.
    /// It does not activate any child Renderer or Collider, like the default observers does,
    /// instead it only activates the table plane renderer.
    /// </summary>
    public class TableObserverEventHandler : DefaultObserverEventHandler
    {
        [SerializeField] private InstrumentTable instrumentTable;

        protected override void OnTrackingFound()
        {
            instrumentTable.TablePlaneRenderer.enabled = true;
            OnTargetFound?.Invoke();
        }

        protected override void OnTrackingLost()
        {
            instrumentTable.TablePlaneRenderer.enabled = false;
            OnTargetLost?.Invoke();
        }
    }

}

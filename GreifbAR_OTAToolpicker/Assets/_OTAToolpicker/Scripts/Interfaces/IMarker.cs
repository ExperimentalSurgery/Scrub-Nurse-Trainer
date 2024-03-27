using UnityEngine;
using UnityEngine.Events;

namespace NMY.OTAToolpicker
{
    public interface IMarkerBase
    {
        void EnableTracking();
        void DisableTracking();
        bool IsTrackingEnabled { get; }

        GameObject gameObject { get; }
    }

    public interface IMarker : IMarkerBase
    {
        public UnityEvent OnMarkerFound { get; }
        public UnityEvent OnMarkerLost { get; }
    }

    public interface IInstrumentMarker : IMarkerBase
    {
        public PlaceableInstrument PlaceableInstrument { get; }
        public PlaceableInstrumentElement ElementsDisplayed { get; set; }
        public UnityEvent<InstrumentMarker> OnInstrumentFound { get; }
        public UnityEvent<InstrumentMarker> OnInstrumentLost { get; }
        public UnityEvent<InstrumentMarker> OnInstrumentDropped { get; }
    }
}

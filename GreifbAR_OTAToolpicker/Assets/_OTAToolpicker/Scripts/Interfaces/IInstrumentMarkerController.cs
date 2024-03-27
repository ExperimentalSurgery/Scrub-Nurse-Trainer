using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace NMY.OTAToolpicker
{
    public interface IInstrumentMarkerController
    {
        UnityEvent<InstrumentMarker> InstrumentFound { get; }
        UnityEvent<InstrumentMarker> InstrumentLost { get; }
        UnityEvent<InstrumentMarker> InstrumentDropped { get; }
    }
}

using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using Cysharp.Threading.Tasks;

namespace NMY.OTAToolpicker
{
    public interface ITrackingHardwareController
    {
        public UniTask InitializeTrackingAsync();
        public UniTask StartTrackingAsync();
        public UniTask StopTrackingAsync();
    }
}

using System.Collections.Generic;
using UnityEngine;
using Cysharp.Threading.Tasks;

namespace NMY.OTAToolpicker
{
    public interface ILevelController
    {
        public UniTask StartLevelAsync();
        public void StopLevel();
        public void ResetLevel();

        // public bool IsLevelStopRequested { get;  set; }
    }
}

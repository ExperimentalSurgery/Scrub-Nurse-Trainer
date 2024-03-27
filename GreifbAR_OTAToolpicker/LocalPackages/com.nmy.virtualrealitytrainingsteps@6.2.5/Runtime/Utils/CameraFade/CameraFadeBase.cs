using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace NMY.VirtualRealityTraining
{
    public abstract class CameraFadeBase : MonoBehaviour
    {
        public abstract void FadeIn(float duration);
        public abstract void FadeOut(float duration);
        public abstract UniTask FadeInAsync(float duration);
        public abstract UniTask FadeOutAsync(float duration);
    }
}
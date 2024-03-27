using System;
using UnityEngine;

namespace NMY.VirtualRealityTraining
{
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
    public class AnimatorClipAttribute : PropertyAttribute
    {
        public string animatorName { get; }

        public AnimatorClipAttribute(string animatorName)
        {
            this.animatorName = animatorName;
        }
    }
}
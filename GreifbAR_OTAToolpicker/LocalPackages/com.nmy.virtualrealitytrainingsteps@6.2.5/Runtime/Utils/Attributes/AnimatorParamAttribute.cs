using System;
using UnityEngine;

namespace NMY.VirtualRealityTraining
{
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
    public class AnimatorParamAttribute : PropertyAttribute
    {
        public string                           animatorName      { get; }
        public AnimatorControllerParameterType? animatorParamType { get; }


        public AnimatorParamAttribute(string animatorName)
        {
            this.animatorName      = animatorName;
            this.animatorParamType = null;
        }

        public AnimatorParamAttribute(string animatorName, AnimatorControllerParameterType animatorParamType)
        {
            this.animatorName      = animatorName;
            this.animatorParamType = animatorParamType;
        }
    }
}
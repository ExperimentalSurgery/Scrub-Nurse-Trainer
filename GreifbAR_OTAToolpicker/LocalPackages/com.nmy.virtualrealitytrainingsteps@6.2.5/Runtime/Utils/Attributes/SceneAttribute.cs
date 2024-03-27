using System;
using UnityEngine;

namespace NMY.VirtualRealityTraining
{
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
    public class SceneAttribute : PropertyAttribute
    {
    }
}
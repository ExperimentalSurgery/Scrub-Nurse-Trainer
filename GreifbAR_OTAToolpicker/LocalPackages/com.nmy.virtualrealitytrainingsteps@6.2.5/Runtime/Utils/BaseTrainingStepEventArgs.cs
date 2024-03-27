using System;
using NMY.VirtualRealityTraining.Steps;

namespace NMY.VirtualRealityTraining
{
    /// <summary>
    /// Event arguments for the <see cref="BaseTrainingStep"/> class.
    /// </summary>
    public class BaseTrainingStepEventArgs : EventArgs
    {
        /// <summary>
        /// A reference to the step from which the event was called
        /// </summary>
        public BaseTrainingStep step { get; }

        /// <summary>
        /// The constructor for this event args.
        /// </summary>
        /// <param name="step">The step that raised the event with this event args.</param>
        public BaseTrainingStepEventArgs(BaseTrainingStep step)
        {
            this.step = step;
        }
    }
}

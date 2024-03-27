using System.Threading;
using Cysharp.Threading.Tasks;

namespace NMY.VirtualRealityTraining.Steps
{
    /// <summary>
    /// A training step representing a chapter in a virtual reality training sequence.
    /// </summary>
    /// <remarks>
    /// This class is only used for structuring the step sequence in logical parts.
    /// </remarks>
    public class ChapterTrainingStep : BaseTrainingStep
    {
        protected override async UniTask ClientStepActionAsync(CancellationToken cancellationToken)
        {
            await UniTask.CompletedTask;
            RaiseClientStepFinished();
        }

        protected override string GameObjectPrefixName() => "[Chapter]";
    }
}

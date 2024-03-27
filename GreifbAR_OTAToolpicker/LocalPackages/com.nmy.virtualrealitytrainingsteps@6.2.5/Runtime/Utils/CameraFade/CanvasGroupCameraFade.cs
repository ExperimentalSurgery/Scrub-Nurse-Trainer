using Cysharp.Threading.Tasks;
using UnityEngine;

namespace NMY.VirtualRealityTraining
{
    /// <summary>
    /// A camera tool with a fade effect.
    /// </summary>
    public class CanvasGroupCameraFade : CameraFadeBase
    {
        /// <summary>
        /// A flag that indicates whether the camera should be faded in (from black to transparent) at Start.
        /// </summary>
        [SerializeField] private bool _fadeAtStart;

        /// <summary>
        /// A reference to the <see cref="_canvasGroup"/> component that contains an image for fading. 
        /// </summary>
        [SerializeField] private CanvasGroup _canvasGroup;

        /// <summary>
        /// An <see cref="AnimationCurve"/> that represents how the fade effect is animated.
        /// </summary>
        [SerializeField] private AnimationCurve _faceCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

        /// <summary>
        /// The current alpha value of the <see cref="_canvasGroup"/>. 
        /// </summary>
        private float _currentFadeValue;

        /// <summary>
        /// Starts a Fade-in animation if <see cref="_fadeAtStart"/> is true.
        /// </summary>
        private async void Start()
        {
            if (_fadeAtStart)
            {
                await Fade(0, 1);
                await Fade(2, 0);
            }
        }

        /// <summary>
        /// Asynchronously lerps the <see cref="CanvasGroup.alpha"/> value of <see cref="CanvasGroup"/>
        /// from its current value to <see cref="alpha"/> within <see cref="duration"/> seconds.
        /// </summary>
        /// <param name="duration">The duration in seconds the animation should take.</param>
        /// <param name="alpha">The alpha value the fade should have at the end.</param>
        public async UniTask Fade(float duration, float alpha)
        {
            var elapsedTime      = 0.0f;
            var currentFadeValue = _currentFadeValue;

            if (duration == 0)
            {
                _currentFadeValue  = alpha;
                _canvasGroup.alpha = _currentFadeValue;
                return;
            }

            while (elapsedTime < duration)
            {
                _currentFadeValue = Mathf.Lerp(currentFadeValue, alpha,
                                               _faceCurve.Evaluate(elapsedTime / duration));

                _canvasGroup.alpha =  _currentFadeValue;
                elapsedTime        += Time.deltaTime;
                await UniTask.WaitForFixedUpdate();
            }

            _canvasGroup.alpha = _currentFadeValue;
        }

        public override void FadeIn(float duration)
        {
            Fade(duration, 1).Forget();
        }

        public override void FadeOut(float duration)
        {
            Fade(duration, 0).Forget();
        }

        public override async UniTask FadeInAsync(float duration)
        {
            await Fade(duration, 1);
        }

        public override async UniTask FadeOutAsync(float duration)
        {
            await Fade(duration, 0);
        }
    }
}
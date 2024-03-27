using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Serialization;

namespace NMY.VirtualRealityTraining
{
    public class XrFade : CameraFadeBase
    {
        private const string DEFAULT_SHADER = "VR/XrFade";

        private static class ShaderPropertyLookup
        {
            public static readonly int FadeColor = Shader.PropertyToID("_Color");
            public static readonly int Alpha     = Shader.PropertyToID("_Alpha");
        }

        [Header("Alpha Settings")]
        [SerializeField] [Range(0f, 1f)] private float _alpha;
        [SerializeField]                 private Color _fadeColor = Color.black;

        [SerializeField] private AnimationCurve _alphaAnimationCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

        [Header("Start Behaviour")]
        [SerializeField] private bool _fadeOutAtStart = true;
        [SerializeField] private float _fadeOutAtStartDuration = 2f;

        private MeshRenderer          _meshRender;
        private MeshFilter            _meshFilter;
        private Material              _sharedMaterial;
        private MaterialPropertyBlock _fadePropertyBlock;

        private CancellationTokenSource _fadeCancellationTokenSource = new();

        private async void Start()
        {
            UpdateShader(_alpha);

            if (_fadeOutAtStart)
            {
                await FadeInAsync(0);
                await FadeOutAsync(_fadeOutAtStartDuration);
            }
        }

        public override void FadeIn(float duration)
        {
            Fade(1, duration);
        }

        public override void FadeOut(float duration)
        {
            Fade(0, duration);
        }

        public void Fade(float toAlpha, float duration)
        {
            Fade(_alpha, toAlpha, duration);
        }

        public void Fade(float fromAlpha, float toAlpha, float duration)
        {
            _fadeCancellationTokenSource.Cancel();
            _fadeCancellationTokenSource.Dispose();
            _fadeCancellationTokenSource = new CancellationTokenSource();

            FadeAsync(fromAlpha, toAlpha, duration, _fadeCancellationTokenSource.Token).Forget();
        }

        public override async UniTask FadeInAsync(float duration)
        {
            await FadeAsync(1, duration);
        }

        public override async UniTask FadeOutAsync(float duration)
        {
            await FadeAsync(0, duration);
        }

        public async UniTask FadeAsync(float toAlpha, float duration)
        {
            await FadeAsync(_alpha, toAlpha, duration);
        }

        public async UniTask FadeAsync(float fromAlpha, float toAlpha, float duration)
        {
            _fadeCancellationTokenSource.Cancel();
            _fadeCancellationTokenSource.Dispose();
            _fadeCancellationTokenSource = new CancellationTokenSource();

            await FadeAsync(fromAlpha, toAlpha, duration, _fadeCancellationTokenSource.Token);
        }

        private async UniTask FadeAsync(float fromAlpha, float toAlpha, float duration, CancellationToken ct)
        {
            var elapsedTime = 0f;

            if (duration == 0)
            {
                UpdateShader(toAlpha);
                return;
            }

            try
            {
                while (elapsedTime <= duration)
                {
                    var alpha = Mathf.Lerp(fromAlpha, toAlpha, _alphaAnimationCurve.Evaluate(elapsedTime / duration));

                    UpdateShader(Mathf.Clamp01(alpha));
                    elapsedTime += Time.deltaTime;

                    await UniTask.NextFrame(PlayerLoopTiming.Update, ct);
                }

                UpdateShader(toAlpha);
            }
            catch (OperationCanceledException)
            {
            }
        }

        private void UpdateShader(float alpha)
        {
            if (!TrySetUpMaterial()) return;

            _meshRender.GetPropertyBlock(_fadePropertyBlock);
            _fadePropertyBlock.SetFloat(ShaderPropertyLookup.Alpha, alpha);
            _fadePropertyBlock.SetColor(ShaderPropertyLookup.FadeColor, _fadeColor);
            _meshRender.SetPropertyBlock(_fadePropertyBlock);
            
            _meshRender.enabled = alpha switch
            {
                > 0 => true,
                0   => false,
                _   => _meshRender.enabled
            };

            _alpha = alpha;
        }

        private bool TrySetUpMaterial()
        {
            if (gameObject == null) return false;
            
            if (_meshRender == null)
                _meshRender = GetComponent<MeshRenderer>();
            if (_meshRender == null)
                _meshRender = gameObject.AddComponent<MeshRenderer>();

            if (_fadePropertyBlock == null)
                _fadePropertyBlock = new MaterialPropertyBlock();

            if (_meshFilter == null)
                _meshFilter = GetComponent<MeshFilter>();
            if (_meshFilter == null)
                _meshFilter = gameObject.AddComponent<MeshFilter>();

            if (_meshFilter.sharedMesh == null)
            {
                Debug.LogWarning($"{GetType()}: The default mesh for the XrFade is not set.", this);
                return false;
            }

            if (_meshRender.sharedMaterial == null)
            {
                var defaultShader = Shader.Find(DEFAULT_SHADER);
                if (defaultShader == null)
                {
                    Debug.LogWarning($"{GetType()}: The default material for the XrFade is not set, and the default Shader: {DEFAULT_SHADER} cannot be found.", this);
                    return false;
                }

                Debug.LogWarning($"{GetType()}: The default material for the XrFade is not set. " +
                                 $"Try creating a material using the default Shader: {DEFAULT_SHADER}", this);

                _sharedMaterial = new Material(defaultShader)
                {
                    name = "XrFade",
                };
                _meshRender.sharedMaterial = _sharedMaterial;
            }
            else
            {
                _sharedMaterial = _meshRender.sharedMaterial;
            }

            return true;
        }

        private void OnValidate()
        {
            if (Application.isEditor || Application.isPlaying)
            {
                UpdateShader(_alpha);
            }
        }

        [ContextMenu("Fade in 2 seconds")]
        public void FADEIN_CONTEXTMENU() => FadeIn(2);

        [ContextMenu("Fade out 2 seconds")]
        public void FADEOUT_CONTEXTMENU() => FadeOut(2);
    }
}
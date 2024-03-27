using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace NMY.VirtualRealityTraining
{
    public class XrVignette : CameraFadeBase
    {
        private const string DEFAULT_SHADER = "VR/XrVignette";

        private static class ShaderPropertyLookup
        {
            public static readonly int ApertureSize       = Shader.PropertyToID("_ApertureSize");
            public static readonly int FeatheringEffect   = Shader.PropertyToID("_FeatheringEffect");
            public static readonly int VignetteColor      = Shader.PropertyToID("_VignetteColor");
            public static readonly int VignetteColorBlend = Shader.PropertyToID("_VignetteColorBlend");
        }

        public enum ApertureSizeEndState
        {
            Default,
            Open,
            Close
        }
        
        [Header("Vignette Settings")]
        [SerializeField] [Range(0.0f, 1.0f)] private float _apertureSize       = 1f;
        [SerializeField] [Range(0.0f, 1.0f)] private float _featheringEffect   = 1f;
        [SerializeField]                     private Color _vignetteColor      = Color.black;
        [SerializeField]                     private Color _vignetteColorBlend = Color.black;

        [SerializeField] private AnimationCurve _apertureSizeAnimationCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
        [SerializeField] private AnimationCurve _featheringEffectAnimationCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

        [Header("Start Behaviour")]
        [SerializeField] private bool  _fadeOutAtStart         = true;
        [SerializeField] private float _fadeOutAtStartDuration = 2f;


        private MeshRenderer          _meshRender;
        private MeshFilter            _meshFilter;
        private Material              _sharedMaterial;
        private MaterialPropertyBlock _vignettePropertyBlock;

        private CancellationTokenSource _easingCancellationTokenSource = new();


        private async void Start()
        {
            UpdateVignette(_apertureSize, _featheringEffect);
            
            if (_fadeOutAtStart)
            {
                await FadeInAsync(0);
                await FadeOutAsync(_fadeOutAtStartDuration);
            }
        }

        public override void FadeIn(float duration)
        {
            Fade(0, duration, 0, duration);
        }

        public override void FadeOut(float duration)
        {
            Fade(1, duration, 1, duration);
        }

        public void Fade(float toApertureSize, float apertureTime, float toFeatheringEffect,
                         float featheringEffectTime,
                         ApertureSizeEndState apertureSizeEndState = ApertureSizeEndState.Default)
        {
            Fade(_apertureSize, toApertureSize, apertureTime,
                 _featheringEffect, toFeatheringEffect,
                 featheringEffectTime, apertureSizeEndState);
        }

        public void Fade(float fromApertureSize, float toApertureSize, float apertureTime, float fromFeatheringEffect,
                         float toFeatheringEffect, float featheringEffectTime,
                         ApertureSizeEndState apertureSizeEndState = ApertureSizeEndState.Default)
        {
            _easingCancellationTokenSource.Cancel();
            _easingCancellationTokenSource.Dispose();
            _easingCancellationTokenSource = new CancellationTokenSource();

            FadeAsync(fromApertureSize, toApertureSize, apertureTime, fromFeatheringEffect, toFeatheringEffect,
                      featheringEffectTime, apertureSizeEndState, _easingCancellationTokenSource.Token)
               .Forget();
        }

        public override async UniTask FadeInAsync(float duration)
        {
            await FadeAsync(0, duration, 0, duration);
        }

        public override async UniTask FadeOutAsync(float duration)
        {
            await FadeAsync(1, duration, 1, duration);
        }

        public async UniTask FadeAsync(float toApertureSize, float apertureTime, float toFeatheringEffect,
                                       float featheringEffectTime,
                                       ApertureSizeEndState apertureSizeEndState = ApertureSizeEndState.Default)
        {
            await FadeAsync(_apertureSize, toApertureSize, apertureTime,
                            _featheringEffect, toFeatheringEffect,
                            featheringEffectTime, apertureSizeEndState);
        }

        public async UniTask FadeAsync(float fromApertureSize, float toApertureSize, float apertureTime,
                                       float fromFeatheringEffect, float toFeatheringEffect, float featheringEffectTime,
                                       ApertureSizeEndState apertureSizeEndState = ApertureSizeEndState.Default)
        {
            _easingCancellationTokenSource.Cancel();
            _easingCancellationTokenSource.Dispose();
            _easingCancellationTokenSource = new CancellationTokenSource();

            await FadeAsync(fromApertureSize, toApertureSize, apertureTime, fromFeatheringEffect,
                            toFeatheringEffect,
                            featheringEffectTime, apertureSizeEndState, _easingCancellationTokenSource.Token);
        }

        private async UniTask FadeAsync(float fromApertureSize, float toApertureSize, float apertureTime,
                                        float fromFeatheringEffect, float toFeatheringEffect,
                                        float featheringEffectTime,
                                        ApertureSizeEndState apertureSizeEndState, CancellationToken ct)
        {
            var duration    = Mathf.Max(apertureTime, featheringEffectTime);
            var elapsedTime = 0f;

            if (duration == 0)
            {
                switch (apertureSizeEndState)
                {
                    case ApertureSizeEndState.Default:
                        UpdateVignette(toApertureSize, toFeatheringEffect);
                        break;
                    case ApertureSizeEndState.Open:
                        UpdateVignette(1, toFeatheringEffect);
                        break;
                    case ApertureSizeEndState.Close:
                        UpdateVignette(0, toFeatheringEffect);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(apertureSizeEndState), apertureSizeEndState, null);
                }
                
                return;
            }

            try
            {
                while (elapsedTime <= duration)
                {
                    var apertureSize = Mathf.Lerp(fromApertureSize, toApertureSize,
                                                  _apertureSizeAnimationCurve.Evaluate(elapsedTime / apertureTime));
                    var featheringEffect = Mathf.Lerp(fromFeatheringEffect, toFeatheringEffect,
                                                      _featheringEffectAnimationCurve.Evaluate(elapsedTime / featheringEffectTime));

                    UpdateVignette(Mathf.Clamp01(apertureSize), Mathf.Clamp01(featheringEffect));
                    elapsedTime += Time.deltaTime;

                    await UniTask.NextFrame(PlayerLoopTiming.Update, ct);
                }

                switch (apertureSizeEndState)
                {
                    case ApertureSizeEndState.Default:
                        UpdateVignette(toApertureSize, toFeatheringEffect);
                        break;
                    case ApertureSizeEndState.Open:
                        UpdateVignette(1, toFeatheringEffect);
                        break;
                    case ApertureSizeEndState.Close:
                        UpdateVignette(0, toFeatheringEffect);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(apertureSizeEndState), apertureSizeEndState, null);
                }
            }
            catch (OperationCanceledException)
            {
            }
        }

        private void UpdateVignette(float apertureSize, float featheringEffect)
        {
            if (!TrySetUpMaterial()) return;

            _meshRender.GetPropertyBlock(_vignettePropertyBlock);
            _vignettePropertyBlock.SetFloat(ShaderPropertyLookup.ApertureSize, apertureSize);
            _vignettePropertyBlock.SetFloat(ShaderPropertyLookup.FeatheringEffect, featheringEffect);
            _vignettePropertyBlock.SetColor(ShaderPropertyLookup.VignetteColor, _vignetteColor);
            _vignettePropertyBlock.SetColor(ShaderPropertyLookup.VignetteColorBlend, _vignetteColorBlend);
            _meshRender.SetPropertyBlock(_vignettePropertyBlock);
            
            _meshRender.enabled = apertureSize switch
            {
                < 1 => true,
                1   => false,
                _   => _meshRender.enabled
            };

            _apertureSize     = apertureSize;
            _featheringEffect = featheringEffect;
        }

        private bool TrySetUpMaterial()
        {
            if (_meshRender == null)
                _meshRender = GetComponent<MeshRenderer>();
            if (_meshRender == null)
                _meshRender = gameObject.AddComponent<MeshRenderer>();

            if (_vignettePropertyBlock == null)
                _vignettePropertyBlock = new MaterialPropertyBlock();

            if (_meshFilter == null)
                _meshFilter = GetComponent<MeshFilter>();
            if (_meshFilter == null)
                _meshFilter = gameObject.AddComponent<MeshFilter>();

            if (_meshFilter.sharedMesh == null)
            {
                Debug.LogWarning($"{GetType()}: The default mesh for the XrVignette is not set.", this);
                return false;
            }

            if (_meshRender.sharedMaterial == null)
            {
                var defaultShader = Shader.Find(DEFAULT_SHADER);
                if (defaultShader == null)
                {
                    Debug.LogWarning(
                        $"{GetType()}: The default material for the XrVignette is not set, and the default Shader: {DEFAULT_SHADER} cannot be found.",
                        this);
                    return false;
                }

                Debug.LogWarning($"{GetType()}: The default material for the XrVignette is not set. " +
                                 $"Try creating a material using the default Shader: {DEFAULT_SHADER}", this);

                _sharedMaterial = new Material(defaultShader)
                {
                    name = "XrVignette",
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
                UpdateVignette(_apertureSize, _featheringEffect);
            }
        }
    }
}
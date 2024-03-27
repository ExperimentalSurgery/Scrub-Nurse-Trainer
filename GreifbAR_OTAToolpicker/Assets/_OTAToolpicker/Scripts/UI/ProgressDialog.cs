using Cysharp.Threading.Tasks;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Localization;
using Tweens;
using TMPro;
using UnityEngine.SocialPlatforms;

namespace NMY.OTAToolpicker
{
    public enum ProgressDialogStatus {
        HandNotVisible,
        HandVisible,
        PoseDetected
    }

    public class ProgressDialog : MonoBehaviour
    {
        [Tooltip("If <b>true</b>, the dialog will be shown when Start() is called.")]
        [SerializeField] private bool showInitially = false;

        [SerializeField] private float progressTimeS = 3f;

        [Header("Status")]

        [SerializeField] private ProgressDialogStatus status = ProgressDialogStatus.HandNotVisible;
        public ProgressDialogStatus Status {
            get => status;
            set {
                status = value;
                switch (status) {
                    case ProgressDialogStatus.HandNotVisible:
                        infoColorImage.color = handNotVisibleColor;
                        infoText.text = handNotVisibleInfo.GetLocalizedString();
                        titleText.text = waitingForHandPoseString.GetLocalizedString();
                        break;
                    case ProgressDialogStatus.HandVisible:
                        infoColorImage.color = handVisibleColor;
                        infoText.text = handVisibleInfo.GetLocalizedString();
                        titleText.text = waitingForHandPoseString.GetLocalizedString();
                        break;
                    case ProgressDialogStatus.PoseDetected:
                        infoColorImage.color = poseDetectedColor;
                        infoText.text = poseDetectedInfo.GetLocalizedString();
                        titleText.text = holdHandPoseString.GetLocalizedString();
                        break;
                }
            }
        }
        [SerializeField] private Color handNotVisibleColor = Color.red;
        [SerializeField] private Color handVisibleColor = Color.yellow;
        [SerializeField] private Color poseDetectedColor = Color.green;
        [SerializeField] private LocalizedString handNotVisibleInfo;
        [SerializeField] private LocalizedString handVisibleInfo;
        [SerializeField] private LocalizedString poseDetectedInfo;


        [Header("Texts")]
        [SerializeField] private LocalizedString title;
        public LocalizedString Title => title;

        [Tooltip("The localized string to show in the progress dialog when waiting for a hand pose.")]
        [SerializeField] private LocalizedString waitingForHandPoseString;
        public LocalizedString WaitingForHandPoseString => waitingForHandPoseString;
        [SerializeField] private LocalizedString holdHandPoseString;
        public LocalizedString HoldHandPoseString => holdHandPoseString;


        [Header("UI Components")]
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private Slider progressSlider;
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private Image infoColorImage;
        [SerializeField] private TMP_Text infoText;

        #region Tweening
        [Header("Tweening")]
        [SerializeField] private bool useTweening = true;
        [SerializeField] private float tweenDuration = 0.5f;
        [SerializeField] private EaseType showEaseType = EaseType.QuadIn;
        [SerializeField] private EaseType hideEaseType = EaseType.QuadOut;

        private FloatTween canvasShowTween;
        private FloatTween canvasHideTween;
        private TweenInstance showTweenInstance;
        private TweenInstance hideTweenInstance;
        #endregion

        private float currentTimeS = 0f;
        public float CurrentTimeS => currentTimeS;

        private bool isTimerRunning = false;

        void Awake()
        {
            if (!useTweening) return;

            canvasGroup = gameObject.AddComponent<CanvasGroup>();
            if (canvasGroup == null) {
                Debug.LogWarning("DialogUI: useTweening==true but no CanvasGroup was found! Tweening requires a CanvasGroup component. Disabling tweening.");
                useTweening = false;
                return;
            }

            canvasGroup.alpha = 0f;

            // prepare the show and hide tweens
            canvasShowTween = new FloatTween {
                from = 0f,
                to = 1f,
                fillMode = FillMode.Both,
                duration = tweenDuration,
                easeType = showEaseType,
                onStart = (_) => {
                    canvasGroup.interactable = true;
                    canvasGroup.blocksRaycasts = true;
                },
                onUpdate = (_, value) => { canvasGroup.alpha = value; }
            };

            canvasHideTween = new FloatTween {
                from = 1f,
                to = 0f,
                fillMode = FillMode.Both,
                duration = tweenDuration,
                easeType = hideEaseType,
                onUpdate = (_, value) => { canvasGroup.alpha = value; },
                onEnd = (_) => {
                    canvasGroup.interactable = false;
                    canvasGroup.blocksRaycasts = false;
                }
            };
        }

        async void Start()
        {
            titleText.text = title.GetLocalizedString();

            // Do NOT move the localization event subscription to OnEnable()!
            // Otherwise some of the Show() methods will not work properly.
            title.StringChanged += UpdateTitleText;
            // message.StringChanged += UpdateMessageText;

            if (showInitially) {
                await Show(progressTimeS);
            }
            else {
                gameObject.SetActive(false);
            }

            Status = status;
        }

        void OnDestroy()
        {
            title.StringChanged -= UpdateTitleText;
        }

        async public UniTask Show(string title, float progressTimeS, CancellationToken ct=default(CancellationToken))
        {
            titleText.text = title;
            await Show(progressTimeS, ct);
        }

        async public UniTask Show(float progressTimeS, CancellationToken ct=default(CancellationToken))
        {
            ResetTimer();
            StopTimer();
            gameObject.SetActive(true);
            if (useTweening && canvasGroup) ShowWithTween();

            try {
                // await UniTask.WaitForSeconds(progressTimeS, cancellationToken: ct);
                await UniTask.WaitUntil(() => currentTimeS >= progressTimeS, cancellationToken: ct);
            }
            catch (System.OperationCanceledException) {}

            Hide();
        }

        public void Hide()
        {
            StopTimer();

            if (useTweening && canvasGroup)
                HideWithTween();
            else
                gameObject.SetActive(false);

        }

        public void StartTimer()
        {
            currentTimeS = 0f;
            progressSlider.maxValue = progressTimeS;
            isTimerRunning = true;
        }

        public void StopTimer()
        {
            isTimerRunning = false;
        }

        public void ResetTimer()
        {
            currentTimeS = 0f;
            progressSlider.value = 0f;
        }

        public void SetTitle(LocalizedString title)
        {
            titleText.text = title.GetLocalizedString();
        }

        void Update()
        {
            if (isTimerRunning) {
                currentTimeS += Time.deltaTime;
                progressSlider.value = currentTimeS;
            }
        }

        private void ShowWithTween()
        {
            hideTweenInstance?.Cancel();
            // additionally use CancelTweens() to cancel any previous tweens
            // because cancelling the hideTweenInstance did not work as expected :( (why?)
            gameObject.CancelTweens();
            canvasShowTween.from = canvasGroup.alpha;
            canvasShowTween.to = 1f;
            showTweenInstance = gameObject.AddTween(canvasShowTween);
        }

        private void HideWithTween()
        {
            showTweenInstance?.Cancel();
            // additionally use CancelTweens() to cancel any previous tweens
            // because cancelling the showTweenInstance did not work as expected :( (why?)
            gameObject.CancelTweens();
            canvasHideTween.from = canvasGroup.alpha;
            canvasHideTween.to = 0f;
            hideTweenInstance = gameObject.AddTween(canvasHideTween);
        }

        private void UpdateTitleText(string text) => titleText.text = text;

    }
}

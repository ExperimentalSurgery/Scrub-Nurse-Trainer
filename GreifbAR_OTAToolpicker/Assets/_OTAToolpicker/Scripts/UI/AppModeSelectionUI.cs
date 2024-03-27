using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using Tweens;

namespace NMY.OTAToolpicker.UI
{
    /// <summary>
    /// UI for selecting the app mode.
    /// </summary>
    /// <remarks>
    /// Use it by calling <see cref="SelectAppMode"/> like this:
    /// <code>
    /// AppMode appMode = await appModeSelectionUI.SelectAppMode(cancellationToken);
    /// </code>
    /// </remarks>
    public class AppModeSelectionUI : MonoBehaviour
    {
        [Tooltip("If true, the UI will be hidden when the user has selected an app mode.")]
        [SerializeField] private bool hideOnModeSelected = true;

        [Header("UI Elements")]
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private Button demoModeButton;
        [SerializeField] private Button teachModeButton;

        [Header("Audio")]
        [Tooltip("The audio source that will play the audio clips.")]
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip welcomeAudioClip;
        [SerializeField] private AudioClip welcomeReminderAudioClip;
        [Tooltip("The interval in seconds between the reminder audio being played when the user has not press any button yet.")]
        [SerializeField] private int welcomeReminderIntervalS = 10;
        [Tooltip("If true, the welcome audio will only be played once. If false, the welcome audio will be played every time the app mode selection UI is shown.")]
        [SerializeField] private bool onlyPlayWelcomeAudioOnce = true;

        [Header("Events")]
        public UnityEvent<AppMode> AppModeSelected = new UnityEvent<AppMode>();
        public UnityEvent DemoModeSelected = new UnityEvent();
        public UnityEvent TeachModeSelected = new UnityEvent();

        private AppMode selectedAppMode = AppMode.Initial;
        private bool hasPlayedWelcomeAudio = false;

        private FloatTween canvasShowTween;
        private FloatTween canvasHideTween;

        void Awake()
        {
            // prepare the show and hide tweens
            canvasShowTween = new FloatTween {
                from = 0f,
                to = 1f,
                duration = 0.5f,
                easeType = EaseType.QuadIn,
                onUpdate = (_, value) => { canvasGroup.alpha = value; },
            };

            canvasHideTween = new FloatTween {
                from = 1f,
                to = 0f,
                duration = 0.5f,
                easeType = EaseType.QuadOut,
                onUpdate = (_, value) => { canvasGroup.alpha = value; },
                onEnd = (_) => gameObject.SetActive(false)
            };
        }

        public async UniTask<AppMode> SelectAppMode(CancellationToken ct = default)
        {
            selectedAppMode = AppMode.Initial;

            PlayWelcomeAudio();
            Show();

            CancellationTokenSource reminderCts = new CancellationTokenSource();
            RemindUserToSelectAppMode(welcomeAudioClip.length, reminderCts.Token).Forget();

            await UniTask.WaitUntil(() => selectedAppMode != AppMode.Initial, cancellationToken: ct);
            if (hideOnModeSelected)
                Hide();

            reminderCts.Cancel();
            audioSource.Stop();

            return selectedAppMode;
        }
        private void Show()
        {
            gameObject.SetActive(true);
            if (canvasGroup)
                gameObject.AddTween(canvasShowTween);
        }

        private void Hide()
        {
            if (canvasGroup!=null)
                gameObject.AddTween(canvasHideTween);
            else
                gameObject.SetActive(false);
        }

        private void OnEnable()
        {
            demoModeButton.onClick.AddListener(OnDemoModeSelected);
            teachModeButton.onClick.AddListener(OnTeachModeSelected);
        }

        private void OnDisable()
        {
            demoModeButton.onClick.RemoveListener(OnDemoModeSelected);
            teachModeButton.onClick.RemoveListener(OnTeachModeSelected);
        }

        private void OnDemoModeSelected()
        {
            AppModeSelected.Invoke(AppMode.DemoMode);
            DemoModeSelected.Invoke();
            selectedAppMode = AppMode.DemoMode;
        }

        private void OnTeachModeSelected()
        {
            AppModeSelected.Invoke(AppMode.TeachMode);
            TeachModeSelected.Invoke();
            selectedAppMode = AppMode.TeachMode;
        }

        private void PlayWelcomeAudio()
        {
            if (onlyPlayWelcomeAudioOnce && hasPlayedWelcomeAudio) return;

            audioSource.PlayOneShot(welcomeAudioClip);
            hasPlayedWelcomeAudio = true;
        }

        private async UniTask RemindUserToSelectAppMode(float delayS, CancellationToken ct)
        {
            await UniTask.Delay((int)(delayS*1000f), cancellationToken: ct);
            while (!ct.IsCancellationRequested)
            {
                await UniTask.Delay(welcomeReminderIntervalS*1000 + (int)(welcomeReminderAudioClip.length*1000f), cancellationToken: ct);
                audioSource.PlayOneShot(welcomeReminderAudioClip);
            }
        }
    }
}

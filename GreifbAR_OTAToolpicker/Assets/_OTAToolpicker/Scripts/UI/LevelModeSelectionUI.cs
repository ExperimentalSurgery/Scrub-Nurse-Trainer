using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using Tweens;
using UnityEngine.Serialization;

namespace NMY.OTAToolpicker.UI
{
    public class LevelModeSelectionUI : MonoBehaviour
    {
        [SerializeField] private bool hideOnModeSelected = true;

        [Header("UI Elements")]
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private Button level1LearnModeButton;
        [SerializeField] private Button level1QuizModeButton;
        [SerializeField] private Button level2LearnModeButton;
        [SerializeField] private Button level2QuizModeButton;
        [SerializeField] private Button level3LearnModeButton;
        [SerializeField] private Button level3QuizModeButton;

        [Header("Audio")]
        [SerializeField] private AudioSource audioSource;
        [FormerlySerializedAs("levelSelectionNewAudioClip")]
        [SerializeField] private AudioClip firstTimeAudioClip;
        [FormerlySerializedAs("levelSelectionContinueAudioClip")]
        [SerializeField] private AudioClip welcomeBackAudioClip;
        [FormerlySerializedAs("levelSelectionReminderAudioClip")]
        [SerializeField] private AudioClip pressButtonReminderAudioClip;
        [FormerlySerializedAs("levelSelectionUnlockedLevelAudioClip")]
        [SerializeField] private AudioClip levelUnlockedAudioClip;
        [SerializeField] private int reminderIntervalS = 10;

        [Header("Events")]
        public UnityEvent<LevelMode> LevelModeSelected = new UnityEvent<LevelMode>();

        private LevelMode levelMode = LevelMode.Initial;
        private FloatTween canvasShowTween;
        private FloatTween canvasHideTween;

        private List<LevelMode> unlockedLevels = new();

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

        async public UniTask<LevelMode> SelectLevelMode(Session session, CancellationToken cancellationToken = default)
        {
            ConfigureUI(session);
            levelMode = LevelMode.Initial;
            if (session.General.NrOfTimesMainMenuShown == 0) {
                audioSource.PlayOneShot(firstTimeAudioClip);
            }
            else {
                if (HasUnlockedNewLevel(session))
                    audioSource.PlayOneShot(levelUnlockedAudioClip);
                else
                    audioSource.PlayOneShot(welcomeBackAudioClip);
            }

            CancellationTokenSource reminderCts = new CancellationTokenSource();
            RemindUserToSelectLevel(firstTimeAudioClip.length, reminderCts.Token).Forget();

            Show();
            await UniTask.WaitUntil(() => levelMode != LevelMode.Initial, cancellationToken: cancellationToken);
            if (hideOnModeSelected)
                Hide();

            session.General.IncrementNrOfTimesMainMenuShown();
            reminderCts.Cancel();
            audioSource.Stop();
            return levelMode;
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
            level1LearnModeButton.onClick.AddListener(OnLevel1LearnModeSelected);
            level1QuizModeButton.onClick.AddListener(OnLevel1QuizModeSelected);
            level2LearnModeButton.onClick.AddListener(OnLevel2LearnModeSelected);
            level2QuizModeButton.onClick.AddListener(OnLevel2QuizModeSelected);
            level3LearnModeButton.onClick.AddListener(OnLevel3LearnModeSelected);
            level3QuizModeButton.onClick.AddListener(OnLevel3QuizModeSelected);
        }

        private void OnDisable()
        {
            level1LearnModeButton.onClick.RemoveListener(OnLevel1LearnModeSelected);
            level1QuizModeButton.onClick.RemoveListener(OnLevel1QuizModeSelected);
            level2LearnModeButton.onClick.RemoveListener(OnLevel2LearnModeSelected);
            level2QuizModeButton.onClick.RemoveListener(OnLevel2QuizModeSelected);
            level3LearnModeButton.onClick.RemoveListener(OnLevel3LearnModeSelected);
            level3QuizModeButton.onClick.RemoveListener(OnLevel3QuizModeSelected);
        }

        private void ConfigureUI(Session session)
        {
            switch (session.AppMode)
            {
                case AppMode.DemoMode:
                    level1LearnModeButton.interactable = true;
                    level1QuizModeButton.interactable = true;
                    level2LearnModeButton.interactable = true;
                    level2QuizModeButton.interactable = true;
                    level3LearnModeButton.interactable = true;
                    level3QuizModeButton.interactable = true;
                    break;
                case AppMode.TeachMode:
                    level1LearnModeButton.interactable = true;
                    level1QuizModeButton.interactable = session.HasCompletedLevel(LevelMode.Level1LearnMode);
                    level2LearnModeButton.interactable = session.HasCompletedLevel(LevelMode.Level1QuizMode);
                    level2QuizModeButton.interactable = session.HasCompletedLevel(LevelMode.Level2LearnMode);
                    level3LearnModeButton.interactable = session.HasCompletedLevel(LevelMode.Level2QuizMode);
                    level3QuizModeButton.interactable = session.HasCompletedLevel(LevelMode.Level3LearnMode);
                    break;
                default:
                    break;
            }
        }

        private void OnLevel1LearnModeSelected()
        {
            levelMode = LevelMode.Level1LearnMode;
            LevelModeSelected.Invoke(LevelMode.Level1LearnMode);
        }

        private void OnLevel1QuizModeSelected()
        {
            levelMode = LevelMode.Level1QuizMode;
            LevelModeSelected.Invoke(LevelMode.Level1QuizMode);
        }

        private void OnLevel2LearnModeSelected()
        {
            levelMode = LevelMode.Level2LearnMode;
            LevelModeSelected.Invoke(LevelMode.Level2LearnMode);
        }

        private void OnLevel2QuizModeSelected()
        {
            levelMode = LevelMode.Level2QuizMode;
            LevelModeSelected.Invoke(LevelMode.Level2QuizMode);
        }

        private void OnLevel3LearnModeSelected()
        {
            levelMode = LevelMode.Level3LearnMode;
            LevelModeSelected.Invoke(LevelMode.Level3LearnMode);
        }

        private void OnLevel3QuizModeSelected()
        {
            levelMode = LevelMode.Level3QuizMode;
            LevelModeSelected.Invoke(LevelMode.Level3QuizMode);
        }

        private async UniTask RemindUserToSelectLevel(float audioClipLengthS, CancellationToken ct)
        {
            while (!ct.IsCancellationRequested)
            {
                await UniTask.Delay(reminderIntervalS*1000 + (int)(audioClipLengthS*1000f), cancellationToken: ct);
                audioSource.PlayOneShot(pressButtonReminderAudioClip);
            }
        }

        private bool HasUnlockedNewLevel(Session session)
        {
            // demo mode does not restrict the user from accessing any level, all levels are unlocked
            if (session.AppMode == AppMode.DemoMode)
                return false;

            bool hasUnlockedNewLevel = false;

            // check if the user has unlocked a new level by comparing the sesson completed levels with the unlocked levels
            if (session.CompletedLevels.Count > unlockedLevels.Count)
            {
                hasUnlockedNewLevel = true;
            }

            unlockedLevels.Clear();
            unlockedLevels.AddRange(session.CompletedLevels);

            Debug.Log($"session.CompletedLevels.Count={session.CompletedLevels.Count}, unlockedLevels.Count={unlockedLevels.Count}. Has unlocked new level: {hasUnlockedNewLevel}");

            return hasUnlockedNewLevel;
        }
    }
}

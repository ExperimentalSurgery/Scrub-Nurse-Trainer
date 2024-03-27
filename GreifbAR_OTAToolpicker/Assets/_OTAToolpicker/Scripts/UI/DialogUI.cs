using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.UI;
using Tweens;
using UnityEngine.Assertions;
using UnityEngine.Serialization;

namespace NMY.OTAToolpicker.UI
{
    [System.Flags]
    public enum DialogButton {
        None=0,
        Primary = 1,
        Secondary = 2,
        Tertiary = 4
    }

    /// <summary>
    /// A simple dialog UI with up to three buttons that can be used in a functional style.
    /// </summary>
    /// <remarks>
    /// The dialog can be shown with a title, a message and up to three buttons.
    /// The dialog can be shown or hidden with or without tweening.
    /// </remarks>
    public class DialogUI : MonoBehaviour
    {
        [Tooltip("If <b>true</b>, the dialog will be shown when Start() is called.")]
        [SerializeField] private bool showInitially = false;

        [Header("Buttons")]
        [Tooltip("Which buttons should be shown in the dialog?")]
        [FormerlySerializedAs("dialogButtons")]
        [SerializeField] private DialogButton buttons = DialogButton.Primary;
        public DialogButton Buttons => buttons;
        [Tooltip("If <b>true</b>, the dialog will be hidden when any button is clicked.")]
        [SerializeField] private bool hideOnButtonClicked = true;

        [Header("Texts")]
        [SerializeField] private LocalizedString title;
        public LocalizedString Title => title;
        [SerializeField] private LocalizedString message;
        public LocalizedString Message => message;
        [SerializeField] private LocalizedString primaryButtonText;
        [SerializeField] private LocalizedString secondaryButtonText;
        [SerializeField] private LocalizedString tertiaryButtonText;

        [Header("UI Components")]
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text messageText;
        [SerializeField] private Button primaryButton;
        [SerializeField] private Button secondaryButton;
        [SerializeField] private Button tertiaryButton;
        private CanvasGroup canvasGroup = null;

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

        private bool wasPrimaryButtonClicked = false;
        private bool wasSecondaryButtonClicked = false;
        private bool wasTertiaryButtonClicked = false;

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
            messageText.text = message.GetLocalizedString();

            ConfigureButtons();

            // Do NOT move the localization event subscription to OnEnable()!
            // Otherwise some of the Show() methods will not work properly.
            title.StringChanged += UpdateTitleText;
            message.StringChanged += UpdateMessageText;

            if (showInitially) {
                await Show();
            }
            else {
                gameObject.SetActive(false);
            }
        }

        void OnDestroy()
        {
            title.StringChanged -= UpdateTitleText;
            message.StringChanged -= UpdateMessageText;
        }

        private void UpdateTitleText(string text) => titleText.text = text;
        private void UpdateMessageText(string text) => messageText.text = text;

        async public UniTask<DialogButton> Show(string title, string message, string primaryButtonText, string secondaryButtonText=null, string tertiaryButtonText=null, CancellationToken ct=default(CancellationToken))
        {
            titleText.text = title;
            messageText.text = message;
            SetPrimaryButtonText(primaryButtonText);
            SetSecondaryButtonText(secondaryButtonText ?? string.Empty);
            SetTertiaryButtonText(tertiaryButtonText ?? string.Empty);
            return await Show(ct);
        }

        async public UniTask<DialogButton> Show(string title, string message, CancellationToken ct=default(CancellationToken))
        {
            titleText.text = title;
            messageText.text = message;
            return await Show(ct);
        }

        async public UniTask<DialogButton> Show(CancellationToken ct=default(CancellationToken))
        {
            gameObject.SetActive(true);
            if (useTweening && canvasGroup) ShowWithTween();

            try {
                wasPrimaryButtonClicked = wasSecondaryButtonClicked = wasTertiaryButtonClicked = false;
                await UniTask.WaitUntil(() => wasPrimaryButtonClicked || wasSecondaryButtonClicked || wasTertiaryButtonClicked, cancellationToken: ct);
                if (hideOnButtonClicked)
                    Hide();
                return GetClickedButton();
            }
            catch (System.OperationCanceledException)
            {
                Hide();
                return DialogButton.None;
            }
        }

        public void Hide()
        {
            if (useTweening && canvasGroup)
                HideWithTween();
            else
                gameObject.SetActive(false);
        }

        /// <summary>
        /// Sets the interactable state of the specified button.
        /// </summary>
        /// <param name="button">The button to set the interactable state for.</param>
        /// <param name="interactable"><c>true</c> to make the button interactable, <c>false</c> to make it non-interactable.</param>
        public void SetButtonInteractable(DialogButton button, bool interactable)
        {
            if ((buttons & button) == 0) return;

            if (button == DialogButton.Primary)
                primaryButton.interactable = interactable;
            else if (button == DialogButton.Secondary)
                secondaryButton.interactable = interactable;
            else if (button == DialogButton.Tertiary)
                tertiaryButton.interactable = interactable;
        }

        /// <summary>
        /// Sets the interactable state of the primary button.
        /// </summary>
        /// <param name="interactable"><c>true</c> to make the button interactable, <c>false</c> to make it non-interactable.</param>
        public void SetPrimaryButtonInteractable(bool interactable) => SetButtonInteractable(DialogButton.Primary, interactable);
        /// <summary>
        /// Sets the interactable state of the secondary button.
        /// </summary>
        /// <param name="interactable"><c>true</c> to make the button interactable, <c>false</c> to make it non-interactable.</param>
        public void SetSecondaryButtonInteractable(bool interactable) => SetButtonInteractable(DialogButton.Secondary, interactable);
        /// <summary>
        /// Sets the interactable state of the tertiary button.
        /// </summary>
        /// <param name="interactable"><c>true</c> to make the button interactable, <c>false</c> to make it non-interactable.</param>
        public void SetTertiaryButtonInteractable(bool interactable) => SetButtonInteractable(DialogButton.Tertiary, interactable);

        public void SetPrimaryButtonText(string text) => primaryButton.GetComponentInChildren<TMP_Text>().text = text;
        public void SetSecondaryButtonText(string text) => secondaryButton.GetComponentInChildren<TMP_Text>().text = text;
        public void SetTertiaryButtonText(string text) => tertiaryButton.GetComponentInChildren<TMP_Text>().text = text;

        private void ShowWithTween()
        {
            hideTweenInstance?.Cancel();
            // additionally use CancelTweens() to cancel any previous tweens
            // because cancelling the hideTweenInstance did not work as expected :( (why?)
            gameObject.CancelTweens();
            canvasShowTween.from = canvasGroup.alpha;
            canvasShowTween.to = 1f;
            // Debug.Log($"Adding show tween instance... from={canvasShowTween.from.ToString()}, to={canvasShowTween.to.ToString()}");
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

        private void OnEnable()
        {
            primaryButton.onClick.AddListener(OnPrimaryButtonClicked);
            secondaryButton.onClick.AddListener(OnSecondaryButtonClicked);
            tertiaryButton.onClick.AddListener(OnTertiaryButtonClicked);
        }

        private void OnDisable()
        {
            primaryButton.onClick.RemoveListener(OnPrimaryButtonClicked);
            secondaryButton.onClick.RemoveListener(OnSecondaryButtonClicked);
            tertiaryButton.onClick.RemoveListener(OnTertiaryButtonClicked);
        }

        private void OnPrimaryButtonClicked() => wasPrimaryButtonClicked = true;
        private void OnSecondaryButtonClicked() => wasSecondaryButtonClicked = true;
        private void OnTertiaryButtonClicked() => wasTertiaryButtonClicked = true;

        private void ConfigureButtons()
        {
            if ((buttons & DialogButton.Primary) != 0 && primaryButton && !primaryButtonText.IsEmpty)
                primaryButton.GetComponentInChildren<TMP_Text>().text = primaryButtonText.GetLocalizedString();
            else
                primaryButton.gameObject.SetActive(false);

            if ((buttons & DialogButton.Secondary) != 0 && secondaryButton && !secondaryButtonText.IsEmpty)
                secondaryButton.GetComponentInChildren<TMP_Text>().text = secondaryButtonText.GetLocalizedString();
            else
                secondaryButton.gameObject.SetActive(false);

            if ((buttons & DialogButton.Tertiary) != 0 && tertiaryButton && !tertiaryButtonText.IsEmpty)
                tertiaryButton.GetComponentInChildren<TMP_Text>().text = tertiaryButtonText.GetLocalizedString();
            else
                tertiaryButton.gameObject.SetActive(false);
        }

        /// <summary>
        /// Returns which button was clicked.
        /// </summary>
        /// <returns>
        /// The button that was clicked. If no button was clicked, returns <see cref="DialogButton.None"/>.
        /// </returns>
        private DialogButton GetClickedButton()
        {
            if (wasPrimaryButtonClicked)
                return DialogButton.Primary;
            else if (wasSecondaryButtonClicked)
                return DialogButton.Secondary;
            else if (wasTertiaryButtonClicked)
                return DialogButton.Tertiary;
            else
                return DialogButton.None;
        }
    }
}

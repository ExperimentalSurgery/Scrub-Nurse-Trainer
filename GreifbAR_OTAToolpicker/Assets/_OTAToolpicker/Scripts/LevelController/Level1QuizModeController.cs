using Cysharp.Threading.Tasks;
using NMY.OTAToolpicker.UI;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using UnityEngine.Serialization;
using UnityEngine.Localization.SmartFormat.PersistentVariables;

namespace NMY.OTAToolpicker
{
    public class Level1QuizModeController : MonoBehaviour, ILevelController
    {
        [SerializeField] private OTAAppController app;
        public InstrumentMarkerController MarkerController => app.InstrumentMarkerController;
        [SerializeField] private bool resetSessionEntriesOnStart = true;

        [Tooltip("If true, the instrument details UI will be shown when an instrument is found.")]
        [SerializeField] private bool isShowingInstrumentDetails = true;

        private bool isLevelStopRequested = false;
        public bool IsLevelStopRequested
        {
            get => isLevelStopRequested;
            set => isLevelStopRequested = value;
        }

        [Header("Questions")]
        [SerializeField] private List<IdentificationQuestion> questionPool;
        public IEnumerable<IdentificationQuestion> QuestionPool => questionPool;
        public int QuestionPoolCount => questionPool.Count;

        [SerializeField] private int nrOfQuestionsToAsk = 5;
        [SerializeField] private int nrOfRequiredCorrectAnswers = 3;

        [Header("UI")]
        [SerializeField] private DialogUI infoDialogUI;
        [SerializeField] private DialogUI warningDialogUI;
        [SerializeField] private DialogUI questionDialogUI;
        [SerializeField] private DialogUI readyDialogUI;
        [SerializeField] private DialogUI resultsDialogUI;
        [SerializeField] private Button backButton;

        [Header("Audio source")]
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioSource sfxAudioSource;
        [Header("Audio clips")]
        [SerializeField] private AudioClip introAudioClip;
        [SerializeField] private AudioClip warningAudioClip;
        [SerializeField] private AudioClip explanationAudioClip;
        [SerializeField] private AudioClip reminderAudioClip;
        [SerializeField] private float reminderIntervalS = 5f;
        [SerializeField] private AudioClip resultsAudioClip;
        [Tooltip("Voice clip played when the user identifies the correct instrument.")]
        [SerializeField] private AudioClip correctAudioClip;
        [Tooltip("Voice clip played when the user identifies the wrong instrument.")]
        [SerializeField] private AudioClip wrongAudioClip;
        [SerializeField] private AudioClip passedQuizAudioClip;
        [SerializeField] private AudioClip failedQuizAudioClip;

        [Header("Audio clips - SFX")]
        [Tooltip("SFX played when the user identifies the correct instrument.")]
        [SerializeField] private AudioClip correctSfxAudioClip;
        [SerializeField] private float correctSfxVolume = 0.5f;
        [Tooltip("SFX played when the user identifies the wrong instrument.")]
        [SerializeField] private AudioClip wrongSfxAudioClip;
        [SerializeField] private float wrongSfxVolume = 0.5f;

        private CancellationTokenSource levelPlayingCts;
        private CancellationToken ct;

        public async UniTask StartLevelAsync()
        {
            if (levelPlayingCts != null) {
                Debug.LogError("Level1QuizModeController: Already running!");
                return;
            }

            ResetLevel();
            int nrOfCorrectAnswers = 0;
            int nrOfWrongAnswers = 0;

            levelPlayingCts = new CancellationTokenSource();
            ct = levelPlayingCts.Token;

            MarkerController.gameObject.SetActive(true);
            // in Level1 quiz mode the marker controller should not
            // play any confirmation audio clip when an instrument was lost, as we handle playing
            // the correct/wrong audio clip in this controller
            MarkerController.IsPlayingAudioOnInstrumentFound = false;
            MarkerController.IsShowingInstrumentDetails = isShowingInstrumentDetails;
            MarkerController.SetInstrumentElementsDisplayed(PlaceableInstrumentElement.None);

            // play audio intro and show info dialog. wait for any of them to finish first.
            await HelperTasks.ShowDialogWithAudio(infoDialogUI, audioSource, introAudioClip, ct: ct);

            // if the user has not completed the learn mode yet, show warning dialog
            if (app.Session.AppMode==AppMode.TeachMode && !app.Session.CompletedLevels.Contains(LevelMode.Level1LearnMode))
            {
                // play warning audio and show warning dialog. wait for either to finish
                await UniTask.Delay(1000, cancellationToken: ct);
                audioSource.PlayOneShot(warningAudioClip);
                DialogButton button = await warningDialogUI.Show(ct);

                // if the user selects "continue" flow will continue, otherwise stop the level
                if (button == DialogButton.Secondary) {
                    StopLevel();
                    return;
                }
                audioSource.Stop();
            }

            // level can only be stopped by pressing the main menu button from this point on.
            // prior to this, cancellation token should not be used.
            backButton.onClick.AddListener(StopLevel);
            backButton.gameObject.SetActive(true);

            await HelperTasks.PlayAndWaitForAudioClip(audioSource, explanationAudioClip, ct);
            if (ct.IsCancellationRequested) return;

            MarkerController.EnableAllInstrumentMarkers();

            // question loop, using Select and automatic tuple deconstruction to get the question and the index
            foreach(var (question,i) in GetRandomQuestions(nrOfQuestionsToAsk).Select((question, i) => (question, i)))
            {
                CancellationTokenSource questionCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
                questionDialogUI.Show(title: $"Frage {i+1}", message: question.Question.GetLocalizedString(), questionCts.Token).Forget();

                InstrumentMarker instrumentFound = await HelperTasks.WaitForAnyInstrumentIdentification(MarkerController, audioSource, reminderAudioClip, reminderIntervalS, ct);
                if (ct.IsCancellationRequested) return;

                string resultString = "";
                if(question.IsFulfilledBy(instrumentFound.Instrument)) {
                    Debug.Log("<color=green>Correct!</color>");
                    resultString = "Richtig!";
                    nrOfCorrectAnswers++;
                    HelperTasks.PlayOneShot(audioSource, correctAudioClip);
                    sfxAudioSource.PlayOneShot(correctSfxAudioClip, correctSfxVolume);
                    app.Session.Level1Quiz.AddQuestionAnswer(question, true);
                }
                else {
                    Debug.Log("<color=red>Wrong!</color>");
                    resultString = "Leider falsch";
                    nrOfWrongAnswers++;
                    HelperTasks.PlayOneShot(audioSource, wrongAudioClip);
                    sfxAudioSource.PlayOneShot(wrongSfxAudioClip, wrongSfxVolume);
                    app.Session.Level1Quiz.AddQuestionAnswer(question, false);
                }
                questionCts.Cancel();

                UniTask droppedInstrumentTask = HelperTasks.WaitForInstrumentDropped(MarkerController, instrumentFound, readyDialogUI, ct);
                string instrumentTitle = instrumentFound.Instrument.Title.GetLocalizedString();
                UniTask showReadyDialogTask = readyDialogUI.Show(resultString, $"Lege <b>{instrumentTitle}</b> ab und klicke auf \"Weiter\"", ct);
                await UniTask.WhenAll(droppedInstrumentTask, showReadyDialogTask);
            }

            MarkerController.DisableAllInstrumentMarkers();

            // deactivate the back button, as there will be one in the result dialog
            // which is displayed next
            backButton.gameObject.SetActive(false);

            audioSource.Stop();
            // while the audio clip is playing, we cannot cancel the level or return to the main menu
            await HelperTasks.PlayAndWaitForAudioClip(audioSource, resultsAudioClip, ct);

            // play "passed" or "failed" audio clip
            audioSource.Stop();
            if (nrOfCorrectAnswers >= nrOfRequiredCorrectAnswers)
                audioSource.PlayOneShot(passedQuizAudioClip);
            else
                audioSource.PlayOneShot(failedQuizAudioClip);

            // store the level completion in the session
            app.Session.AddCompletedLevel(LevelMode.Level1QuizMode);

            // we do not use a CancellationToken here, as we want to wait for the user to click a button
            (resultsDialogUI.Message["nrOfCorrectAnswers"] as IntVariable).Value = nrOfCorrectAnswers;
            (resultsDialogUI.Message["nrOfQuestionsToAsk"] as IntVariable).Value = nrOfQuestionsToAsk;
            DialogButton btn = await resultsDialogUI.Show();

            if (btn == DialogButton.Primary)
                StopLevel();
            else {
                // restart the level
                StopLevel();
                ResetLevel();
                await StartLevelAsync();
            }

            Debug.Log("Level1QuizModeController: Level finished.");
        }

        public void StopLevel()
        {
            levelPlayingCts?.Cancel();
            levelPlayingCts?.Dispose();
            levelPlayingCts = null;

            audioSource.Stop();

            infoDialogUI.Hide();
            warningDialogUI.Hide();
            questionDialogUI.Hide();
            readyDialogUI.Hide();
            resultsDialogUI.Hide();

            backButton.onClick.RemoveListener(StopLevel);
            backButton.gameObject.SetActive(false);

            MarkerController.DisableAllInstrumentMarkers();

            IsLevelStopRequested = true;
        }

        public void ResetLevel()
        {
            IsLevelStopRequested = false;
            backButton.gameObject.SetActive(false);
            IsLevelStopRequested = false;
            if (resetSessionEntriesOnStart) app.Session.Level1Quiz.Reset();
        }

        /// <summary>
        /// Returns a list of random questions from the question pool.
        /// </summary>
        /// <param name="count">The number of questions to return.</param>
        /// <returns></returns>
        private List<IdentificationQuestion> GetRandomQuestions(int count)
        {
            // limit count to the number of questions in the pool
            if (count > questionPool.Count)
                count = questionPool.Count;

            List<IdentificationQuestion> randomQuestions = new List<IdentificationQuestion>();
            for (int i = 0; i < count; i++)
            {
                int randomIndex = Random.Range(0, questionPool.Count);
                while (randomQuestions.Contains(questionPool[randomIndex]))
                {
                    randomIndex = Random.Range(0, questionPool.Count);
                }
                randomQuestions.Add(questionPool[randomIndex]);
            }
            return randomQuestions;
        }
    }
}

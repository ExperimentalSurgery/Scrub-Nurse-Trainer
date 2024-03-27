using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace NMY.OTAToolpicker
{
    [CreateAssetMenu(fileName = "Session", menuName = "OTA/Session", order = 1)]
    public class Session : ScriptableObject
    {
        [System.Serializable]
        public class GeneralSession
        {
            [SerializeField] private int nrOfTimesMainMenuShown = 0;
            public int NrOfTimesMainMenuShown => nrOfTimesMainMenuShown;
            public void IncrementNrOfTimesMainMenuShown() => nrOfTimesMainMenuShown++;
            public void Reset() => nrOfTimesMainMenuShown = 0;
        }

        [System.Serializable]
        public class Level1LearnSession
        {
            [SerializeField] private List<InstrumentData> identifiedInstruments = new();
            /// <summary>
            /// Adds an instrument to the list of identified instruments.
            /// If the instrument is already in the list, nothing happens.
            /// </summary>
            /// <param name="instrumentData">The instrument to add.</param>
            public void AddIdentifiedInstrument(InstrumentData instrumentData)
            {
                if (HasIdentifiedInstrument(instrumentData)) return;
                identifiedInstruments.Add(instrumentData);
            }
            public int NrOfIdentifiedInstruments => identifiedInstruments.Count;
            public bool HasIdentifiedInstrument(InstrumentData instrumentData) => identifiedInstruments.Contains(instrumentData);
            public void Reset() => identifiedInstruments.Clear();
        }

        [System.Serializable]
        public class Level1QuizSession
        {
            [System.Serializable]
            public class QuestionAnswer
            {
                public QuestionAnswer(IdentificationQuestion question, bool hasCorrectAnswer)
                {
                    this.question = question;
                    this.hasCorrectAnswer = hasCorrectAnswer;
                }
                [SerializeField]
                private IdentificationQuestion question;
                public IdentificationQuestion Question => question;
                [SerializeField] private bool hasCorrectAnswer = false;
                public bool HasCorrectAnswer => hasCorrectAnswer;
            }
            [SerializeField] private List<QuestionAnswer> questionAnswers = new();
            public int NrOfQuestionsAsked => questionAnswers.Count;
            public int NrOfCorrectAnswers => questionAnswers.FindAll(qa => qa.HasCorrectAnswer).Count;
            public int NrOfInstrumentsRequired => questionAnswers.Count;
            public void AddQuestionAnswer(QuestionAnswer questionAnswer) => questionAnswers.Add(questionAnswer);
            public void AddQuestionAnswer(IdentificationQuestion question, bool hasCorrectAnswer) => questionAnswers.Add(new QuestionAnswer(question, hasCorrectAnswer));

            public void Reset() {
                questionAnswers.Clear();
            }
        }

        // [System.Serializable]
        // public class Level2QuizSession
        // {
        //     // PlacementResult is not a ScriptableObject, so we cannot store it in the Session (which is a ScriptableObject).
        //     [SerializeField]
        //     private List<PlacementResult> placementResults = new();
        //     public IEnumerable<PlacementResult> PlacementResults => placementResults;
        //     public int NrOfPlacements => placementResults.Count;
        //     public void AddPlacementResult(PlacementResult placementResult) => placementResults.Add(placementResult);
        //     public PlacementResult GetPlacementResult(PlaceableInstrument placeableInstrument) => placementResults.Find(pr => pr.placeableInstrument == placeableInstrument);
        //     public void Reset() => placementResults.Clear();
        // }

        [SerializeField] private AppMode appMode = AppMode.Initial;

        public AppMode AppMode {
            get => appMode;
            set => appMode = value;
        }

        [SerializeField] private List<LevelMode> completedLevels = new List<LevelMode>();

        public List<LevelMode> CompletedLevels {
            get => completedLevels;
            set => completedLevels = value;
        }

        public bool HasCompletedLevel(LevelMode levelMode)
        {
            return completedLevels.Contains(levelMode);
        }

        public void AddCompletedLevel(LevelMode levelMode)
        {
            if (!HasCompletedLevel(levelMode))
            {
                completedLevels.Add(levelMode);
            }
        }

        public void AddAllLevelsAsCompleted()
        {
            completedLevels.Clear();
            foreach (LevelMode levelMode in System.Enum.GetValues(typeof(LevelMode)))
            {
                completedLevels.Add(levelMode);
            }

            completedLevels.Remove(LevelMode.Initial);
        }

        public void Reset()
        {
            appMode = AppMode.Initial;
            completedLevels.Clear();
            generalSession.Reset();
            level1Learn.Reset();
            level1Quiz.Reset();
            // level2Quiz.Reset();
        }

        [SerializeField] private GeneralSession generalSession = new GeneralSession();
        public GeneralSession General => generalSession;

        #region Level1Learn
        [SerializeField] private Level1LearnSession level1Learn = new();
        public Level1LearnSession Level1Learn  => level1Learn;
        #endregion

        #region Level1Quiz
        [SerializeField] private Level1QuizSession level1Quiz = new();
        public Level1QuizSession Level1Quiz => level1Quiz;
        #endregion

        // #region Level2Quiz
        // [SerializeField] private Level2QuizSession level2Quiz = new();
        // public Level2QuizSession Level2Quiz => level2Quiz;
        // #endregion

    }
}

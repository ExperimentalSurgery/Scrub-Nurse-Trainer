using Cysharp.Threading.Tasks;
using Leap;
using Leap.Unity;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using UnityEngine;
using UnityEngine.Events;


namespace NMY.OTAToolpicker.UI
{
    static public class HelperTasks
    {
        static public void PlayOneShot(AudioSource audioSource, AudioClip audioClip, bool stopSourceBeforePlayingClip=true)
        {
            if (stopSourceBeforePlayingClip)
                audioSource.Stop();
            audioSource.PlayOneShot(audioClip);
        }

        /// <summary>
        /// Show an info dialog and play an audio clip. Wait for either to finish.
        /// </summary>
        /// <param name="dialog">The dialog to show.</param>
        /// <param name="dialogTitle">The title text of the dialog. If <c>null</c>, the title configured in the <paramref name="dialog"/> is used.</param>
        /// <param name="dialogText">The text of the dialog. If <c>null</c>, the text configured in the <paramref name="dialog"/> is used.</param>
        /// <param name="audioSource">The audio source used to play the <paramref name="audioClip"/>.</param>
        /// <param name="audioClip">The audio clip to play.</param>
        /// <param name="ct">A CancellationToken.</param>
        /// <returns></returns>
        static async public UniTask<DialogButton> ShowDialogWithAudio(DialogUI dialog, AudioSource audioSource, AudioClip audioClip,
                                                                string dialogTitle=null, string dialogText=null, bool shouldWaitForClick=false, CancellationToken ct=default)
        {
            try
            {
                if (audioSource == null || audioClip == null)
                    throw new ArgumentNullException("audioSource or audioClip is null!");

                string title = dialogTitle ?? dialog.Title.GetLocalizedString();
                string text = dialogText ?? dialog.Message.GetLocalizedString();
                audioSource.PlayOneShot(audioClip);
                UniTask<DialogButton> dialogTask = dialog.Show(title, text, ct);
                UniTask audioTask = UniTask.Delay((int)(audioClip.length*1000f), cancellationToken: ct);
                bool hasResult = false;
                DialogButton result = DialogButton.None;
                if (!shouldWaitForClick) {
                    (hasResult, result) = await UniTask.WhenAny(dialogTask, audioTask);
                }
                else
                    result = await dialogTask;
                // DialogButton btn = hasResult ? result : DialogButton.None;
                audioSource.Stop();
                dialog.Hide();
                return result;
            }
            catch (OperationCanceledException) {}

            Debug.Log("UIHelper: ShowDialogWithAudio was cancelled.");
            audioSource.Stop();
            dialog.Hide();

            return DialogButton.None;
        }

        static async public UniTask<DialogButton> ShowDialog(DialogUI dialog,
                                                      string dialogTitle=null, string dialogText=null, CancellationToken ct=default)
        {
            try
            {
                string title = dialogTitle ?? dialog.Title.GetLocalizedString();
                string text = dialogText ?? dialog.Message.GetLocalizedString();
                DialogButton btn = await dialog.Show(title, text, ct);
                dialog.Hide();
                return btn;
            }
            catch (OperationCanceledException)
            {
                Debug.Log("UIHelper: ShowDialog was cancelled.");
                dialog.Hide();
                return DialogButton.None;
            }
        }

        /// <summary>
        /// Play a reminder audio clip at a given interval.
        /// </summary>
        /// <param name="delayS">The audio clip will be played after delayS seconds.</param>
        /// <param name="intervalS">The audio clip will repeat after intervalS seconds.</param>
        /// <param name="audioSource">The AudioSource to use for playback.</param>
        /// <param name="reminderAudioClip">The AudioClip to play.</param>
        /// <param name="ct">The CancellationToken.</param>
        /// <returns>Returns when the task was cancelled via the <paramref name="ct"/>.</returns>
        static async public UniTask PlayReminderAudioClip(float delayS, float intervalS, AudioSource audioSource, AudioClip reminderAudioClip, CancellationToken ct)
        {
            await UniTask.Delay((int)(delayS*1000f), cancellationToken: ct);

            while (!ct.IsCancellationRequested)
            {
                audioSource.PlayOneShot(reminderAudioClip);
                await UniTask.Delay((int)((intervalS+reminderAudioClip.length)*1000f), cancellationToken: ct);
            }

            return;
        }

        /// <summary>
        /// Play an audio clip at a given interval, until the <paramref name="condition"/> is fulfilled or the task is cancelled via the <paramref name="ct"/>.
        /// </summary>
        /// <param name="condition">If resolves to <c>true</c> the task will be stopped.</param>
        /// <param name="initialDelayS">The initial delay in seconds. This is waited for only once.</param>
        /// <param name="audioSource">The audio source used to play the clip.</param>
        /// <param name="audioClip">The audio clip to play.</param>
        /// <param name="intervalS">The interval in seconds to play the audio clip.</param>
        /// <param name="ct">The CancellationToken.</param>
        static async public UniTask PlayAudioClipIntervalUntil(Func<bool> condition, float initialDelayS, AudioSource audioSource, AudioClip audioClip, float intervalS, CancellationToken ct)
        {
            await UniTask.Delay((int)(initialDelayS*1000f), cancellationToken: ct);

            try {
                while (!ct.IsCancellationRequested && !condition())
                {
                    await UniTask.Delay((int)(intervalS*1000f), cancellationToken: ct);

                    // if the condition is fulfilled after waiting, break the loop and do not play the audio clip
                    if (condition()) break;

                    audioSource.clip = audioClip;
                    audioSource.Play();
                    await UniTask.Delay((int)(audioClip.length*1000f), cancellationToken: ct);
                }
            }
            catch (OperationCanceledException) {}
            finally {
                // stopping the audio source here might stop other sounds being played
                // by the same audio source. It's better to stop the audio source from
                // the outside.
                // audioSource.Stop();
                Debug.Log($"Level1Controller::PlayAudioClipInterval cancelled. ct.IsCancellationRequested: {ct.IsCancellationRequested}, condition: {condition()}");
            }
        }

        /// <summary>
        /// Play an audio clip and wait for it to finish.
        /// </summary>
        /// <remarks>
        /// When the task is cancelled via the <paramref name="ct"/>, the audio source will be stopped.
        /// </remarks>
        /// <param name="audioSource">The audio source to use.</param>
        /// <param name="audioClip">The audio clip to play.</param>
        /// <param name="ct">The cancellation token.</param>
        static async public UniTask PlayAndWaitForAudioClip(AudioSource audioSource, AudioClip audioClip, CancellationToken ct)
        {
            audioSource.PlayOneShot(audioClip);

            try {
                await UniTask.Delay((int)(audioClip.length*1000f), cancellationToken: ct);
            }
            catch (OperationCanceledException) {}
            finally {
                audioSource.Stop();
            }
        }

        /// <summary>
        /// Monitor the tracking of a specific instrument and call the given actions when the instrument is tracked/visible or not.
        /// </summary>
        /// <param name="instrument">The instrument to monitor.</param>
        /// <param name="markerController">The marker controller to use.</param>
        /// <param name="onTracked">The action to call when the instrument is tracked/visible.</param>
        /// <param name="onNotTracked">The action to call when the instrument is not tracked/visible.</param>
        /// <param name="pauseMS">The pause in milliseconds between checks.</param>
        /// <param name="ct">The cancellation token.</param>
        static async public UniTask MonitorInstrumentTracking(InstrumentData instrument,
            InstrumentMarkerController markerController, Action onTracked, Action onNotTracked, int pauseMS=100, CancellationToken ct=default)
        {
            while(ct.IsCancellationRequested == false)
            {
                if (markerController.IsCurrentlyTracked(instrument))
                    onTracked.Invoke();
                else
                    onNotTracked.Invoke();

                if (pauseMS > 0)
                    await UniTask.Delay(pauseMS, cancellationToken: ct);
            }
        }

        /// <summary>
        /// Monitor the tracking of instruments and call the given actions when an instrument is found or dropped.
        /// </summary>
        /// <param name="markerController">The marker controller to use.</param>
        /// <param name="onFound">The action to call when an instrument is found.</param>
        /// <param name="onDropped">The action to call when an instrument is dropped.</param>
        /// <param name="pauseMS">The pause in milliseconds between checks.</param>
        /// <param name="ct">The cancellation token.</param>
        static async public UniTask MonitorInstrumentTracking(InstrumentMarkerController markerController, Action<InstrumentMarker> onFound,
            Action<InstrumentMarker> onDropped, int pauseMS=100, CancellationToken ct=default)
        {
            UnityAction<InstrumentMarker> instrumentFound = (instrumentMarker) => onFound.Invoke(instrumentMarker);
            UnityAction<InstrumentMarker> instrumentDropped = (instrumentMarker) => onDropped.Invoke(instrumentMarker);

            markerController.InstrumentFound.AddListener(instrumentFound);
            markerController.InstrumentDropped.AddListener(instrumentDropped);

            while(ct.IsCancellationRequested == false)
            {
                if (pauseMS > 0)
                    await UniTask.Delay(pauseMS, cancellationToken: ct);
            }

            markerController.InstrumentFound.RemoveListener(instrumentFound);
            markerController.InstrumentDropped.RemoveListener(instrumentDropped);
        }

        /// <summary>
        /// Calibrate the table by showing a dialog and waiting for the user to press continue.
        /// </summary>
        /// <param name="tableMarker">The table marker to calibrate.</param>
        /// <param name="instrumentTable">The instrument table to disable placement volumes on.</param>
        /// <param name="tableCalibrationDialog">The dialog UI to use.</param>
        /// <param name="ct">The CancellationToken.</param>
        static async public UniTask CalibrateTable(
            TableMarker tableMarker,
            InstrumentTable instrumentTable,
            DialogUI tableCalibrationDialog,
            bool shouldWaitForClick,
            CancellationToken ct
        )
        {
            instrumentTable.gameObject.SetActive(true);
            tableMarker.EnableTracking();
            // Vuforia will enable all Renderer and Collider components of the tableMarker's children
            instrumentTable.DisableAllPlacementVolumes();

            Debug.Log("Before waiting for table marker");

            UniTask waitForTableMarkerTask = HelperTasks.WaitForTableMarker(tableMarker, ct);
            UniTask waitForDialogTask = HelperTasks.ShowDialog(tableCalibrationDialog, ct: ct);

            if (shouldWaitForClick)
                await HelperTasks.ShowDialog(tableCalibrationDialog, ct: ct);
            else {
                await UniTask.WhenAny(waitForTableMarkerTask, waitForDialogTask);
                tableCalibrationDialog.Hide();
            }

            Debug.Log("After waiting for table marker");

            // store the pose of the table and disable table tracking
            tableMarker.transform.GetPositionAndRotation(out Vector3 tablePosition, out Quaternion tableRotation);
            Vector3 tableScale = tableMarker.transform.localScale;
            tableMarker.DisableTracking();
            tableMarker.transform.rotation = Quaternion.Euler(0, tableMarker.transform.rotation.eulerAngles.y, 0);

            // apply the stored table pose (after tracking was disabled)
            tableMarker.transform.SetPositionAndRotation(tablePosition, tableRotation);
            tableMarker.transform.localScale = tableScale;
        }

        static async public UniTask<InstrumentMarker> WaitForAnyInstrumentIdentification(
            IInstrumentMarkerController markerController,
            AudioSource audioSource,
            AudioClip reminderAudioClip,
            float reminderIntervalS,
            CancellationToken ct)
        {
            InstrumentMarker lastInstrumentIdentified = null;

            CancellationTokenSource reminderCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            HelperTasks.PlayAudioClipIntervalUntil(() => lastInstrumentIdentified != null,
                initialDelayS: 0f,
                audioSource: audioSource,
                audioClip: reminderAudioClip,
                intervalS: reminderIntervalS,
                ct: reminderCts.Token
            ).Forget();

            UnityAction<InstrumentMarker> foundFunc = (instrumentMarker) => lastInstrumentIdentified=instrumentMarker;
            markerController.InstrumentFound.AddListener(foundFunc);

            try {
                Debug.Log("Wait for instrument identification.");
                await UniTask.WaitUntil(() => lastInstrumentIdentified, cancellationToken: ct);
            }
            catch (OperationCanceledException) {
                Debug.Log("WaitForInstrumentIdentification: Operation cancelled.");
            }

            reminderCts.Cancel();
            reminderCts.Dispose();
            markerController.InstrumentFound.RemoveListener(foundFunc);

            return lastInstrumentIdentified;
        }

        static async public UniTask<InstrumentMarker> WaitForAnyCloseInstrumentIdentification(
            InstrumentMarkerController markerController,
            AudioSource audioSource,
            AudioClip reminderAudioClip,
            float reminderIntervalS,
            CancellationToken ct)
        {
            InstrumentMarker lastInstrumentIdentified = null;
            
            // lastInstrumentIdentified = markerController.GetNearestInstrumentMarker();
            // bool isAboveTable = lastInstrumentIdentified.transform.position.y > markerController.tableRerefence.position.y + markerController.minHeightAboveTable;

            CancellationTokenSource reminderCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            HelperTasks.PlayAudioClipIntervalUntil(() => markerController.GetNearestInstrumentMarker() != null && (markerController.GetNearestInstrumentMarker().transform.position.y > markerController.tableRerefence.position.y + markerController.minHeightAboveTable),
                initialDelayS: 0f,
                audioSource: audioSource,
                audioClip: reminderAudioClip,
                intervalS: reminderIntervalS,
                ct: reminderCts.Token
            ).Forget();

            // UnityAction<InstrumentMarker> foundFunc = (instrumentMarker) => {                
            //     lastInstrumentIdentified=instrumentMarker;                
            // };
            // markerController.InstrumentFound.AddListener(foundFunc);

            try {
                Debug.Log("Wait for instrument identification.");
                await UniTask.WaitUntil(() => markerController.GetNearestInstrumentMarker() && (markerController.GetNearestInstrumentMarker().transform.position.y > markerController.tableRerefence.position.y + markerController.minHeightAboveTable), cancellationToken:ct);
                lastInstrumentIdentified = markerController.GetNearestInstrumentMarker();
            }
            catch (OperationCanceledException) {
                Debug.Log("WaitForInstrumentIdentification: Operation cancelled.");
            }

            reminderCts.Cancel();
            reminderCts.Dispose();
            // markerController.InstrumentFound.RemoveListener(foundFunc);

            return lastInstrumentIdentified;
        }

        static async public UniTask<InstrumentMarker> Level1LearnWaitForMarkerAboveThreshold(
            InstrumentMarkerController markerController,
             Action<InstrumentMarker> onFound,            
            CancellationToken ct=default)
        {
            InstrumentMarker lastInstrumentIdentified = null;
            int pauseMS = 30;

            while(ct.IsCancellationRequested == false)
            {                
                float tableThresholdHeight = markerController.tableRerefence.position.y + markerController.minHeightAboveTable;
                await UniTask.WaitUntil(() => 
                    markerController.GetNearestInstrumentMarker() && (markerController.GetNearestInstrumentMarker().transform.position.y > tableThresholdHeight), 
                    cancellationToken:ct
                );
                lastInstrumentIdentified = markerController.GetNearestInstrumentMarker();
                StaticlastInstrumentIdentified = lastInstrumentIdentified;   
                // Debug.Log("Level1LearnWaitForMarkerAboveThreshold: " + lastInstrumentIdentified.gameObject.name);
                onFound(lastInstrumentIdentified);

                await UniTask.Delay(pauseMS, cancellationToken: ct);
            }

           

            return lastInstrumentIdentified;
        }

        public static InstrumentMarker StaticlastInstrumentIdentified;

        static async public UniTask<InstrumentMarker> Level1LearnWaitForMarkerBelowThreshold(
            InstrumentMarkerController markerController,
            Func<InstrumentMarker> lastInstrumentFoundFunc,
            Action<InstrumentMarker> onDropped, 
            CancellationToken ct=default)
        {
            
            int pauseMS= 30;
            

            while(ct.IsCancellationRequested == false)
            {
                

                if(StaticlastInstrumentIdentified) Debug.Log("static last instrumenIdentified="+StaticlastInstrumentIdentified.gameObject.name);
                float tableThresholdHeight = markerController.tableRerefence.position.y + markerController.minHeightAboveTable;
                await UniTask.WaitUntil(() => 
                    (markerController.GetNearestInstrumentMarker() !=null && markerController.GetNearestInstrumentMarker()==StaticlastInstrumentIdentified && (markerController.GetNearestInstrumentMarker().transform.position.y < tableThresholdHeight)) ||
                    (StaticlastInstrumentIdentified!=null && !StaticlastInstrumentIdentified.VarjoMarker.IsTracked), cancellationToken:ct);

                
                onDropped(StaticlastInstrumentIdentified);
                StaticlastInstrumentIdentified = null;
                await UniTask.Delay(pauseMS, cancellationToken: ct);
            }

            return StaticlastInstrumentIdentified;
        }

        // static async public UniTask<InstrumentMarker> WaitForAnyCloseInstrumentIdentification(
        //     InstrumentMarkerController markerController,
        //     AudioSource audioSource,
        //     AudioClip reminderAudioClip,
        //     float reminderIntervalS,
        //     CancellationToken ct = default(CancellationToken),
        //     int pauseMS = 40
        // )
        // {
        //     InstrumentMarker currentlyTrackedMarker = null;

        //     try
        //     {
        //         bool isAboveTable = false;
        //         while (currentlyTrackedMarker == null && !isAboveTable)
        //         {
        //             if (pauseMS > 0)
        //                 await UniTask.Delay(pauseMS, cancellationToken: ct);

        //             currentlyTrackedMarker = markerController.GetNearestInstrumentMarker();
                    
        //             if (currentlyTrackedMarker)
        //                 isAboveTable = currentlyTrackedMarker.transform.position.y > markerController.tableRerefence.position.y + markerController.minHeightAboveTable;
        //         }
        //     }
        //     catch (OperationCanceledException) { }

        //     return currentlyTrackedMarker;
        // }

        /// <summary>
        /// Waits for a specific instrument to be found by the marker controller.
        /// </summary>
        /// <remarks>
        /// This method will play a reminder audio clip every x seconds if provided.
        /// </remarks>
        /// <param name="markerController">The marker controller to use.</param>
        /// <param name="instrumentData">The instrument to wait for.</param>
        /// <param name="audioSource">The audio source used to play the scan reminder and result audio clips.</param>
        /// <param name="wrongInstrumentAudioClip">The audio clip played when the user identified the wrong instrument.</param>
        /// <param name="reminderAudioClip">The reminder audio clip played when the user did not identify any instrument for <paramref name="scanReminderIntervalS"/> seconds.</param>
        /// <param name="scanReminderIntervalS">The interval of the scan reminder audio message.</param>
        /// <param name="pauseMS">Pause in milliseconds between the checks.</param>
        /// <param name="ct">The CancellationToken.</param>
        /// <returns></returns>
        async static public UniTask<InstrumentMarker> WaitForSpecificInstrument(
            InstrumentMarkerController markerController,
            InstrumentData instrumentData,
            AudioSource audioSource,
            AudioClip wrongInstrumentAudioClip,
            AudioClip reminderAudioClip=null,
            float scanReminderIntervalS=12f,
            int pauseMS=40,
            CancellationToken ct=default(CancellationToken)
        )
        {
            InstrumentMarker currentlyTrackedInstrument = null;

            UnityAction<InstrumentMarker> instrumentFound = (instrumentMarker) => {
                currentlyTrackedInstrument = instrumentMarker;
                if (instrumentMarker.Instrument != instrumentData)
                {
                    HelperTasks.PlayOneShot(audioSource, wrongInstrumentAudioClip);
                    Debug.Log("Level3QuizModeController::WaitForInstrument: Wrong instrument found.");
                }
            };
            UnityAction<InstrumentMarker> instrumentDropped = (instrumentMarker) => {
                currentlyTrackedInstrument = null;
            };

            markerController.InstrumentFound.AddListener(instrumentFound);
            markerController.InstrumentDropped.AddListener(instrumentDropped);

            // if a reminder audio clip is provided, play it every x seconds
            using CancellationTokenSource reminderCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            if (reminderAudioClip != null)
            {
                HelperTasks.PlayReminderAudioClip(reminderAudioClip.length + scanReminderIntervalS, scanReminderIntervalS, audioSource, reminderAudioClip, ct: reminderCts.Token).Forget();
            }

            try {
                while(currentlyTrackedInstrument==null || currentlyTrackedInstrument.Instrument!=instrumentData)
                {
                    if (pauseMS > 0)
                        await UniTask.Delay(pauseMS, cancellationToken: ct);
                }
            }
            catch (OperationCanceledException) {}

            reminderCts.Cancel();

            markerController.InstrumentFound.RemoveListener(instrumentFound);
            markerController.InstrumentDropped.RemoveListener(instrumentDropped);

            return currentlyTrackedInstrument;
        }

        /// <summary>
        /// Waits for the user to drop the <paramref name="instrumentMarker"/> and then enables the primary button of the <paramref name="dialogUI"/>.
        /// </summary>
        /// <param name="markerController">The controller for the markers.</param>
        /// <param name="instrumentMarker">The instrument waiting to be dropped.</param>
        /// <param name="dialogUI">The dialog UI to enable the button in.</param>
        /// <param name="ct">The CancellationToken.</param>
        /// <returns></returns>
        static public async UniTask WaitForInstrumentDropped(InstrumentMarkerController markerController, InstrumentMarker instrumentMarker, DialogUI dialogUI, CancellationToken ct)
        {
            Debug.Log($"Waiting for instrument '{instrumentMarker.Instrument.Title.GetLocalizedString()}' being dropped.");
            bool hasDroppedInstrument = false;
            InstrumentMarker droppedInstrument = null;

            dialogUI.SetPrimaryButtonInteractable(false);

            UnityAction<InstrumentMarker> droppedFunc = (instrumentMarker) => {
                hasDroppedInstrument = true;
                droppedInstrument = instrumentMarker;
            };
            markerController.InstrumentDropped.AddListener(droppedFunc);

            try {
                await UniTask.WaitUntil(() => hasDroppedInstrument && droppedInstrument==instrumentMarker, cancellationToken: ct);
            }
            catch (System.OperationCanceledException) {
                // if we dont catch the possible exception from WaitUntil, the event listener will not be removed
            }

            Debug.Log($"Instrument '{instrumentMarker.Instrument.Title.GetLocalizedString()}' was dropped.");
            dialogUI.SetPrimaryButtonInteractable(true);
            markerController.InstrumentDropped.RemoveListener(droppedFunc);
        }

        static public async UniTask WaitForInstrumentDroppedOrBelowTableThreshold(InstrumentMarkerController markerController, InstrumentMarker instrumentMarker, DialogUI dialogUI, CancellationToken ct)
        {
            Debug.Log($"Waiting for instrument '{instrumentMarker.Instrument.Title.GetLocalizedString()}' being dropped.");
            bool hasDroppedInstrument = false;
            InstrumentMarker droppedInstrument = null;

            dialogUI.SetPrimaryButtonInteractable(false);

            UnityAction<InstrumentMarker> droppedFunc = (instrumentMarker) => {
                hasDroppedInstrument = true;
                droppedInstrument = instrumentMarker;
            };
            markerController.InstrumentDropped.AddListener(droppedFunc);

            // markerController.GetNearestInstrumentMarker() != null && (markerController.GetNearestInstrumentMarker().transform.position.y > markerController.tableRerefence.position.y + markerController.minHeightAboveTable)

            try {
                await UniTask.WaitUntil(() => 
                    (hasDroppedInstrument && droppedInstrument==instrumentMarker) ||
                     markerController.GetNearestInstrumentMarker() != null && (markerController.GetNearestInstrumentMarker().transform.position.y < markerController.tableRerefence.position.y + markerController.minHeightAboveTable)
                    , cancellationToken: ct
                );
            }
            catch (System.OperationCanceledException) {
                // if we dont catch the possible exception from WaitUntil, the event listener will not be removed
            }

            Debug.Log($"Instrument '{instrumentMarker.Instrument.Title.GetLocalizedString()}' was dropped.");
            dialogUI.SetPrimaryButtonInteractable(true);
            markerController.InstrumentDropped.RemoveListener(droppedFunc);
        }

        static public async UniTask WaitForTableMarker(TableMarker tableMarker, CancellationToken ct)
        {
            bool hasTableMarker = false;

            UnityAction tableMarkerFound = () => hasTableMarker = true;
            UnityAction tableMarkerLost = () => hasTableMarker = false;

            tableMarker.OnMarkerFound.AddListener(tableMarkerFound);
            tableMarker.OnMarkerLost.AddListener(tableMarkerLost);

            try {
                await UniTask.WaitUntil(() => hasTableMarker, cancellationToken: ct);
            }
            catch (System.OperationCanceledException) {
                // if we dont catch the possible exception from WaitUntil,
                // the flow will be interupted and the event listener will not be removed
            }

            tableMarker.OnMarkerFound.RemoveListener(tableMarkerFound);
            tableMarker.OnMarkerLost.RemoveListener(tableMarkerLost);
        }

        /// <summary>
        /// Monitors a hand pose to be present for a certain duration. Returns the duration the pose was held.
        /// </summary>
        /// <param name="handPoseDetector">The hand pose detector to use.</param>
        /// <param name="handPose">The hand pose to monitor.</param>
        /// <param name="durationS">The duration in seconds the hand pose has to be present.</param>
        /// <param name="onPoseDetected">Called when the pose was detected.</param>
        /// <param name="onPoseLost">Called when the pose was lost.</param>
        /// <param name="ct">The CancellationToken</param>
        /// <returns>The duration in seconds the pose was held.</returns>
        async static public UniTask<float> MonitorHandPoseHold(
            LeapServiceProvider leapProvider,
            HandPoseDetector handPoseDetector,
            HandoverData handoverData,
            float durationS,
            Action onPoseDetected,
            Action onPoseLost,
            Action onHandLost,
            Action onHandVisible,
            CancellationToken ct
        )
        {
            bool wasDetectorEnabled = handPoseDetector.enabled;
            bool isPoseDetected = false;
            float startTimeS = Time.time;

            using CancellationTokenSource monitorCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            async UniTask MonitorHand(CancellationToken ct)
            {
                while (!ct.IsCancellationRequested)
                {
                    if (leapProvider.CurrentFrame!=null)
                    {
                        Frame frame =  new();
                        frame.CopyFrom(leapProvider.CurrentFrame);
                        if (frame.Hands.Count == 0) onHandLost();
                        else if (frame.Hands.Count > 0 && leapProvider.GetHand(Chirality.Right)!=null && !isPoseDetected) onHandVisible();
                    }
                    try {
                        await UniTask.Yield(ct);
                    }
                    catch (System.OperationCanceledException) {}
                }
            }

            handPoseDetector.enabled = true;
            handPoseDetector.checkBothHands = false;
            handPoseDetector.chiralityToCheck = handoverData.Chirality;
            handPoseDetector.SetPosesToDetect(new List<HandPoseScriptableObject>() { handoverData.HandPose });

            MonitorHand(monitorCts.Token).Forget();

            UnityAction onPoseDetectedInternal = () => {
                isPoseDetected = true;
                startTimeS = Time.time;
                onPoseDetected();
                Debug.Log("Level3LearnModeController::MonitorHandPoseHold: Hand pose detected.");
            };
            UnityAction onPoseLostInternal = () => {
                isPoseDetected = false;
                onPoseLost();
                Debug.Log("Level3LearnModeController::MonitorHandPoseHold: Hand pose lost.");
            };

            Func<bool> isDurationElapsed = () => (Time.time - startTimeS) >= durationS;

            handPoseDetector.OnPoseDetected.AddListener(onPoseDetectedInternal);
            handPoseDetector.OnPoseLost.AddListener(onPoseLostInternal);

            // If the pose is already detected when this task is started, we need to call
            // the onPoseDetected action manually otherwise the timer will not start properly.
            // Basically we simulate the pose being detected at the start of the task.
            if (handPoseDetector.IsPoseCurrentlyDetected())
                onPoseDetectedInternal();

            try {
                await UniTask.WaitUntil(() => isDurationElapsed() && isPoseDetected, cancellationToken: ct);
            }
            catch (System.OperationCanceledException) {}

            handPoseDetector.OnPoseDetected.RemoveListener(onPoseDetectedInternal);
            handPoseDetector.OnPoseLost.RemoveListener(onPoseLostInternal);
            // reset the detector to its previous state
            handPoseDetector.enabled = wasDetectorEnabled;

            monitorCts.Cancel();

            return Time.time - startTimeS;
        }
    }
}

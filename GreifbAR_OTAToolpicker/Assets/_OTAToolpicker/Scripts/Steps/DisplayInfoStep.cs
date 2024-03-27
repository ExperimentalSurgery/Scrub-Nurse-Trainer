using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NMY.VirtualRealityTraining.Steps;
using Cysharp.Threading.Tasks;
using System.Threading;
using NMY.OTAToolpicker.UI;
using System.Threading.Tasks;

namespace NMY.OTAToolpicker
{
    public class DisplayInfoStep : BaseTrainingStep
    {
        [SerializeField] private DialogUI level1LearnModeInfoDialogUI;
        protected async override UniTask ClientStepActionAsync(CancellationToken ct)
        {
            try {
                Debug.Log("DisplayInfoStep ######################");
                await level1LearnModeInfoDialogUI.Show();
                RaiseClientStepFinished();
            }
            catch (System.OperationCanceledException)
            {
                RaiseClientStepFinished();
            }
            // try
            // {
            //     await level1LearnModeInfoDialogUI.Show();
            // }
            // catch (TaskCanceledException ex)
            // {
            //     Debug.LogError(ex);
            //     level1LearnModeInfoDialogUI.Hide();
            // }
        }
    }
}

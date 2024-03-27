using System.Threading;
using UnityEngine;
using NMY.OTAToolpicker.UI;
using UnityEngine.UI;

namespace NMY.OTAToolpicker
{
    public class DialogUISample : MonoBehaviour
    {
        [Header("Left dialog")]
        [SerializeField] private DialogUI infoDialogUILeft;

        [SerializeField] private Button showButtonLeft;
        [SerializeField] private Button hideButtonLeft;
        [SerializeField] private Button cancelButtonLeft;

        [Header("Right dialog")]
        [SerializeField] private DialogUI infoDialogUIRight;
        [SerializeField] private Button showButtonRight;
        [SerializeField] private Button hideButtonRight;
        [SerializeField] private Button cancelButtonRight;

        [Header("Extra dialog")]
        [SerializeField] private DialogUI infoDialogUIExtra;
        [SerializeField] private Button showButtonExtra;
        [SerializeField] private Button hideButtonExtra;
        [SerializeField] private Button cancelButtonExtra;

        private CancellationTokenSource ctsLeft;
        private CancellationTokenSource ctsRight;
        private CancellationTokenSource ctsExtra;

        void Start()
        {
            ctsLeft = new CancellationTokenSource();
            ctsRight = new CancellationTokenSource();
            ctsExtra = new CancellationTokenSource();

            showButtonLeft?.onClick.AddListener(async () => await infoDialogUILeft.Show(ctsLeft.Token));
            hideButtonLeft?.onClick.AddListener(() => infoDialogUILeft.Hide());
            showButtonRight?.onClick.AddListener(async ()
                => await infoDialogUIRight.Show("This is the title", "This is the dialog text.", "Primary button text that is longer", "Secondard button", ct: ctsRight.Token));
            hideButtonRight?.onClick.AddListener(() => infoDialogUIRight.Hide());

            cancelButtonLeft?.onClick.AddListener(() => {
                ctsLeft.Cancel();
                ctsLeft.Dispose();
                ctsLeft = new CancellationTokenSource();
            });
            cancelButtonRight?.onClick.AddListener(() => {
                ctsRight.Cancel();
                ctsRight.Dispose();
                ctsRight = new CancellationTokenSource();
            });

            // --- extra dialog is shown using UIHelpers
            showButtonExtra?.onClick.AddListener(async () => await HelperTasks.ShowDialog(infoDialogUIExtra, "Extra dialog", "This is the extra dialog text.", ct: ctsExtra.Token));
            hideButtonExtra?.onClick.AddListener(() => infoDialogUIExtra.Hide());
            cancelButtonExtra?.onClick.AddListener(() => {
                ctsExtra.Cancel();
                ctsExtra.Dispose();
                ctsExtra = new CancellationTokenSource();
            });

        }

    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Threading.Tasks;
using System.Threading;
using Unity.VisualScripting;

namespace NMY.OTAToolpicker
{
    public class DotNETTaskSamples : MonoBehaviour
    {
        [SerializeField] private List<Color> colors = new();

        private CancellationTokenSource cts;

        async public void Run()
        {
            cts = new CancellationTokenSource();
            CancellationToken ct = cts.Token;

            string colorStr = $"<color=#{colors[0].ToHexString()}>";
            // Debug.Log($"{colorStr}------ TaskDelaySampleAsync(0) with await ------</color>");
            // // with await, the method will wait for the async method to complete before continuing
            // Debug.Log($"{colorStr}Before TaskDelaySampleAsync(0) called, time={Time.realtimeSinceStartup}</color>");
            // await TaskDelaySampleAsync(3000, 0, colorStr, ct);
            // Debug.Log($"{colorStr}After await TaskDelaySampleAsync(0) called, time={Time.realtimeSinceStartup}</color>");

            colorStr = $"<color=#{colors[1].ToHexString()}>";
            Debug.Log($"{colorStr}------ VoidTaskDelaySampleAsync(1) without await ------</color>");
            // without await (fire and forget), the method will return immediately but runs async, flow will continue
            Debug.Log($"{colorStr}Before TaskDelaySampleAsync(1) called, time={Time.realtimeSinceStartup}</color>");
            TaskDelaySampleAsync(3000, 1, colorStr, ct);
            Debug.Log($"{colorStr}After TaskDelaySampleAsync(1) called, time={Time.realtimeSinceStartup}</color>");

            colorStr = $"<color=#{colors[2].ToHexString()}>";
            // Debug.Log($"{colorStr}------ VoidTaskDelaySampleAsync(2) without await ------</color>");
            // // void return type, without await (fire and forget), the method will return immediately but runs async, flow will continue
            // // the compiled warning is gone
            // VoidTaskDelaySampleAsync(3000, 2, colorStr, ct);
        }

        public void Cancel()
        {
            Debug.Log("Cancelling tasks...");
            cts.Cancel();
            cts.Dispose();
            cts = null;
        }

        public async Task TaskDelaySampleAsync(int delayMS,  int index, string colorStr, CancellationToken ct)
        {
            float startTime = Time.realtimeSinceStartup;
            Debug.Log($"{colorStr}Task #{index} started at: {startTime}</color>");

            await Task.Delay(delayMS, cancellationToken: ct);

            float endTime = Time.realtimeSinceStartup;
            Debug.Log($"{colorStr}Task #{index} finished at: {endTime}, duration: {endTime - startTime} seconds.</color>");
        }

         public async void VoidTaskDelaySampleAsync(int delayMS,  int index, string colorStr, CancellationToken ct)
        {
            float startTime = Time.realtimeSinceStartup;
            Debug.Log($"{colorStr}Task #{index} started at: {startTime}</color>");

            await Task.Delay(delayMS, cancellationToken: ct);

            float endTime = Time.realtimeSinceStartup;
            Debug.Log($"{colorStr}Task #{index} finished at: {endTime}, duration: {endTime - startTime} seconds.</color>");
        }
    }
}

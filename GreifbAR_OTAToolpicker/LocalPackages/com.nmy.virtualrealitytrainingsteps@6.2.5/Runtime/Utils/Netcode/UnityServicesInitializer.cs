#if NMY_ENABLE_VIVOX

using Cysharp.Threading.Tasks;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Core.Environments;
using UnityEngine;

namespace NMY.VirtualRealityTraining
{
    public class UnityServicesInitializer : MonoBehaviourSingleton<UnityServicesInitializer>
    {
        [SerializeField] private string _environment = "production";

        public bool isSigningIn { get; private set; }
        public bool isSignedIn  { get; private set; }

        public async UniTaskVoid SignInIfNeeded()
        {
            if (isSigningIn)
            {
                await UniTask.WaitUntil(() => isSigningIn == false);
                if (isSignedIn) return;
            }

            isSigningIn = true;
            var options = new InitializationOptions().SetEnvironmentName(_environment);

            await UnityServices.InitializeAsync(options);

            if (!AuthenticationService.Instance.IsSignedIn)
            {
                await AuthenticationService.Instance.SignInAnonymouslyAsync();
                isSignedIn = true;
            }

            isSigningIn = false;
        }
    }
}
#endif

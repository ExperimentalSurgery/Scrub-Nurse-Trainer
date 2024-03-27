using UnityEngine;
using Cysharp.Threading.Tasks;
using System;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Unity.XR.CoreUtils;
using UnityEngine.Android;

#if NMY_ENABLE_VIVOX
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Vivox;
using Channel = Unity.Services.Vivox.Channel;
using VivoxUnity;
#endif

namespace NMY.VirtualRealityTraining
{
    /// <summary>
    /// This class is responsible for handling the interaction with Vivox, the voice over IP solution for unity.
    /// It does not support Linux.
    /// </summary>
    public partial class VivoxManager : MonoBehaviour
    {
#if NMY_ENABLE_VIVOX

#region Enums

        /// <summary>
        /// An enumeration representing the capability of a vivox channel.
        /// </summary>
        public enum ChatCapability
        {
            TextOnly,
            AudioOnly,
            TextAndAudio
        }

#endregion

#region Member Variables

        /// <summary>
        /// A reference to the vivox account of a user.
        /// </summary>
        private static Account _account;

        /// <summary>
        /// A CancellationTokenSource used to cancel the positional update task.
        /// </summary>
        private static CancellationTokenSource _positionalAudioCts = new();
        
        /// <summary>
        /// The amount of seconds for updating the position of the local player to vivox.
        /// </summary>
        private static float _positionUpdateFrequency = 0.3f;

#endregion

#region Properties
        
        private static Client client => VivoxService.Instance.Client;

        public static LoginState    loginState   { get; private set; }
        public static ILoginSession loginSession { get; private set; }

        public static IReadOnlyDictionary<ChannelId, IChannelSession> activeChannels =>
            loginSession?.ChannelSessions;

        public static IAudioDevices audioInputDevices  => client.AudioInputDevices;
        public static IAudioDevices audioOutputDevices => client.AudioOutputDevices;

        private static int audioInputDevicesVolume  => audioInputDevices.VolumeAdjustment;
        private static int audioOutputDevicesVolume => audioOutputDevices.VolumeAdjustment;


        /// <summary>
        /// Retrieves the first instance of a session that is transmitting. 
        /// </summary>
        public static IChannelSession transmittingSession
        {
            get
            {
                if (client == null)
                    throw new NullReferenceException("client");
                return client.GetLoginSession(_account).ChannelSessions.FirstOrDefault(x => x.IsTransmitting);
            }
            set
            {
                if (value != null)
                {
                    client.GetLoginSession(_account).SetTransmissionMode(TransmissionMode.Single, value.Channel);
                }
            }
        }

#endregion

        private void Awake()
        {
            var managers = FindObjectsOfType<VivoxManager>();

            if (managers.Length > 1 && managers.Contains(GetComponent<VivoxManager>()))
                Destroy(gameObject);
        }

        private static async UniTask Initialize()
        {
            await UnityServices.InitializeAsync();

            try
            {
                await AuthenticationService.Instance.SignInAnonymouslyAsync();
                Debug.Log($"{typeof(VivoxManager)}: Sign in anonymously succeeded!");

                // Shows how to get the playerID
                Debug.Log($"{typeof(VivoxManager)}: PlayerID: {AuthenticationService.Instance.PlayerId}");
            }
            // catch (AuthenticationException ex)
            // {
            //     // Compare error code to AuthenticationErrorCodes
            //     // Notify the player with the proper error message
            //     Debug.LogException(ex);
            // }
            catch (RequestFailedException ex)
            {
                // Compare error code to CommonErrorCodes
                // Notify the player with the proper error message
                Debug.LogException(ex);
            }

            VivoxService.Instance.Initialize();
        }

        private void OnApplicationQuit()
        {
#if !UNITY_SERVER

            // Needed to add this to prevent some unsuccessful uninit, we can revisit to do better -carlo
            Client.Cleanup();
            if (client == null) return;

            Debug.Log($"{GetType()}: Uninitializing client.", this);
            client.Uninitialize();
#endif
        }

        public static async UniTask LoginAsync(string displayName = null,
                                          LoginStatusChangedHandler onLoggedInCallback = null)
        {
#if !UNITY_SERVER
            
            await Initialize();

            await RequestMicrophonePermissionAsync();

            Login(displayName, onLoggedInCallback);

            await UniTask.WaitUntil(() => loginSession.State == LoginState.LoggedIn);
#endif
        }

        private static void Login(string displayName, LoginStatusChangedHandler onLoggedInCallback = null)
        {
#if !UNITY_SERVER

            // We are already logged in; dont log in again, call only the callback
            if (loginSession is { State: LoginState.LoggingIn or LoginState.LoggedIn })
            {
                onLoggedInCallback?.Invoke();
                return;
            }
            
            _account = new Account(displayName);

            loginSession                 =  client.GetLoginSession(_account);
            loginSession.PropertyChanged += OnLoginSessionPropertyChanged;
            loginSession.BeginLogin(loginSession.GetLoginToken(), SubscriptionMode.Accept, null, null, null, ar =>
            {
                try
                {
                    loginSession.EndLogin(ar);

                    // OnUserLoggedInEvent -= onLoggedInCallback;
                    OnUserLoggedInEvent += onLoggedInCallback;
                }
                catch (Exception e)
                {
                    // Handle error 
                    Debug.LogError($"{nameof(e)}: {e.Message}");
                    // Unbind if we failed to login.
                    loginSession.PropertyChanged -= OnLoginSessionPropertyChanged;
                }
            });
            
#endif
        }
        

        public static async UniTask LogoutAsync()
        {
#if !UNITY_SERVER
            
            Logout();
            await UniTask.WaitUntil(() => loginSession.State == LoginState.LoggedOut);
            
#endif
        }
        
        private static void Logout()
        {
#if !UNITY_SERVER

            if (loginSession == null || loginState is LoginState.LoggedOut or LoginState.LoggingOut) return;

            OnUserLoggedOutEvent?.Invoke();
            loginSession.PropertyChanged -= OnLoginSessionPropertyChanged;
            loginSession.Logout();

#endif
        }

        public static void JoinChannel(string channelName, ChannelType channelType, ChatCapability chatCapability,
                                       bool transmissionSwitch = true, Channel3DProperties properties = null, 
                                       float positionUpdateFrequency = 0.3f)
        {
#if !UNITY_SERVER

            _positionUpdateFrequency = positionUpdateFrequency;

            if (loginState == LoginState.LoggedIn)
            {
                var channel = new Channel(channelName, channelType, properties);

                var channelSession = loginSession.GetChannelSession(channel);

                channelSession.PropertyChanged                += OnChannelPropertyChanged;
                channelSession.Participants.AfterKeyAdded     += OnParticipantAdded;
                channelSession.Participants.BeforeKeyRemoved  += OnParticipantRemoved;
                channelSession.Participants.AfterValueUpdated += OnParticipantValueUpdated;
                channelSession.MessageLog.AfterItemAdded      += OnMessageLogReceived;

                channelSession.BeginConnect(chatCapability != ChatCapability.TextOnly,
                                            chatCapability != ChatCapability.AudioOnly, transmissionSwitch,
                                            channelSession.GetConnectToken(), ar =>
                                            {
                                                try
                                                {
                                                    channelSession.EndConnect(ar);
                                                }
                                                catch (Exception e)
                                                {
                                                    // Handle error 
                                                    Debug.LogError($"{typeof(VivoxManager)}: Could not connect to voice channel: {e.Message}");
                                                }
                                            });
            }
            else
            {
                Debug.LogError($"{typeof(VivoxManager)}: Cannot join a channel when not logged in.");
            }

#endif
        }

        public void SendTextMessage(string messageToSend, ChannelId channel, string applicationStanzaNamespace = null,
                                    string applicationStanzaBody = null)
        {
#if !UNITY_SERVER

            if (ChannelId.IsNullOrEmpty(channel))
            {
                throw new ArgumentException($"{typeof(VivoxManager)}: Must provide a valid ChannelId");
            }

            if (string.IsNullOrEmpty(messageToSend))
            {
                throw new ArgumentException($"{typeof(VivoxManager)}: Must provide a message to send");
            }

            var channelSession = loginSession.GetChannelSession(channel);

            channelSession.BeginSendText(null, messageToSend, applicationStanzaNamespace, applicationStanzaBody, ar =>
            {
                try
                {
                    channelSession.EndSendText(ar);
                }
                catch (Exception e)
                {
                    Debug.Log($"{typeof(VivoxManager)}: SendTextMessage failed with exception {e.Message}", this);
                }
            });

#endif
        }

        public static void DisconnectAllChannels()
        {
#if !UNITY_SERVER

            if (!(activeChannels?.Count > 0)) return;

            foreach (var channelSession in activeChannels)
            {
                channelSession?.Disconnect();
            }

#endif
        }

#region Vivox Callbacks

        private static void OnMessageLogReceived(object sender,
                                                 QueueItemAddedEventArgs<IChannelTextMessage> textMessage)
        {
            ValidateArgs(new[] { sender, textMessage });

            var channelTextMessage = textMessage.Value;
            Debug.Log(channelTextMessage.Message);
            OnTextMessageLogReceivedEvent?.Invoke(channelTextMessage.Sender.DisplayName, channelTextMessage);
        }

        private static void OnLoginSessionPropertyChanged(object sender,
                                                          PropertyChangedEventArgs propertyChangedEventArgs)
        {
            if (propertyChangedEventArgs.PropertyName != "State")
            {
                return;
            }

            loginSession = (ILoginSession)sender;
            loginState   = loginSession.State;
            Debug.Log($"{typeof(VivoxManager)}: Detecting login session change");
            switch (loginState)
            {
                case LoginState.LoggingIn:
                {
                    Debug.Log($"{typeof(VivoxManager)}: Logging in");
                    break;
                }
                case LoginState.LoggedIn:
                {
                    Debug.Log($"{typeof(VivoxManager)}: Connected to voice server and logged in.");
                    OnUserLoggedInEvent?.Invoke();
                    break;
                }
                case LoginState.LoggingOut:
                {
                    Debug.Log($"{typeof(VivoxManager)}: Logging out");
                    break;
                }
                case LoginState.LoggedOut:
                {
                    Debug.Log($"{typeof(VivoxManager)}: Logged out");
                    loginSession.PropertyChanged -= OnLoginSessionPropertyChanged;
                    break;
                }
                default:
                    break;
            }
        }

        private static void OnParticipantAdded(object sender, KeyEventArg<string> keyEventArg)
        {
            ValidateArgs(new[] { sender, keyEventArg });

            // INFO: sender is the dictionary that changed and trigger the event.  Need to cast it back to access it.
            var source = (IReadOnlyDictionary<string, IParticipant>)sender;
            // Look up the participant via the key.
            var participant    = source[keyEventArg.Key];
            var username       = participant.Account.DisplayName;
            var channel        = participant.ParentChannelSession.Key;
            var channelSession = participant.ParentChannelSession;

            if (channel.Type == ChannelType.Positional && participant.IsSelf)
            {
                UpdatePositionalAudioAsync(channelSession, _positionUpdateFrequency, _positionalAudioCts.Token).Forget();
#if UNITY_ANDROID
            //TODO: Does not work for the Quest 2 .. ask support for help 
            // ModifyAndroidAudioSettings(0);
#endif
            }

            // Trigger callback
            OnParticipantAddedEvent?.Invoke(username, channel, participant);
        }

        private static void OnParticipantRemoved(object sender, KeyEventArg<string> keyEventArg)
        {
            ValidateArgs(new[] { sender, keyEventArg });

            // INFO: sender is the dictionary that changed and trigger the event.  Need to cast it back to access it.
            var source = (IReadOnlyDictionary<string, IParticipant>)sender;
            // Look up the participant via the key.
            var participant    = source[keyEventArg.Key];
            var username       = participant.Account.Name;
            var channel        = participant.ParentChannelSession.Key;
            var channelSession = participant.ParentChannelSession;

            if (participant.IsSelf)
            {
                Debug.Log($"{typeof(VivoxManager)}: Unsubscribing from: {channelSession.Key.Name}");
                // Now that we are disconnected, unsubscribe.
                channelSession.PropertyChanged                -= OnChannelPropertyChanged;
                channelSession.Participants.AfterKeyAdded     -= OnParticipantAdded;
                channelSession.Participants.BeforeKeyRemoved  -= OnParticipantRemoved;
                channelSession.Participants.AfterValueUpdated -= OnParticipantValueUpdated;
                channelSession.MessageLog.AfterItemAdded      -= OnMessageLogReceived;

                if (channel.Type == ChannelType.Positional)
                {
                    _positionalAudioCts.Cancel();
                }

                // Remove session.
                var user = client.GetLoginSession(_account);
                user.DeleteChannelSession(channelSession.Channel);
            }

            // Trigger callback
            OnParticipantRemovedEvent?.Invoke(username, channel, participant);
        }

        private static void ValidateArgs(object[] objs)
        {
            foreach (var obj in objs)
            {
                if (obj == null)
                    throw new ArgumentNullException(obj.GetType().ToString(), "Specify a non-null/non-empty argument.");
            }
        }

        private static void OnParticipantValueUpdated(object sender, ValueEventArg<string, IParticipant> valueEventArg)
        {
            ValidateArgs(new[] { sender, valueEventArg });

            var source = (IReadOnlyDictionary<string, IParticipant>)sender;
            // Look up the participant via the key.
            var participant = source[valueEventArg.Key];

            var username = valueEventArg.Value.Account.Name;
            var channel  = valueEventArg.Value.ParentChannelSession.Key;
            var property = valueEventArg.PropertyName;

            switch (property)
            {
                case "SpeechDetected":
                {
                    // Debug.Log($"OnSpeechDetectedEvent: {username} in {channel}.", this);
                    OnSpeechDetectedEvent?.Invoke(username, channel, valueEventArg.Value.SpeechDetected);
                    break;
                }
                case "AudioEnergy":
                {
                    OnAudioEnergyChangedEvent?.Invoke(username, channel, valueEventArg.Value.AudioEnergy);
                    break;
                }
                default:
                    break;
            }
        }

        private static void OnChannelPropertyChanged(object sender, PropertyChangedEventArgs propertyChangedEventArgs)
        {
            ValidateArgs(new[] { sender, propertyChangedEventArgs });

            //if (_client == null)
            //    throw new InvalidClient("Invalid client.");
            var channelSession = (IChannelSession)sender;

            // IF the channel has removed audio, make sure all the VAD indicators aren't showing speaking.
            if (propertyChangedEventArgs.PropertyName == "AudioState" &&
                channelSession.AudioState == ConnectionState.Disconnected)
            {
                Debug.Log($"{typeof(VivoxManager)}: Audio disconnected from: {channelSession.Key.Name}");

                foreach (var participant in channelSession.Participants)
                {
                    OnSpeechDetectedEvent?.Invoke(participant.Account.Name, channelSession.Channel, false);
                }
            }

            // IF the channel has fully disconnected, unsubscribe and remove.
            if (propertyChangedEventArgs.PropertyName is "AudioState" or "TextState" &&
                channelSession.AudioState == ConnectionState.Disconnected &&
                channelSession.TextState == ConnectionState.Disconnected)
            {
                Debug.Log($"{typeof(VivoxManager)}: Unsubscribing from: {channelSession.Key.Name}");
                // Now that we are disconnected, unsubscribe.
                channelSession.PropertyChanged                -= OnChannelPropertyChanged;
                channelSession.Participants.AfterKeyAdded     -= OnParticipantAdded;
                channelSession.Participants.BeforeKeyRemoved  -= OnParticipantRemoved;
                channelSession.Participants.AfterValueUpdated -= OnParticipantValueUpdated;
                channelSession.MessageLog.AfterItemAdded      -= OnMessageLogReceived;

                // Remove session.
                var user = client.GetLoginSession(_account);
                user.DeleteChannelSession(channelSession.Channel);
            }
        }

#endregion

#region Utilities

        // https://docs.vivox.com/v5/general/unity/15_1_160000/en-us/Default.htm#Unity/developer-guide/channels/positional-channel-configuration.htm%3FTocPath%3DVivox%2520Unity%2520SDK%2520documentation%7CVivox%2520Unity%2520Developer%2520Guide%7CChannels%7CPositional%2520channels%7C_____2
        private static async UniTaskVoid UpdatePositionalAudioAsync(IChannelSession channelSession,
                                                                    float positionUpdateFrequency, CancellationToken ct)
        {
            if (channelSession.AudioState != ConnectionState.Connected)
                await UniTask.WaitUntil(() => channelSession.AudioState == ConnectionState.Connected,
                                        cancellationToken: ct);

            Debug.Log($"{typeof(VivoxManager)}: Start 3D positional update");

            var headTransform = FindObjectOfType<XROrigin>()?.Camera.transform;
            while (channelSession.AudioState == ConnectionState.Connected)
            {
                if (headTransform == null)
                {
                    headTransform = FindObjectOfType<XROrigin>()?.Camera.transform;
                    if (headTransform == null)
                    {
                        await UniTask.NextFrame(ct);
                        continue;
                    }
                }

                var speakerPos      = headTransform.position;
                var listenerPos     = speakerPos;
                var listenerForward = headTransform.forward;
                var listenerUp      = headTransform.up;

                channelSession.Set3DPosition(speakerPos, listenerPos, listenerForward, listenerUp);

                await UniTask.Delay(TimeSpan.FromSeconds(positionUpdateFrequency), cancellationToken: ct);
            }

            Debug.Log($"{typeof(VivoxManager)}: Stop 3D positional update");
        }

        private static async UniTask<bool> RequestMicrophonePermissionAsync()
        {
            if (Permission.HasUserAuthorizedPermission(Permission.Microphone)) return true;

            Debug.Log($"{typeof(VivoxManager)}: User has not granted microphone permission. Will request.");
            Permission.RequestUserPermission(Permission.Microphone);

            var startTime = Time.realtimeSinceStartup;
            while (true)
            {
                // Break from the loop if we have permission.
                if (Permission.HasUserAuthorizedPermission(Permission.Microphone))
                {
                    Debug.Log($"{typeof(VivoxManager)}: User has granted microphone permissions!");
                    return true;
                }

                // Bail if too much time has passed.
                var elapsed = Time.realtimeSinceStartup - startTime;
                if (elapsed > 20.0f)
                {
                    Debug.LogError($"{typeof(VivoxManager)}: User has not granted microphone permissions. Unable to start microphone device.");
                    return false;
                }

                await UniTask.NextFrame();
            }
        }

        /// <summary>
        /// Through Vivox, Android sets the AudioManager Mode to MODE_IN_COMMUNICATION. However, in OfB Quests,
        /// the volume buttons are not responsive.<para />
        ///
        /// Problem Description: https://support.unity.com/hc/en-us/articles/4418149427604-How-to-Adjust-Android-media-volume-while-in-channel<para />
        /// Source: https://forum.unity.com/threads/help-please-recording-mode-earpiece-mode-and-not-speaker-mode.462945/<para />
        /// Android Documentation: https://developer.android.com/reference/android/media/AudioManager#getMode()<para />
        /// </summary>
        /// <param name="newMode">The new audio mode for Android.<br />
        /// 0 = MODE_NORMAL<br />
        /// 1 = MODE_RINGTONE<br />
        /// 2 = MODE_IN_CALL<br />
        /// 3 = MODE_IN_COMMUNICATION<br />
        /// 4 = MODE_CALL_SCREENING<br />
        /// 5 = MODE_CALL_REDIRECT<br />
        /// 6 = MODE_COMMUNICATION_REDIRECT<br />
        /// </param>
        private void ModifyAndroidAudioSettings(int newMode)
        {
            try
            {
                var unityPlayer  = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
                var activity     = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
                var audioManager = activity.Call<AndroidJavaObject>("getSystemService", "audio");

                var currentMode = audioManager.Call<int>("getMode");
                audioManager.Call("setMode", newMode);
                var checkMode = audioManager.Call<int>("getMode");

                // Debug.Log($"Change audio mode from '{currentMode}' to '{checkMode}'", this);
            }
            catch (Exception ex)
            {
                Debug.LogError(ex.Message, this);
            }
        }

        // https://docs.vivox.com/v5/general/unity/15_1_160000/en-us/Default.htm#Unity/developer-guide/muting/mute-user-microphone.htm?Highlight=AudioInputDevices%20
        [ContextMenu("Mute Local")]
        public static void MuteLocal() => audioInputDevices.Muted = true;

        [ContextMenu("Unmute Local")]
        public static void UnmuteLocal() => audioInputDevices.Muted = false;

        public static void IncreaseLocalInputVolume(int amount)
        {
            audioInputDevices.BeginRefresh(_ =>
            {
                var volume    = audioInputDevices.VolumeAdjustment;
                var newVolume = Mathf.Clamp(volume + amount, -10, 25);
                audioInputDevices.VolumeAdjustment = newVolume;
                Debug.Log($"{typeof(VivoxManager)}: Increase local input volume to {newVolume}.");
            });
        }

        public static void DecreaseLocalInputVolume(int amount)
        {
            audioInputDevices.BeginRefresh(_ =>
            {
                var volume    = audioInputDevices.VolumeAdjustment;
                var newVolume = Mathf.Clamp(volume - amount, -10, 25);
                audioInputDevices.VolumeAdjustment = newVolume;
                Debug.Log($"{typeof(VivoxManager)}: Decrease local input volume to {newVolume}.");
            });
        }

        public static void IncreaseLocalOutputVolume(int amount)
        {
            audioOutputDevices.BeginRefresh(_ =>
            {
                var volume    = audioOutputDevices.VolumeAdjustment;
                var newVolume = Mathf.Clamp(volume + amount, -10, 25);
                audioOutputDevices.VolumeAdjustment = newVolume;
                Debug.Log($"{typeof(VivoxManager)}: Increase local output volume to {newVolume}.");
            });
        }

        public static void DecreaseLocalOutputVolume(int amount)
        {
            audioOutputDevices.BeginRefresh(_ =>
            {
                var volume    = audioOutputDevices.VolumeAdjustment;
                var newVolume = Mathf.Clamp(volume - amount, -10, 25);
                audioOutputDevices.VolumeAdjustment = newVolume;
                Debug.Log($"{typeof(VivoxManager)}: Increase local output volume to {newVolume}.");
            });
        }

#endregion


#region Context Menu

        [ContextMenu("Print Info")]
        private void PrintInfo()
        {
            Debug.Log($"{typeof(VivoxManager)}: Input Device: {audioInputDevices.ActiveDevice.Name} ", this);
            Debug.Log($"{typeof(VivoxManager)}: Output Device: {audioOutputDevices.ActiveDevice.Name}", this);
            Debug.Log($"{typeof(VivoxManager)}: Input volume: {audioInputDevicesVolume} dB", this);
            Debug.Log($"{typeof(VivoxManager)}: Output volume: {audioOutputDevicesVolume} dB", this);

            foreach (var channelSession in activeChannels)
            {
                Debug.Log($"{typeof(VivoxManager)}: Channel name: {channelSession.Channel.Name}", this);
                foreach (var participant in channelSession.Participants)
                {
                    Debug.Log(
                        $"{typeof(VivoxManager)}: DisplayName: '{participant.Account.DisplayName}'; " +
                        $"ID: '{participant.ParticipantId}'"
                        // $"Volume: {participant.LocalVolumeAdjustment}; "
                        , this);
                }

                Debug.Log("===============================================", this);
            }
        }


        [ContextMenu("Increase Local Input Volume")]
        private void IncreaseLocalInputVolume_CONTEXT_MENU() => IncreaseLocalInputVolume(5);

        [ContextMenu("Decrease Local Input Volume")]
        private void DecreaseLocalInputVolume_CONTEXT_MENU() => DecreaseLocalInputVolume(5);

        [ContextMenu("Increase Local Output Volume")]
        private void IncreaseLocalOutputVolume_CONTEXT_MENU() => IncreaseLocalOutputVolume(5);

        [ContextMenu("Decrease Local Output Volume")]
        private void DecreaseLocalOutputVolume_CONTEXT_MENU() => DecreaseLocalOutputVolume(5);

#endregion

#endif
    }
}
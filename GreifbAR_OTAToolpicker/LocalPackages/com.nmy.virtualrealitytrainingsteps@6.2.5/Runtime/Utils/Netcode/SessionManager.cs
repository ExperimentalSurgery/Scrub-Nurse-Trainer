using System;
using System.Linq;
using System.Text;
using Cysharp.Threading.Tasks;
using JetBrains.Annotations;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
#if NMY_ENABLE_VIVOX
using NMY.VirtualRealityTraining.NetworkAvatars;
using VivoxUnity;
#endif

namespace NMY.VirtualRealityTraining
{
    /// <summary>
    /// This class is responsible for handling the network session and connecting the application to the network.
    /// </summary>
    public class SessionManager : MonoBehaviourSingleton<SessionManager>
    {
#if NMY_ENABLE_VIVOX

        /// <summary>
        /// If Vivox is installed, enable voice over IP if true.
        /// </summary>
        [Header("Vivox Parameter")]
        [SerializeField] private bool _enableVoip;

        /// <summary>
        /// The name of the vivox channel to connect to.
        /// </summary>
        [SerializeField] private string _vivoxChannelName = "NMY Unfold Vivox";
        
        /// <summary>
        /// The amount of seconds for updating the position of the local player to vivox.
        /// </summary>
        [SerializeField] private float  _positionalUpdateFrequency = 0.3f;
        
#endif // NMY_ENABLE_VIVOX

        /// <summary>
        /// A unity event that is called when the login was successful.
        /// </summary>
        [Header("Events")]
        public UnityEvent onLoginSuccessful = new();

        /// <summary>
        /// A unity event that is called when the login failed.
        /// </summary>
        public UnityEvent onLoginFailed = new();

        /// <summary>
        /// A reference to the currently loaded scene.
        /// </summary>
        private Scene _loadedScene;

        private UnityTransport unityTransport =>
            (UnityTransport)NetworkManager.Singleton.NetworkConfig.NetworkTransport;

        // private async void Start()
        // {
        //     await UnityServicesInitializer.instance.SignInIfNeeded();
        // }

#region Login / Logout
        
        
        /// <summary>
        /// Starts a client.
        /// </summary>
        /// <param name="serverIP">Optional: The server IP to connect to. If <c>null</c>, use the default value.</param>
        /// <param name="serverPort">Optional: The server port to connect to. If <c>null</c>, use the default value.</param>
        /// <param name="onClientConnectedCallback">A callback when the client connected to the network.</param>
        public async UniTask LoginAsClient([CanBeNull] string serverIP = null, ushort? serverPort = null,
                                           Action<ulong> onClientConnectedCallback = null)
        {
            if (serverIP != null)
            {
                unityTransport.ConnectionData.Address             = serverIP;
                unityTransport.ConnectionData.ServerListenAddress = "0.0.0.0";
            }

            if (serverPort.HasValue) unityTransport.ConnectionData.Port = serverPort.Value;

            if (onClientConnectedCallback != null)
            {
                NetworkManager.Singleton.OnClientConnectedCallback -= onClientConnectedCallback;
                NetworkManager.Singleton.OnClientConnectedCallback += onClientConnectedCallback;
            }

            // start the client
            Debug.Log($"{GetType()}: Starting Client", this);
            var success = NetworkManager.Singleton.StartClient();

            if (success)
            {
                Debug.Log($"{GetType()}: Start Client succeeded at Server IP " +
                          $"{unityTransport.ConnectionData.Address} on port " +
                          $"{unityTransport.ConnectionData.Port}", this);
                NetworkManager.Singleton.SceneManager.OnSceneEvent += OnSceneEvent;

                await UniTask.WaitUntil(() => NetworkManager.Singleton.IsConnectedClient);
                onLoginSuccessful?.Invoke();

#if NMY_ENABLE_VIVOX
                if (_enableVoip)
                    // After successful login of the user, it gets connected to the corresponding vivox channel
                    await LoginToVivoxAsync();
#endif
            }
            else
            {
                onLoginFailed?.Invoke();
                Debug.Log($"{GetType()}: Start Client failed at Server IP " +
                          $"{unityTransport.ConnectionData.Address} on port " +
                          $"{unityTransport.ConnectionData.Port}", this);
            }
        }

        /// <summary>
        /// Starts a server.
        /// </summary>
        /// <param name="serverIP">Optional: The server IP if this server. If <c>null</c>, use the default value.</param>
        /// <param name="serverPort">Optional: The server port to listen to clients. If <c>null</c>, use the default value.</param>
        public async UniTask LoginAsServer([CanBeNull] string serverIP = null, ushort? serverPort = null)
        {
            if (serverIP != null)
            {
                unityTransport.ConnectionData.Address             = serverIP;
                unityTransport.ConnectionData.ServerListenAddress = "0.0.0.0";
            }

            if (serverPort.HasValue) unityTransport.ConnectionData.Port = serverPort.Value;

            // start the server
            var success = NetworkManager.Singleton.StartServer();

            if (success)
            {
                NetworkManager.Singleton.SceneManager.OnSceneEvent += OnSceneEvent;
                Debug.Log($"{GetType()}: Server started at IP " +
                          $"{unityTransport.ConnectionData.Address} on port " +
                          $"{unityTransport.ConnectionData.Port}", this);

                await new WaitUntil(() => NetworkManager.Singleton.IsListening);
                onLoginSuccessful?.Invoke();

                //TODO: check periodically the number of players, if no player is connected, reset the instance
            }
            else
            {
                onLoginFailed?.Invoke();
                Debug.LogError($"{GetType()}: Start Server failed at IP " +
                          $"{unityTransport.ConnectionData.Address} on port " +
                          $"{unityTransport.ConnectionData.Port}", this);
            }
        }

        /// <summary>
        /// Starts a client.
        /// </summary>
        /// <param name="serverIP">Optional: The server IP to connect to. If <c>null</c>, use the default value.</param>
        /// <param name="serverPort">Optional: The server port to connect to. If <c>null</c>, use the default value.</param>
        public async UniTask LoginAsHost([CanBeNull] string serverIP = null, ushort? serverPort = null)
        {
            if (serverIP != null)
            {
                unityTransport.ConnectionData.Address             = serverIP;
                unityTransport.ConnectionData.ServerListenAddress = serverIP;
            }

            if (serverPort.HasValue) unityTransport.ConnectionData.Port = serverPort.Value;

            // start the host
            Debug.Log($"{GetType()}: Starting Host", this);
            var success = NetworkManager.Singleton.StartHost();

            if (success)
            {
                NetworkManager.Singleton.SceneManager.OnSceneEvent += OnSceneEvent;
                Debug.Log($"{GetType()}: Host started at IP " +
                          $"{unityTransport.ConnectionData.Address} on port " +
                          $"{unityTransport.ConnectionData.Port}", this);

                
                await new WaitUntil(() => NetworkManager.Singleton.IsListening);
                onLoginSuccessful?.Invoke();

#if NMY_ENABLE_VIVOX
                if (_enableVoip)
                    // After successful login of the user, it gets connected to the corresponding vivox channel
                    await LoginToVivoxAsync();
#endif
            }
            else
            {
                onLoginFailed?.Invoke();
                Debug.Log($"{GetType()}: Start Host failed!", this);
            }
        }


        /// <summary>
        /// Globally shuts down the network. Disconnects clients if connected and stops server if running
        /// </summary>
        /// <param name="onClientDisconnectedCallback">A callback when the client disconnected from the network.</param>
        public async UniTask Logout(Action<ulong> onClientDisconnectedCallback = null)
        {
            if (onClientDisconnectedCallback != null)
            {
                NetworkManager.Singleton.OnClientDisconnectCallback -= onClientDisconnectedCallback;
                NetworkManager.Singleton.OnClientDisconnectCallback += onClientDisconnectedCallback;
            }

#if NMY_ENABLE_VIVOX
            if (_enableVoip)
            {
                VivoxManager.DisconnectAllChannels();
                await VivoxManager.LogoutAsync();
            }
#else
        await UniTask.CompletedTask;
#endif

            // Close all network connections from host and client 
            NetworkManager.Singleton.Shutdown();

            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }

#endregion

#region VIVOX

#if NMY_ENABLE_VIVOX

        /// <summary>
        /// Log into vivox voice over IP.
        /// </summary>
        private async UniTask LoginToVivoxAsync()
        {
            await UniTask.WaitUntil(() => NetworkAvatarManager.localAvatar != null);
            var displayName = $"{NetworkManager.Singleton.LocalClientId}";

            Debug.Log($"{GetType()}: Vivox Login with username: '{displayName}'", this);
            await VivoxManager.LoginAsync(displayName, OnUserLoggedInVivox);
        }
        
        /// <summary>
        /// The event handler for the <see cref="VivoxManager.LoginAsync"/> method.
        /// Handles the joining of the vivox voice channel after the user was logged in.
        /// </summary>
        private void OnUserLoggedInVivox()
        {
            if (string.IsNullOrEmpty(_vivoxChannelName))
            {
                Debug.LogError($"{GetType()}: Cannot join channel, channel name is empty!", this);
                return;
            }

            // Some sanity checks for vivox channel names
            var channelName = _vivoxChannelName;
            if (channelName.Length > 200)
                channelName = _vivoxChannelName[..200];
            if (channelName.Contains(" "))
                channelName = channelName.Replace(" ", "_");
            
            Debug.Log($"{GetType()}: Vivox logged in. Join channel: {channelName}", this);

            var lobbyChannel = VivoxManager.activeChannels.FirstOrDefault(ac => ac.Channel.Name == channelName);
            if (VivoxManager.activeChannels.Count == 0 || lobbyChannel == null)
            {
                VivoxManager.JoinChannel(channelName, ChannelType.Positional, VivoxManager.ChatCapability.AudioOnly,
                                         positionUpdateFrequency: _positionalUpdateFrequency);
            }
            else
            {
                if (lobbyChannel.AudioState == ConnectionState.Disconnected)
                {
                    // Ask for hosts since we're already in the channel and part added won't be triggered.

                    lobbyChannel.BeginSetAudioConnected(
                        true, true, _ => { Debug.Log($"{GetType()}: Now transmitting into channel: {channelName}", this); });
                }
            }
        }

#endif

#endregion

#region Scene Management

        /// <summary>
        /// Handles processing notifications when subscribed to OnSceneEvent
        /// </summary>
        /// <param name="sceneEvent">class that contains information about the scene event</param>
        private void OnSceneEvent(SceneEvent sceneEvent)
        {
            var clientOrServer = sceneEvent.ClientId == NetworkManager.ServerClientId ? "server" : "client";

            // Both client and server receive these notifications
            switch (sceneEvent.SceneEventType)
            {
                // Handle server to client Load Notifications
                case SceneEventType.Load:
                {
                    // This event provides you with the associated AsyncOperation
                    // AsyncOperation.progress can be used to determine scene loading progression
                    var asyncOperation = sceneEvent.AsyncOperation;
                    // Since the server "initiates" the event we can simply just check if we are the server here
                    if (NetworkManager.Singleton.IsServer)
                    {
                        // Handle server side load event related tasks here
                    }
                    else
                    {
                        // Handle client side load event related tasks here
                    }

                    break;
                }
                // Handle server to client unload notifications
                case SceneEventType.Unload:
                {
                    // You can use the same pattern above under SceneEventType.Load here
                    break;
                }
                // Handle client to server LoadComplete notifications
                case SceneEventType.LoadComplete:
                {
                    // This will let you know when a load is completed
                    // Server Side: receives this notification for both itself and all clients
                    if (NetworkManager.Singleton.IsServer)
                    {
                        if (sceneEvent.ClientId == NetworkManager.Singleton.LocalClientId)
                        {
                            // Handle server side LoadComplete related tasks here

                            // *** IMPORTANT ***
                            // Keep track of the loaded scene, you need this to unload it
                            _loadedScene = sceneEvent.Scene;
                        }
                        else
                        {
                            // Handle client LoadComplete **server-side** notifications here
                        }


                        Debug.Log($"{GetType()}: Loaded the {sceneEvent.SceneName} scene on " +
                                  $"{clientOrServer}-({sceneEvent.ClientId}).", this);
                    }
                    else // Clients generate this notification locally
                    {
                        // Handle client side LoadComplete related tasks here
                    }

                    // So you can use sceneEvent.ClientId to also track when clients are finished loading a scene
                    break;
                }
                // Handle Client to Server Unload Complete Notification(s)
                case SceneEventType.UnloadComplete:
                {
                    // This will let you know when an unload is completed
                    // You can follow the same pattern above as SceneEventType.LoadComplete here

                    // Server Side: receives this notification for both itself and all clients
                    // Client Side: receives this notification for itself

                    // So you can use sceneEvent.ClientId to also track when clients are finished unloading a scene

                    Debug.Log($"{GetType()}: Unloaded the {sceneEvent.SceneName} scene on " +
                              $"{clientOrServer}-({sceneEvent.ClientId}).", this);
                    break;
                }
                // Handle Server to Client Load Complete (all clients finished loading notification)
                case SceneEventType.LoadEventCompleted:
                {
                    // This will let you know when all clients have finished loading a scene
                    // Received on both server and clients
                    foreach (var clientId in sceneEvent.ClientsThatCompleted)
                    {
                        // Example of parsing through the clients that completed list
                        if (NetworkManager.Singleton.IsServer)
                        {
                            // Handle any server-side tasks here
                        }
                        else
                        {
                            // Handle any client-side tasks here
                        }
                    }

                    var completedClients = sceneEvent.ClientsThatCompleted
                                                     .Aggregate(new StringBuilder(),
                                                                (b, v) => b.Append(v).Append(", ")).ToString()
                                                     .TrimEnd(' ', ',');
                    var timedOutClients = sceneEvent.ClientsThatTimedOut
                                                    .Aggregate(new StringBuilder(),
                                                               (b, v) => b.Append(v).Append(", ")).ToString()
                                                    .TrimEnd(' ', ',');
                    Debug.Log($"{GetType()}: Load event completed for the following client " +
                              $"identifiers:({completedClients})", this);
                    if (sceneEvent.ClientsThatTimedOut.Count > 0)
                    {
                        Debug.LogWarning($"{GetType()}: Load event timed out for the following client " +
                                         $"identifiers:({timedOutClients})", this);
                    }

                    break;
                }
                // Handle Server to Client unload Complete (all clients finished unloading notification)
                case SceneEventType.UnloadEventCompleted:
                {
                    // This will let you know when all clients have finished unloading a scene
                    // Received on both server and clients
                    foreach (var clientId in sceneEvent.ClientsThatCompleted)
                    {
                        // Example of parsing through the clients that completed list
                        if (NetworkManager.Singleton.IsServer)
                        {
                            // Handle any server-side tasks here
                        }
                        else
                        {
                            // Handle any client-side tasks here
                        }
                    }

                    Debug.Log($"{GetType()}: Unload event completed for the following client " +
                              $"identifiers:({sceneEvent.ClientsThatCompleted})", this);
                    if (sceneEvent.ClientsThatTimedOut.Count > 0)
                    {
                        Debug.LogWarning($"{GetType()}: Unload event timed out for the following client " +
                                         $"identifiers:({sceneEvent.ClientsThatTimedOut})", this);
                    }

                    break;
                }
            }
        }


        public void LoadSceneSingle(string sceneName)
        {
            if (NetworkManager.Singleton.IsServer && !string.IsNullOrEmpty(sceneName))
            {
                var status = NetworkManager.Singleton.SceneManager.LoadScene(sceneName, LoadSceneMode.Single);
                if (status != SceneEventProgressStatus.Started)
                {
                    Debug.LogWarning($"{GetType()}: Failed to load {sceneName} " +
                                     $"with a {nameof(SceneEventProgressStatus)}: {status}");
                }
            }
        }

#endregion

#region Context Menu

        [ContextMenu("Login as Server")]
        private async UniTask LoginAsServer_CONTEXT_MENU() =>
            await LoginAsServer(unityTransport.ConnectionData.Address, unityTransport.ConnectionData.Port);

        [ContextMenu("Login as Host")]
        private async UniTask LoginAsHost_CONTEXT_MENU() =>
            await LoginAsHost(unityTransport.ConnectionData.Address, unityTransport.ConnectionData.Port);

        [ContextMenu("Login as Client")]
        private async UniTask LoginAsClient_CONTEXT_MENU() =>
            await LoginAsClient(unityTransport.ConnectionData.Address, unityTransport.ConnectionData.Port);

        [ContextMenu("Logout")]
        private async UniTask Logout_CONTEXT_MENU() => await Logout();

#endregion
    }
}
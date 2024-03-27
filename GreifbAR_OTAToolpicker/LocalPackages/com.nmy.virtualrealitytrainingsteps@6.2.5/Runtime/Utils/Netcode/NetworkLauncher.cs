using System;
using Cysharp.Threading.Tasks;
using Unity.Netcode;
using UnityEngine;
using Random = UnityEngine.Random;
#if UNITY_EDITOR
using UnityEditor;
#if NMY_ENABLE_PARRELSYNC
using ParrelSync;
#endif

#if NMY_ENABLE_XR_MANAGEMENT
using UnityEngine.XR.Management;
#endif

#endif // UNITY_EDITOR

namespace NMY.VirtualRealityTraining
{
    /// <summary>
    /// <para>
    /// This class is responsible for the automatic start of the server-client architecture.
    /// </para>
    /// <para>
    /// The behaviour of this class depends on whether it is running in play mode or as build.
    /// </para>
    /// <para><b>Play Mode</b><br/>
    /// If ParrelSync is installed, it starts the clone depending on the given arguments.
    /// It starts the original instance as <see cref="_runOriginalAs"/>.
    /// </para>
    ///
    /// <para><b>Build</b><br/>
    /// In build mode, command line arguments can be used to alter the execution (launch as client, host, server) as well
    /// as some server specific parameters (IP, port, target framerate)
    /// </para>
    /// </summary>
    public class NetworkLauncher : MonoBehaviour
    {
        /// <summary>
        /// The default connection type when running the application in a build.
        /// </summary>
        [Header("Client Settings")]
        [SerializeField] private NetworkConnectionTypes _autoConnectClientAs = NetworkConnectionTypes.Host;

        /// <summary>
        /// The port of the server to connect to.
        /// </summary>
        [Header("Server Settings")]
        [SerializeField] private string _serverPort = "7777";

        /// <summary>
        /// The target frame rate of the server.
        /// </summary>
        [SerializeField] private int _targetFrameRate = 60;

        /// <summary>
        /// A reference to a <see cref="NetworkTransport"/> object for the local host configuration.
        /// </summary>
        [Header("Local Unity Testing")]
        [SerializeField] private NetworkTransport _localHostTransport;

        /// <summary>
        /// The default connection type when running the application in play mode.
        /// </summary>
        [SerializeField] private NetworkConnectionTypes _runOriginalAs = NetworkConnectionTypes.Host;

        /// <summary>
        /// The IP of the server.
        /// </summary>
        private string _serverIP;

        /// <summary>
        /// An enumeration representing the type of the network connection.
        /// </summary>
        private enum NetworkConnectionTypes
        {
            Server,
            Client,
            Host
        }

        /// <summary>
        /// A reference to the <see cref="sessionManager"/> object in the scene.
        /// </summary>
        private SessionManager sessionManager => SessionManager.instance;

        /// <summary>
        /// Starts either <see cref="Start_Editor"/> or <see cref="Start_Build"/> depending where the application is started.
        /// </summary>
        private void Start()
        {
#if UNITY_EDITOR
            Start_Editor();
#else // UNITY_EDITOR
            Start_Build();
#endif
        }

#if UNITY_EDITOR
        /// <summary>
        /// Handles the start procedure when running the application in play mode.
        /// It checks the arguments of the <see cref="ClonesManager"/> of ParrelSync if it is installed and runs
        /// the clone depending whether the given argument is "server", "client", or "host". The original instance is
        /// started depending on <see cref="_runOriginalAs"/>.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        private void Start_Editor()
        {
            if (!EditorApplication.isPlaying) return;

            NetworkManager.Singleton.NetworkConfig.NetworkTransport = _localHostTransport;
#if NMY_ENABLE_PARRELSYNC
            if (ClonesManager.IsClone())
            {
                if (ClonesManager.GetArgument() == "server") LaunchAsServer().Forget();
                else if (ClonesManager.GetArgument() == "client") LaunchAsClient().Forget();
                else if (ClonesManager.GetArgument() == "host") LaunchAsHost().Forget();
            }
            else
#endif // NMY_ENABLE_PARRELSYNC
            {
                switch (_runOriginalAs)
                {
                    case NetworkConnectionTypes.Server:
#if NMY_ENABLE_XR_MANAGEMENT
                        XRGeneralSettings.Instance.Manager.StopSubsystems();
#endif // NMY_ENABLE_XR_MANAGEMENT
                        LaunchAsServer().Forget();
                        break;
                    case NetworkConnectionTypes.Client:
                        LaunchAsClient().Forget();
                        break;
                    case NetworkConnectionTypes.Host:
                        LaunchAsHost().Forget();
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }
#endif // UNITY_EDITOR

        /// <summary>
        /// Handles the start procedure when running the application in a build.
        /// It checks the command line arguments to set the internal variables before establishing the connection.
        /// </summary>
        /// <remarks>
        /// 
        /// <list type="bullet">
        /// <item><c>--launch-as-server</c>: Launches the application as server. </item>
        /// <item><c>--launch-as-client</c>: Launches the application as client. </item>
        /// <item><c>--launch-as-host</c>: Launches the application as host. </item>
        /// <item><c>--targetFrameRate</c>: Sets the target framerate of the server to the given value. </item>
        /// <item><c>--serverPort</c>: Sets the server port to the given value. </item>
        /// <item><c>--serverIP</c>: Sets the server IP to the given value. </item>
        /// </list>
        /// </remarks>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        private void Start_Build()
        {
            var args = Environment.GetCommandLineArgs();

            foreach (var arg in args)
            {
                _autoConnectClientAs = arg switch
                {
                    "--launch-as-server" => NetworkConnectionTypes.Server,
                    "--launch-as-client" => NetworkConnectionTypes.Client,
                    "--launch-as-host"   => NetworkConnectionTypes.Host,
                    _                    => _autoConnectClientAs
                };

                if (arg.Contains("--targetFrameRate="))
                {
                    var split = arg.Split('=');
                    if (split.Length >= 2 && int.TryParse(split[1], out _targetFrameRate))
                    {
                    }
                }

                if (arg.Contains("--serverPort="))
                {
                    var split = arg.Split('=');
                    _serverPort = split.Length >= 2 ? split[1] : null;
                }

                if (arg.Contains("--serverIP="))
                {
                    var split = arg.Split('=');
                    if (split.Length >= 2)
                    {
                        _serverIP = split[1];
                    }
                }
            }

            switch (_autoConnectClientAs)
            {
                case NetworkConnectionTypes.Server:
                    LaunchAsServer().Forget();
                    break;
                case NetworkConnectionTypes.Client:
                    LaunchAsClient().Forget();
                    break;
                case NetworkConnectionTypes.Host:
                    LaunchAsHost().Forget();
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        /// <summary>
        /// Launches this application as server.
        /// </summary>
        private async UniTaskVoid LaunchAsServer()
        {
            Application.targetFrameRate = _targetFrameRate;
            await sessionManager.LoginAsServer(_serverIP, ushort.Parse(_serverPort));
        }

        /// <summary>
        /// Launches this application as client.
        /// </summary>
        private async UniTaskVoid LaunchAsClient()
        {
            await sessionManager.LoginAsClient(_serverIP, ushort.Parse(_serverPort));
        }

        /// <summary>
        /// Launches this application as host.
        /// </summary>
        private async UniTaskVoid LaunchAsHost()
        {
            Application.targetFrameRate = _targetFrameRate;
            await sessionManager.LoginAsHost(_serverIP, ushort.Parse(_serverPort));
        }
    }
}
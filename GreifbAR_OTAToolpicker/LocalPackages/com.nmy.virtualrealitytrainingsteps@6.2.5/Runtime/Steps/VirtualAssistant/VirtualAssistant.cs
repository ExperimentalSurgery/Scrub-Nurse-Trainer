using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

#if NMY_ENABLE_UNITY_ATOMS
using UnityAtoms.BaseAtoms;
#endif

#if NMY_ENABLE_GOOGLE_CLOUD_TTS
using NMY.GoogleCloudTextToSpeech;
using UnityEngine.ResourceManagement.Exceptions;
using UnityEngine.Localization.Settings;
#endif

namespace NMY.VirtualRealityTraining.VirtualAssistant
{
    /// <summary>
    /// The `VirtualAssistant` class is a `MonoBehaviour` that represents a virtual assistant .
    /// It provides various functionality for the virtual assistant, such as following the player, rotating to face a
    /// target, and playing animations and audio clips. The `VirtualAssistant` class also provides a static `instance`
    /// property that allows other classes to access the virtual assistant instance in the game.
    /// </summary>
    public class VirtualAssistant : MonoBehaviour
    {
        /// <summary>
        /// The `GameObject` that represents the player.
        /// </summary>
        [SerializeField] private GameObject _player;

        /// <summary>
        /// A flag that indicates whether the virtual assistant should follow the player.
        /// </summary>
        [SerializeField] private bool _followPlayer;

        /// <summary>
        /// A flag that indicates whether the virtual assistant should look at a target. If this flag is `true`, the
        /// virtual assistant will rotate to face the target.
        /// </summary>
        [SerializeField] private bool _lookAtTarget;

        /// <summary>
        /// The maximum distance between the player and the virtual assistant before the virtual assistant will start
        /// moving towards the player.
        /// </summary>
        [SerializeField] private float _maxDistanceToPlayer = 4f;

        /// <summary>
        /// The desired distance between the player and the virtual assistant. The virtual assistant will move towards
        /// or away from the player to try to maintain this distance.
        /// </summary>
        [SerializeField] private float _desiredDistToPlayer = 2f;

        /// <summary>
        /// The speed at which the virtual assistant moves towards or away from the player.
        /// </summary>
        [SerializeField] private float _moveSpeed = 1f;

        /// <summary>
        /// The speed at which the virtual assistant rotates to face a target.
        /// </summary>
        [SerializeField] private float _rotationSpeed = .5f;

        /// <summary>
        /// The height above the ground at which the virtual assistant will hover.
        /// </summary>
        [SerializeField] private float _floatingHeight = 1.6f;

        /// <summary>
        /// The distance to the side of the player at which the virtual assistant will position itself.
        /// </summary>
        [SerializeField] private float _sideOffset = 1f;

        /// <summary>
        /// The <see cref="AudioSource"/> component used to play audio clips.
        /// </summary>
        [SerializeField] private AudioSource _audioSource;

        /// <summary>
        /// The <see cref="Animator"/> component used to play animations.
        /// </summary>
        [SerializeField] private Animator _animator;

        /// <summary>
        /// The `GameObject` that the virtual assistant should look at.
        /// </summary>
        private GameObject _lookTarget;

        /// <summary>
        /// The rotation that the virtual assistant should use to face the target.
        /// </summary>
        private Quaternion _lookRotation;

        /// <summary>
        /// The direction from the virtual assistant to the target.
        /// </summary>
        private Vector3 _lookDirection;

        /// <summary>
        /// The singleton instance of the <see cref="VirtualAssistant"/> class.
        /// </summary>
        public static VirtualAssistant instance { get; private set; }


        /// <summary>
        /// Initializes the `VirtualAssistant` instance. This method is called when the `VirtualAssistant` component is
        /// first initialized, and it ensures that only one instance of the `VirtualAssistant` class exists at any given
        /// time.
        /// </summary>
        private void Awake()
        {
            if (instance is null)
            {
                // first of its kind: use this as our Singleton
                instance = this;
                DontDestroyOnLoad(gameObject); // keep alive!
                SceneManager.sceneLoaded += OnSceneChanged;
            }
            else
            {
                // already exists? get rid of "illegal" copy! but copy relevant local data first
                // use scene-local pos+rot
                instance.transform.position = transform.position;
                instance.transform.rotation = transform.rotation;
                // destroy this copy (including all children) as quickly as possible (i.e., before more of its objects are trying to initialize)
                DestroyImmediate(gameObject);
                return; // prevent any further code execution in this Awake()
            }
        }

        /// <summary>
        /// Updates local scene information when a new scene is loaded. This method is called when the active scene is
        /// changed, and it updates the <see cref="_player"/> and <see cref="_lookTarget"/> fields to reflect the new scene.
        /// </summary>
        /// <param name="scene">The new scene that was loaded.</param>
        /// <param name="mode">The mode in which the scene was loaded.</param>
        private void OnSceneChanged(Scene scene, LoadSceneMode mode)
        {
            if (!_player) _player = Camera.main.gameObject;
            _lookTarget = _player;
        }

        /// <summary>
        /// This method is called once per frame, and it updates the virtual assistant's position and orientation based
        /// on the values of the <see cref="_followPlayer"/> and <see cref="_lookAtTarget"/> fields.
        /// /// </summary>
        private void Update()
        {
            var distToPlayer = (transform.position - _player.transform.position).magnitude;
            if (_followPlayer && distToPlayer > _maxDistanceToPlayer)
            {
                MoveToPlayer();
            }

            if (_lookAtTarget && _lookTarget is not null)
            {
                EvaluateLookDir();
            }
        }

        /// <summary>
        /// Calculates the direction and orientation in which the virtual assistant should look at the target.
        /// </summary>
        /// <remarks>
        /// This method updates the <see cref="_lookDirection"/> and <see cref="_lookRotation"/> fields based on the
        /// position of the target, and it also rotates the virtual assistant towards the target over time.
        /// </remarks>
        private void EvaluateLookDir()
        {
            _lookDirection     = (_lookTarget.transform.position - transform.position).normalized;
            _lookRotation      = Quaternion.LookRotation(_lookDirection);
            transform.rotation = Quaternion.Slerp(transform.rotation, _lookRotation, Time.deltaTime * _rotationSpeed);
        }

        /// <summary>
        /// Enables or disables the virtual assistant's movement towards the player.
        /// </summary>
        /// <param name="follow">Whether the virtual assistant should follow the player.</param>
        public void FollowPlayer(bool follow) => _followPlayer = follow;

        /// <summary>
        /// Makes the virtual assistant look at the player.
        /// </summary>
        /// <remarks>
        /// This method sets the <see cref="_lookTarget"/> field to the player's
        /// game object, and it also sets the <see cref="_lookAtTarget"/> field to `true` so that the virtual assistant will
        /// rotate towards the target in the `Update` method.
        /// </remarks>
        public void LookAtPlayer() => LookAtTarget(_player.gameObject);

        /// <summary>
        /// Makes the virtual assistant look at the specified target.
        /// </summary>
        /// <param name="target">The game object at which the virtual assistant should look.</param>
        public void LookAtTarget(GameObject target)
        {
            _lookTarget   = target;
            _lookAtTarget = true;
        }

        /// <summary>
        /// Makes the virtual assistant stop looking at the target.
        /// </summary>
        public void StopLooking()
        {
            _lookAtTarget = false;
        }

#if NMY_ENABLE_GOOGLE_CLOUD_TTS
        /// <summary>
        /// Makes the virtual assistant speak the specified text-to-speech audio clip.
        /// </summary>
        /// <param name="audioClip">The audio clip containing the text-to-speech audio.</param>
        /// <param name="ct">The cancellation token to use for canceling the operation.</param>
        /// <param name="audioTime">The time at which the audio should start playing. This parameter is optional and
        /// defaults to 0.</param>
        public async UniTask Speak(LocalizedTextToSpeechAudioClip audioClip, CancellationToken ct, float audioTime = 0)
        {
            if (audioClip == null)
            {
                Debug.LogError($"{GetType()}: LocalizedTextToSpeechAudioClip is null!", this);
                return;
            }

            if (audioClip.IsEmpty)
            {
                Debug.LogError($"{GetType()}: AudioClip was not found in TableReference " +
                               $"\"{audioClip.TableReference.TableCollectionName}\" for TableEntryReference key " +
                               $"\"{audioClip.TableEntryReference.Key}\"", this);
                return;
            }


            try
            {
                await LocalizationSettings.InitializationOperation;
                var item = await audioClip.LoadAssetAsync().ToUniTask(cancellationToken: ct);

                if (item == null || item.audioClip == null)
                {
                    Debug.LogError($"{GetType()}: LocalizedTextToSpeechItem or AudioClip could not be loaded!", this);
                    return;
                }

                foreach (var entry in item.timestamps) TriggerExpression(entry, ct).Forget();

                _audioSource.clip = item.audioClip;
                _audioSource.time = Mathf.Min(audioTime, item.GetDuration());
                _audioSource.Play();
                await UniTask.Delay(TimeSpan.FromSeconds(Mathf.Max(0, item.GetDuration() - audioTime)),
                                    cancellationToken: ct);
            }
            // Something went completely wrong with the asset in the AssetTable - maybe the asset was deleted from the
            // file system and could therefore not be loaded?
            catch (OperationException)
            {
                Debug.LogError($"{GetType()}: Could not load LocalizedTextToSpeechAudioClip Asset. ", this);
            }
        }
#endif

        /// <summary>
        /// Stops the virtual assistant from speaking, if it is currently speaking. This method cancels any ongoing text-to-speech
        /// operations and stops playback of any audio clips.
        /// </summary>
        public async UniTask StopSpeaking()
        {
            _audioSource.Pause();
            await UniTask.CompletedTask;
        }

#region Expressions

#if NMY_ENABLE_UNITY_ATOMS
        /// <summary>
        /// <para>
        /// Triggers a (facial) expression animation on the virtual assistant. The specified <paramref name="trigger"/> string
        /// will be used to activate a corresponding animation trigger on the virtual assistant's <see cref="Animator"/> component.
        /// </para>
        ///
        /// <para>
        /// The available trigger strings and corresponding animations are defined in the virtual assistant's animation
        /// controller.
        /// </para>
        /// </summary>
        /// <remarks>
        /// This method has no effect if the virtual assistant does not have an `Animator` component attached.
        /// </remarks>
        /// <param name="trigger">The animation trigger to activate on the virtual assistant's `Animator` component.</param>
        public void TriggerExpression(StringConstant trigger) => TriggerExpression(trigger.Value);
#endif

#if NMY_ENABLE_GOOGLE_CLOUD_TTS
        /// <summary>
        /// <para>
        /// Triggers a (facial) expression animation on the virtual assistant. The specified <paramref name="entry"/> provides
        /// the time at which the expression should be triggered, and the `CancellationToken` <paramref name="ct"/> can be used to cancel the
        /// operation.
        /// </para>
        ///
        /// <para>
        /// If the virtual assistant is currently speaking using text-to-speech, the specified expression will be triggered at
        /// the specified time, provided that the operation has not been cancelled. The available expressions and their
        /// corresponding animation triggers are defined in the virtual assistant's animation controller.
        /// </para>
        /// </summary>
        ///
        /// <remarks>
        /// This method has no effect if the virtual assistant does not have an `Animator` component attached.
        /// </remarks>
        /// <param name="entry">The <see cref="TextToSpeechTimestampEntry"/> providing the time at which the expression should be triggered.</param>
        /// <param name="ct">A <see cref="CancellationToken"/> that can be used to cancel the operation.</param>
        private async UniTaskVoid TriggerExpression(TextToSpeechTimestampEntry entry, CancellationToken ct)
        {
            await UniTask.Delay(TimeSpan.FromSeconds(entry.timeSeconds), cancellationToken: ct);
            TriggerExpression(entry.markName);
        }
#endif


        /// <summary>
        /// Triggers an animation state in the virtual assistant's <see cref="_animator"/> component by specifying a name of the state to
        /// transition to.
        /// </summary>
        /// <param name="trigger">The name of the animation state to transition to.</param>
        public void TriggerExpression(string trigger)
        {
            if (_animator is null) return;
            if (string.IsNullOrEmpty(trigger)) return;

            _animator.SetTrigger(trigger);
        }

#endregion

#region Move

        /// <summary>
        /// Moves the virtual assistant to a new position by providing the <paramref name="moveData"/> object of type
        /// <see cref="VirtualAssistantMoveData"/> that contains information about the target position, movement speed,
        /// and whether the assistant should look at a target while moving.
        /// </summary>
        /// <param name="moveData">The <see cref="VirtualAssistantMoveData"/> object containing information about the target position and movement speed.</param>
        /// <param name="ct">A <see cref="CancellationToken"/> that can be used to cancel the movement.</param>
        public async UniTask Move(VirtualAssistantMoveData moveData, CancellationToken ct)
        {
            var curve = moveData.animationCurve;

            var elapsedTime = 0.0f;
            var waitTime    = moveData.duration;
            var currentPos  = transform.position;
            var newPos      = moveData.destination.transform.position;

            LookAtTarget(moveData.lookAtPlayerDuringMove ? _player : moveData.destination);

            try
            {
                while (elapsedTime < waitTime)
                {
                    transform.position =  Vector3.Lerp(currentPos, newPos, curve.Evaluate(elapsedTime / waitTime));
                    elapsedTime        += Time.deltaTime;
                    await UniTask.NextFrame(PlayerLoopTiming.FixedUpdate, ct);
                }
            }
            finally
            {
                transform.position = newPos;
                if (moveData.lookAtPlayerAfterMove) LookAtPlayer();
            }
        }

        /// <summary>
        /// Moves the virtual assistant to a specified location instantly, without any interpolation or smoothing.
        /// </summary>
        /// <param name="moveData">The data that specifies the destination and orientation of the move.</param>
        public async UniTask MoveInstantly(VirtualAssistantMoveData moveData)
        {
            transform.position = moveData.destination.transform.position;
            if (moveData.lookAtPlayerAfterMove) LookAtPlayer();
            await UniTask.CompletedTask;
        }

        /// <summary>
        /// Moves the virtual assistant towards the player if it is too far away. This method updates the position of the
        /// virtual assistant to maintain the desired distance from the player.
        /// </summary>
        private void MoveToPlayer()
        {
            var playerTrans = _player.transform;

            var pos = playerTrans.position + (playerTrans.forward * _desiredDistToPlayer) +
                      (playerTrans.right * _sideOffset);
            var moveTarget = new Vector3(pos.x, _floatingHeight, pos.z);

            var distance = (moveTarget - transform.position).magnitude;
            if (distance >= .1f)
            {
                transform.position = Vector3.Lerp(transform.position, moveTarget, Time.deltaTime * _moveSpeed);
            }
            else
            {
                LookAtPlayer();
            }
        }

#endregion
    }
}
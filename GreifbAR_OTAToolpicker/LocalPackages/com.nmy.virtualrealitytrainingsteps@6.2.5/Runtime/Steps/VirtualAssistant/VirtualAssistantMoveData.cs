using System;
using UnityEngine;

namespace NMY.VirtualRealityTraining.VirtualAssistant
{
    /// <summary>
    /// Store data related to the movement of a virtual assistant in a virtual reality training environment.
    /// </summary>
    [Serializable]
    public class VirtualAssistantMoveData
    {
#region SerializeFields

        /// <summary>
        /// The object in the virtual environment that the virtual assistant should move towards.
        /// The virtual assistant will move along a straight line from its current position to this destination.
        /// </summary>
        [SerializeField] private GameObject _destination;

        /// <summary>
        /// The duration of the movement, in seconds. This determines how long it will take for the virtual assistant
        /// to move from its current position to the destination.
        /// </summary>
        [SerializeField] private float _duration = 3.0f;
        
        /// <summary>
        /// Specifies whether the virtual assistant should look at the player while moving.
        /// If this is true, the virtual assistant will maintain eye contact with the player throughout the movement.
        /// </summary>
        /// <remarks>
        /// This can create a more natural and engaging interaction, as it makes the virtual assistant seem
        /// more attentive and responsive to the player's actions.
        /// </remarks>
        [SerializeField] private bool  _lookAtPlayerDuringMove;

        /// <summary>
        /// Specifies whether the virtual assistant should look at the player after completing the movement.
        /// If this is true, the virtual assistant will turn its head to face the player once it has reached the destination.
        /// </summary>
        [SerializeField] private bool  _lookAtPlayerAfterMove = true;

        /// <summary>
        /// The curve to use for the movement animation.
        /// This determines the speed at which the virtual assistant moves from its current position to the destination.
        /// </summary>
        /// <remarks>
        /// It is initialized with an EaseInOut curve, which means the virtual assistant will start the movement slowly,
        /// accelerate in the middle, and then slow down again towards the end of the movement.
        /// This creates a more natural and smooth movement for the virtual assistant.
        /// </remarks>
        [SerializeField] private AnimationCurve _animationCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

#endregion

#region Properties

                /// <summary>
        /// The object in the virtual environment that the virtual assistant should move towards.
        /// The virtual assistant will move along a straight line from its current position to this destination.
        /// </summary>
        public GameObject destination => _destination;
        
        /// <summary>
        /// The duration of the movement, in seconds. This determines how long it will take for the virtual assistant
        /// to move from its current position to the destination.
        /// </summary>
        public float      duration    => _duration;

        /// <summary>
        /// Specifies whether the virtual assistant should look at the player while moving.
        /// If this is true, the virtual assistant will maintain eye contact with the player throughout the movement.
        /// </summary>
        /// <remarks>
        /// This can create a more natural and engaging interaction, as it makes the virtual assistant seem
        /// more attentive and responsive to the player's actions.
        /// </remarks>
        public bool lookAtPlayerDuringMove => _lookAtPlayerDuringMove;

        /// <summary>
        /// Specifies whether the virtual assistant should look at the player after completing the movement.
        /// If this is true, the virtual assistant will turn its head to face the player once it has reached the destination.
        /// </summary>
        public bool lookAtPlayerAfterMove => _lookAtPlayerAfterMove;
        
        /// <summary>
        /// The curve to use for the movement animation.
        /// This determines the speed at which the virtual assistant moves from its current position to the destination.
        /// </summary>
        /// <remarks>
        /// It is initialized with an EaseInOut curve, which means the virtual assistant will start the movement slowly,
        /// accelerate in the middle, and then slow down again towards the end of the movement.
        /// This creates a more natural and smooth movement for the virtual assistant.
        /// </remarks>
        public AnimationCurve animationCurve => _animationCurve;

#endregion

#region Constructor

        /// <summary>
        /// Overloaded constructor for the VirtualAssistantMoveData class.
        /// Allows the user to specify the initial values for the object's fields when creating a new VirtualAssistantMoveData object.
        /// </summary>
        /// <param name="destination">The object in the virtual environment that the virtual assistant should move towards.</param>
        /// <param name="duration">The duration of the movement, in seconds.</param>
        /// <param name="lookAtPlayerDuringMove">Specifies whether the virtual assistant should look at the player during the movement.</param>
        /// <param name="lookAtPlayerAfterMove">Specifies whether the virtual assistant should look at the player after the movement.</param>
        public VirtualAssistantMoveData(GameObject destination, float duration, bool lookAtPlayerDuringMove, bool lookAtPlayerAfterMove)
        {
            _destination            = destination;
            _duration               = duration;
            _lookAtPlayerDuringMove = lookAtPlayerDuringMove;
            _lookAtPlayerAfterMove  = lookAtPlayerAfterMove;
        }

        /// <summary>
        /// Default constructor for the VirtualAssistantMoveData class.
        /// Creates a new VirtualAssistantMoveData object and initializes the object's fields with default values.
        /// </summary>
        public VirtualAssistantMoveData() { }

#endregion
    }
}
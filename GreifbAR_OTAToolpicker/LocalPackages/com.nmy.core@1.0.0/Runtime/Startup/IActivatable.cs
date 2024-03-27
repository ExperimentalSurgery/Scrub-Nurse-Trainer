using UnityEngine;
using System;
using System.Collections;

namespace NMY {
	
	/// <summary>
	/// Interface for activatable objects.
	/// </summary>
	/// <remarks>
	/// <para>The <c>IActivatable</c> interface represents a concept for activating game objects in 
	/// Unity3D, i.e. for setting the <c>active</c> property of a game object. The concept consists
	/// of three methods, two properties and four events which must be provided when implementing 
	/// this interface.</para>
	/// <para>
	/// The <see cref="Activate" /> and <see cref="ActivateImmediately" /> methods are used to 
	/// activate an object, <see cref="Deactivate" /> and <see cref="DeactivateImmediately" /> are 
	/// used to deactivate it. 
	/// </para>
	/// <para><see cref="Activate" /> and <see cref="Deactivate" /> are used for 
	/// asynchronous de/activation and can contain animations or other async operations.</para>
	/// <para><see cref="ActivateImmediately" /> and <see cref="DeactivateImmediately" /> are used for 
	/// synchronous de/activation and must de/activate the object without delay.</para>
	/// <para>
	/// Refer to the <see cref="ActivatableStartupBehaviour" /> class for an example implementation 
	/// of the <c>IActivatable</c> interface.
	/// </para>
	/// </remarks>

	public interface IActivatable  {

		/// <summary>The <c>ActivateEvent</c> is send on enter of an <see cref="Activate"/> or 
		/// <see cref="ActivateImmediately"/> call.</summary>
		event EventHandler ActivateEvent;

		/// <summary>The <c>ActivatedEvent</c> is send on exit of an <see cref="Activate"/> or 
		/// <see cref="ActivateImmediately"/> call. If the activation uses some async operation
		/// (e.g. an animation) it must be send at the end of that operation.</summary>
		event EventHandler ActivatedEvent;

		/// <summary>The <c>DeactivateEvent</c> is send on enter of a <see cref="Deactivate"/> or 
		/// <see cref="DeactivateImmediately"/> call.</summary>
		event EventHandler DeactivateEvent;

		/// <summary>The <c>DeactivatedEvent</c> is send on exit of an <see cref="Deactivate"/> or 
		/// <see cref="DeactivateImmediately"/> call. If the activation uses some async operation
		/// (e.g. an animation) it must be send at the end of that operation.</summary>
		event EventHandler DeactivatedEvent;
	
		/// <summary>
		/// Activates the implementing object. 
		/// </summary>
		/// <remarks>
		/// The implementation must conform to the following:
		/// <list type="bullet">
		///   <item><description><see cref="Activate" /> must only run when the object is deactivated. When called on an 
		/// 	 already activated object the method  must return immediately.</description></item>
		///   <item><description>Before running <see cref="Activate" /> the <see cref="isActivated" /> method must return <c>false</c>.</description></item>
		///   <item><description>After running <see cref="Activate" /> the <see cref="isActivated" /> method must return <c>true</c>.</description></item>
		///   <item><description>When <see cref="Activate" /> is entered the <see cref="ActivateEvent" /> must be send.</description></item>
		///   <item><description>When <see cref="Activate" /> is left the <see cref="ActivatedEvent" /> must be send.</description></item>
		/// </list>
		/// </remarks>
		void Activate();
		
		/// <summary>
		/// Deactivates the implementing object. 
		/// </summary>
		/// <remarks>
		/// The implementation must conform to the following:
		/// <list type="bullet">
		///   <item><description><see cref="Deactivate" /> must only run when the object is activated. When called on an 
		/// 	 already deactivated object the method must return immediately.</description></item>
		///   <item><description>Before running <see cref="Deactivate" /> the <see cref="isActivated"/> method must return <c>true</c>.</description></item>
		///   <item><description>After running <see cref="Deactivate"/> the <see cref="isActivated"/> method must return <c>false</c>.</description></item>
		///   <item><description>When <see cref="Deactivate"/> is entered the <see cref="DeactivateEvent"/> must be send.</description></item>
		///   <item><description>When <see cref="Deactivate"/> is left the <see cref="DeactivatedEvent"/> must be send.</description></item>
		/// </list>
		/// </remarks>
		void Deactivate();

		/// <summary>
		/// Activates the implementing object immediately. 
		/// </summary>
		/// <remarks>
		/// The implementation must conform to the following:
		/// <list type="bullet">
		///   <item><description><see cref="ActivateImmediately"/> must only run when the object is deactivated. When 
		///      called on an already activated object the method  must return immediately.</description></item>
		///   <item><description>Before running <see cref="ActivateImmediately"/> the <see cref="isActivated"/> method must return <c>false</c>.</description></item>
		///   <item><description>After running <see cref="ActivateImmediately"/> the <see cref="isActivated"/> method must return <c>true</c>.</description></item>
		///   <item><description>When <see cref="ActivateImmediately"/> is entered the <see cref="ActivateEvent"/> must be send.</description></item>
		///   <item><description>When <see cref="ActivateImmediately"/> is left the <see cref="ActivatedEvent"/> must be send.</description></item>
		/// </list>
		/// </remarks>
		void ActivateImmediately();

		/// <summary>
		/// Deactivates the implementing object immediately. The implementation must conform to 
		/// the following:
		/// </summary>
		/// <remarks>
		/// <list type="bullet">
		///   <item><description><see cref="DeactivateImmediately"/> must only run when the object is activated. When 
		///      called on an already deactivated object the method must return immediately.</description></item>
		///   <item><description>Before running <see cref="DeactivateImmediately"/> the <see cref="isActivated"/> method must return <c>true</c>.</description></item>
		///   <item><description>After running <see cref="DeactivateImmediately"/> the <see cref="isActivated"/> method must return <c>false</c>.</description></item>
		///   <item><description>When <see cref="DeactivateImmediately"/> is entered the <see cref="DeactivateEvent"/> must be send.</description></item>
		///   <item><description>When <see cref="DeactivateImmediately"/> is left the <see cref="DeactivatedEvent"/> must be send.</description></item>
		/// </list>
		/// </remarks>
		void DeactivateImmediately();

		/// <summary>
		/// Gets a value indicating whether the implementing object is currently activated.
		/// </summary>
		/// <returns>
		/// <c>true</c> if is activated; otherwise, <c>false</c>.
		/// </returns>
		bool isActivated { get; }
		
		/// <summary>
		/// Gets a value indicating whether the implementing object is initially activated.
		/// </summary>
		/// <returns>
		/// <c>true</c> if is initially activated; otherwise, <c>false</c>.
		/// </returns>
		bool isInitiallyActivated { get; }
	}

} // namespace NMY

using UnityEngine;
using System;
using System.Collections;

namespace NMY {
	
	public enum OpenableState { Opened, Closed };
	
	/// <summary>
	/// Interface for openable objects.
	/// </summary>
	/// <para>The IOpenable interface represents a concept for opening/closing game objects in 
	/// Unity3D. The concept consists of
	/// three methods, two properties and four events which must be provided when implementing this 
	/// interface.</para>
	/// <para>
	/// The Open() and OpenImmediately() method is used to open an object, Close() and 
	/// CloseImmediately() are used to close it. 
	/// </para>

	public interface IOpenable  {
		
		event EventHandler OpenEvent;
		event EventHandler OpenedEvent;
		event EventHandler CloseEvent;
		event EventHandler ClosedEvent;
	
		OpenableState openableState { get; }
		
		/// <summary>
		/// Gets a value indicating whether the implementing object is initially opened.
		/// </summary>
		/// <value>
		/// <c>Opened</c> if is initially opened; otherwise, <c>Closed</c>.
		/// </value>
		OpenableState initialOpenableState { get; }
		
		/// <summary>
		/// Opens the implementing object. The implementation must conform to the following:
		/// </summary>
		/// <list type="bullet">
		///   <item>Open() must only run when the object is closed. When called on an 
		/// 	 already opened object the method  must return immediately.</item>
		///   <item>Before running Open() the openableState property must return <c>Closed</c>.</item>
		///   <item>After running Open() the openableState property must return <c>Opened</c>.</item>
		///   <item>When Open() is entered the OpenEvent must be send.</item>
		///   <item>When Open() is left the OpenedEvent must be send.</item>
		/// </list>
		void Open();
		
		/// <summary>
		/// Deactivates the implementing object. The implementation must conform to the following:
		/// </summary>
		/// <list type="bullet">
		///   <item>Deactivate() must only run when the object is deactivated. When called on an 
		/// 	 already activated object the method  must return immediately.</item>
		///   <item>Before running Deactivate() the isActivated() method must return <c>true</c>.</item>
		///   <item>After running Deactivate() the isActivated() method must return <c>false</c>.</item>
		///   <item>When Deactivate() is entered the DeactivateEvent must be send.</item>
		///   <item>When Deactivate() is left the DeactivatedEvent must be send.</item>
		/// </list>
		void Close();

		/// <summary>
		/// Activates the implementing object immediately. The implementation must conform to 
		/// the following:
		/// </summary>
		/// <list type="bullet">
		///   <item>ActivateImmediately() must only run when the object is deactivated. When 
		///      called on an already activated object the method  must return immediately.</item>
		///   <item>Before running ActivateImmediately() the isActivated() method must return <c>false</c>.</item>
		///   <item>After running ActivateImmediately() the isActivated() method must return <c>true</c>.</item>
		///   <item>When ActivateImmediately() is entered the ActivateEvent must be send.</item>
		///   <item>When ActivateImmediately() is left the ActivatedEvent must be send.</item>
		/// </list>
		void OpenImmediately();

		/// <summary>
		/// Deactivates the implementing object immediately. The implementation must conform to 
		/// the following:
		/// </summary>
		/// <list type="bullet">
		///   <item>DeactivateImmediately() must only run when the object is deactivated. When 
		///      called on an already activated object the method  must return immediately.</item>
		///   <item>Before running DeactivateImmediately() the isActivated() method must return <c>true</c>.</item>
		///   <item>After running DeactivateImmediately() the isActivated() method must return <c>false</c>.</item>
		///   <item>When DeactivateImmediately() is entered the DeactivateEvent must be send.</item>
		///   <item>When DeactivateImmediately() is left the DeactivatedEvent must be send.</item>
		/// </list>
		void CloseImmediately();

	}

} // namespace NMY

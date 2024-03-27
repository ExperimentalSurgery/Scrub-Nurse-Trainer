using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace NMY{

	/// <summary>
	/// Interface for sequenceable objects.
	/// </summary>

	public interface ISequencable{
		float GetOffset();
	}
}
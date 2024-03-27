using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace NMY{
	public class SimpleSequencableActivatable : SimpleAnimatorActivatable, ISequencable{
		[Header("Sequence Parameters")]
		public float offset;

		public float GetOffset(){
			return offset;
		}
	}
}

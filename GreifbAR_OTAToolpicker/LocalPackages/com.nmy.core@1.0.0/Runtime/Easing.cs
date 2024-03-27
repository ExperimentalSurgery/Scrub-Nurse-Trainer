using UnityEngine;
using System;
using System.Collections;

namespace NMY {

	/// <summary>
	/// Easing functions for [0..1] interpolation.
	/// </summary>
	/// <remarks>
	/// Call constructor with desired EaseType.
	/// Then query ease(t).
	/// </remarks>
	public class Easing {

		public enum EaseType {
			Linear,
			QuadIn,
			QuadOut,
			QuadInOut,
			CubicIn,
			CubicOut,
			CubicInOut
//			SqrtInOut, // anti-easing
//			Pow34InOut // experimental x^(3/4)
		}

		private EaseType _easeType;
		public EaseType easeType {
			get { return _easeType;}
		}

		public delegate float EasingFunction(float value);
		public EasingFunction ease;

		public Easing(EaseType newEaseType){
			_easeType=newEaseType;

			switch(_easeType){
				case EaseType.QuadIn:
					ease  = new EasingFunction(easeInQuad);
					break;
				case EaseType.QuadOut:
					ease  = new EasingFunction(easeOutQuad);
					break;
				case EaseType.QuadInOut:
					ease  = new EasingFunction(easeInOutQuad);
					break;
				case EaseType.CubicIn:
					ease  = new EasingFunction(easeInCubic);
					break;
				case EaseType.CubicOut:
					ease  = new EasingFunction(easeOutCubic);
					break;
				case EaseType.CubicInOut:
					ease  = new EasingFunction(easeInOutCubic);
					break;
//				case EaseType.SqrtInOut:
//					ease  = new EasingFunction(easeInOutSqrt);
//					break;
//				case EaseType.Pow34InOut:
//					ease  = new EasingFunction(easeInOutPow34);
//					break;
				default:
					ease  = new EasingFunction(linear);
					break;
			}
		}

		public float easeInQuad(float t){
			return t*t;
		}

		public float easeOutQuad(float t){
			return t*(2-t);
		}

		public float easeInOutQuad(float t){
			if(t<0.5f)
				return 2*t*t;
			return 1-2*(t-1)*(t-1);
		}

		public float easeInCubic(float t){
			return t*t*t;
		}

		public float easeOutCubic(float t){
			return t*(t*(t-3)+3);
		}

		public float easeInOutCubic(float t){
			if(t<0.5f)
				return 4*t*t*t;
			return t*(t*(4*t-12)+12)-3;
		}

//		public float easeInOutSqrt(float t){
//			if(t<0.5f)
//				return Mathf.Sqrt(t/2);
//			return 1-Mathf.Sqrt((1-t)/2);
//		}
//
//		public float easeInOutPow34(float t){
//			if(t<0.5f)
//				return Mathf.Pow(2*t,3/4f)/2;
//			return 1-Mathf.Pow(2-2*t,3/4f)/2;
//		}

		public float linear(float t){
			return t;
		}
	}
} // namespace NMY

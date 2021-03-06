﻿using System;

namespace HatlessEngine
{
	public static class Misc
	{
		/// <summary>
		/// Whether to show a messagebox when an unhandled exception is thrown.
		/// Crashlog will be written regardless of this setting.
		/// This is disabled by default when DEBUG is defined, but can be changed at will.
		/// </summary>
#if DEBUG
		public static bool ExceptionErrorMessageEnabled = false;
#else
		public static bool ExceptionErrorMessageEnabled = true;
#endif


		/// <summary>
		/// Returns whether a random chance is met.
		/// Explained: chance in values (e.g. 3 in 100) chance of returning true
		/// </summary>
		public static bool Chance(int values, int chance = 1)
		{
			int result = new Random().Next(values);
			return result < chance;
		}

		/// <summary>
		/// Returns the angle to or from angle1 to angle2 from -180f to 180f.
		/// </summary>
		public static float GetRelativeAngle(float angle1, float angle2)
		{
			float diff = angle2 - angle1;

			if (diff > 180f)
				diff -= 360;
			if (diff < -180f)
				diff += 360;

			return diff;
		}

		public static int Modulus(int a, int n)
		{
			int result = a % n;
			if (a < 0 && n > 0
				|| a > 0 && n < 0)
				result += n;
			return result;
		}
		public static float Modulus(float a, float n)
		{
			float result = a % n;
			if (a < 0 && n > 0
				|| a > 0 && n < 0)
				result += n;
			return result;
		}
	}
}
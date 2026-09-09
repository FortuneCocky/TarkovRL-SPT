using UnityEngine;

namespace IRLWeaponHandling.Utilities;

internal static class HandlingMath
{
	public static float Progress(float value, float from, float to)
	{
		if (Mathf.Approximately(from, to))
		{
			if (!(value >= to))
			{
				return 0f;
			}
			return 1f;
		}
		return Mathf.Clamp01((value - from) / (to - from));
	}

	public static float Decay(float value, float target, float speed, float deltaTime)
	{
		if (speed <= 0f)
		{
			return value;
		}
		return Mathf.Lerp(value, target, 1f - Mathf.Exp((0f - speed) * deltaTime));
	}

	public static float SignedRandom(float magnitude)
	{
		return Random.Range(0f - magnitude, magnitude);
	}
}

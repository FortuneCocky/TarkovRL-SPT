using EFT;
using IRLWeaponHandling.Configuration;
using IRLWeaponHandling.Utilities;
using UnityEngine;

namespace IRLWeaponHandling.Core;

internal struct WeaponStats
{
	public string WeaponId;

	public float TotalWeight;

	public float ErgonomicWeight;

	public float Ergonomics01;

	public float WeightProgress;

	public static WeaponStats Read(Player.FirearmController firearm)
	{
		WeaponStats result = default(WeaponStats);
		result.WeaponId = firearm.Weapon?.Id;
		result.TotalWeight = firearm.Weapon?.TotalWeight ?? 0f;
		result.ErgonomicWeight = firearm.ErgonomicWeight;
		result.Ergonomics01 = Mathf.Clamp01(firearm.TotalErgonomics / 100f);
		result.WeightProgress = HandlingMath.Progress(result.TotalWeight, HandlingConfig.LightWeightKg.Value, Mathf.Max(HandlingConfig.HeavyWeightKg.Value, HandlingConfig.LightWeightKg.Value + 0.1f));
		return result;
	}

	public bool Matches(Player.FirearmController firearm)
	{
		if (WeaponId != null)
		{
			return WeaponId == firearm.Weapon?.Id;
		}
		return false;
	}
}

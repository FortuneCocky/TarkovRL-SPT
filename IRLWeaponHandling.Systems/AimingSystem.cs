using EFT.Animations;
using IRLWeaponHandling.Configuration;
using IRLWeaponHandling.Core;
using UnityEngine;

namespace IRLWeaponHandling.Systems;

internal static class AimingSystem
{
	public static void Apply()
	{
		if (HandlingConfig.AimingEnabled.Value && HandlingState.HasWeapon)
		{
			ProceduralWeaponAnimation animation = HandlingState.Animation;
			WeaponStats stats = HandlingState.Stats;
			float num = Mathf.Lerp(HandlingConfig.AdsTimeLight.Value, HandlingConfig.AdsTimeHeavy.Value, stats.WeightProgress);
			float num2 = 1f + HandlingConfig.ErgonomicsInfluence.Value * (1f - stats.Ergonomics01);
			float num3 = 1f + HandlingConfig.StaminaAdsInfluence.Value * (1f - HandlingState.HandsStamina01);
			num = Mathf.Max(0.05f, num * num2 * num3);
			float aimingSpeed = 1f / num;
			float num4 = Mathf.InverseLerp(animation.AimSwayStartsThreshold, animation.AimSwayMaxThreshold, stats.TotalWeight * (1f - stats.Ergonomics01));
			if (HandlingConfig.SwayEnabled.Value)
			{
				num4 *= HandlingConfig.AimSwayMultiplier.Value;
			}
			float num5 = stats.ErgonomicWeight;
			if (HandlingConfig.SwayEnabled.Value)
			{
				num5 *= HandlingConfig.SwayMultiplier.Value;
			}
			animation.ManualSetVariables(aimingSpeed, num4, animation.Overweight, num5);
			animation.UpdateSwayFactors();
		}
	}
}

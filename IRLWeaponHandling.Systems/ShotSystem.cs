using EFT.Animations;
using IRLWeaponHandling.Configuration;
using IRLWeaponHandling.Core;
using IRLWeaponHandling.Utilities;
using UnityEngine;

namespace IRLWeaponHandling.Systems;

internal static class ShotSystem
{
	private static string _baseGainWeaponId;

	private static float _baseGainPerShot;

	public static void OnShot()
	{
		if (HandlingConfig.ShotMisalignmentEnabled.Value && HandlingState.HasWeapon)
		{
			WeaponStats stats = HandlingState.Stats;
			float num = HandlingConfig.ShotMisalignmentStrength.Value * (1f + stats.WeightProgress) * (1.5f - 0.5f * stats.Ergonomics01) * (HandlingState.IsAiming ? 0.5f : 1f) * (1f + (1f - HandlingState.HandsStamina01) * HandlingConfig.StaminaShotInfluence.Value);
			float value = HandlingConfig.ShotMisalignmentMax.Value;
			float yaw = Mathf.Clamp(HandlingMath.SignedRandom(num * 0.6f), 0f - value, value);
			float pitch = Mathf.Clamp(num * Random.Range(0.4f, 1f), 0f - value, value);
			DeadzoneSystem.AddImpulse(yaw, pitch);
		}
	}

	public static void ApplyWeaponVariables(ProceduralWeaponAnimation animation)
	{
		BreathEffector breath = animation.Breath;
		if (breath == null)
		{
			return;
		}
		string weaponId = HandlingState.Stats.WeaponId;
		if (weaponId != null)
		{
			if (_baseGainWeaponId != weaponId)
			{
				_baseGainWeaponId = weaponId;
				_baseGainPerShot = breath.AmplitudeGainPerShot;
			}
			breath.AmplitudeGainPerShot = (HandlingConfig.ShotMisalignmentEnabled.Value ? (_baseGainPerShot * (1f + HandlingConfig.ShotBreathPenalty.Value) * (1f + (1f - HandlingState.HandsStamina01) * HandlingConfig.StaminaShotInfluence.Value)) : _baseGainPerShot);
		}
	}

	public static void Reset()
	{
		_baseGainWeaponId = null;
		_baseGainPerShot = 0f;
	}
}

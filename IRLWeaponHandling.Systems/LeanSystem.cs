using EFT;
using IRLWeaponHandling.Configuration;
using IRLWeaponHandling.Core;
using UnityEngine;

namespace IRLWeaponHandling.Systems;

internal static class LeanSystem
{
	private const float Frequency = 4.5f;

	private const float DampingRatio = 0.8f;

	private static float _lean;

	private static float _velocity;

	// Procedural Q/E lean: smoothed lateral lean value (-1..1) from MovementContext.Tilt
	private static float _procLean;
	private static float _procLeanVel;

	public static float Lean => _lean;

	/// <summary>Smoothed Q/E lean value in the range -1..1 (left = -1, right = +1).</summary>
	public static float ProceduralLean => _procLean;

	public static void Tick(Player player, float deltaTime)
	{
		if (deltaTime <= 0f)
		{
			return;
		}
		deltaTime = Mathf.Min(deltaTime, 1f / 15f);

		// --- Turn/strafe lean (existing roll-based lean) ---
		float num = 0f;
		if (HandlingConfig.LeanEnabled.Value && HandlingState.HasWeapon && player != null)
		{
			float num2 = Mathf.Max(1f, HandlingConfig.LeanTurnRate.Value);
			float turn = Mathf.Clamp(MotionTracker.YawRateSlow / num2, -1.5f, 1.5f);

			float yaw = player.Rotation.x * Mathf.Deg2Rad;
			float sin = Mathf.Sin(yaw);
			float cos = Mathf.Cos(yaw);
			float localRight = player.Velocity.x * cos - player.Velocity.z * sin;

			float inputLateral = MotionTracker.InputDirection.x;
			float movementLateral = Mathf.Clamp(localRight * 0.08f, -1.5f, 1.5f) * MotionTracker.NormalizedSpeed;
			float lateral = inputLateral * 0.6f + movementLateral;

			num = Mathf.Clamp(turn + lateral, -1.5f, 1.5f) * Mathf.Clamp01(HandlingConfig.LeanMultiplier.Value);
			if (HandlingState.IsAiming)
			{
				num *= 0.55f;
			}
		}
		float num3 = 20.25f;
		float num4 = 7.2000003f;
		_velocity += ((num - _lean) * num3 - _velocity * num4) * deltaTime;
		_lean += _velocity * deltaTime;

		// --- Procedural Q/E lean (lateral body displacement + extra roll) ---
		float targetProc = 0f;
		if (HandlingConfig.ProceduralLeanEnabled.Value && HandlingState.HasWeapon && player != null)
		{
			// MotionTracker.Lean is already normalized to -1..1 from MovementContext.Tilt / 5f
			targetProc = MotionTracker.Lean;
			// Slightly reduce procedural lean while aiming so the camera doesn't
			// shift too far from the weapon sight line during ADS.
			if (HandlingState.IsAiming)
			{
				targetProc *= 0.7f;
			}
		}
		// Spring-damper for smooth procedural lean transitions
		float stiffness = Mathf.Max(1f, HandlingConfig.ProceduralLeanSmooth.Value);
		float damping = stiffness * 0.8f;
		_procLeanVel += ((targetProc - _procLean) * stiffness - _procLeanVel * damping) * deltaTime;
		_procLean += _procLeanVel * deltaTime;
		// Clamp to safe range
		_procLean = Mathf.Clamp(_procLean, -1.2f, 1.2f);
	}

	public static Vector3 Modify(Vector3 headRotation)
	{
		headRotation.z -= _lean * HandlingConfig.LeanCameraAngle.Value;
		// Add procedural Q/E lean roll on top of the turn lean
		if (HandlingConfig.ProceduralLeanEnabled.Value)
		{
			headRotation.z -= _procLean * HandlingConfig.ProceduralLeanRoll.Value;
		}
		return headRotation;
	}

	public static float WeaponRoll()
	{
		if (!HandlingConfig.LeanEnabled.Value && !HandlingConfig.ProceduralLeanEnabled.Value)
		{
			return 0f;
		}
		float roll = 0f;
		if (HandlingConfig.LeanEnabled.Value)
		{
			roll += (0f - _lean) * HandlingConfig.LeanWeaponAngle.Value;
		}
		if (HandlingConfig.ProceduralLeanEnabled.Value)
		{
			roll += (0f - _procLean) * HandlingConfig.ProceduralLeanWeaponRoll.Value;
		}
		return roll;
	}

	public static void Reset()
	{
		_lean = 0f;
		_velocity = 0f;
		_procLean = 0f;
		_procLeanVel = 0f;
	}
}

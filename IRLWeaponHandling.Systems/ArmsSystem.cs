using System;
using EFT.Animations;
using IRLWeaponHandling.Configuration;
using IRLWeaponHandling.Core;
using UnityEngine;

namespace IRLWeaponHandling.Systems;

internal static class ArmsSystem
{
	private const float MaxDeltaTime = 1f / 15f;

	private const float ReferenceRate = 180f;

	private const float MaxPosition = 0.05f;

	private const float MaxRotation = 6f;

	private static Vector3 _position;

	private static Vector3 _positionVelocity;

	private static Vector3 _rotation;

	private static Vector3 _rotationVelocity;

	private static float _aimBlend;

	private static Vector2 _smoothedInput;

	private static float _walkPhase;

	private static float _walkAmount;

	private static Vector3 _walkPosition;

	private static Vector3 _walkRotation;

	private static float _breathPhase;

	private static Vector3 _breathPosition;

	private static float _shakeTime;

	private static float _shakeAmount;

	private static Vector3 _shakePosition;

	public static Vector3 SwayRotation => _rotation;

	public static void Update(float deltaTime)
	{
		deltaTime = Mathf.Clamp(deltaTime, 0f, MaxDeltaTime);
		if (!(deltaTime <= 0f))
		{
			WeaponStats stats = HandlingState.Stats;
			_aimBlend = Mathf.Lerp(_aimBlend, HandlingState.IsAiming ? 1f : 0f, 1f - Mathf.Exp((0f - deltaTime) * 5f));
			float weight = Mathf.Clamp(HandlingConfig.ArmsWeight.Value, 0.2f, 2f);
			float num = 1f + 0.2f * (weight - 1f);
			float num2 = Mathf.Lerp(1f, 0.35f, _aimBlend);
			float num3 = Mathf.Lerp(1f, 0.5f, _aimBlend);
			float inputSmooth = Mathf.Lerp(8f, 2f, (weight - 0.2f) / 1.8f);
			_smoothedInput = Vector2.Lerp(_smoothedInput, MotionTracker.InputDirection, 1f - Mathf.Exp((0f - deltaTime) * inputSmooth));
			float num4 = Mathf.Clamp(MotionTracker.YawRate / ReferenceRate, -1.6f, 1.6f);
			float num5 = Mathf.Clamp(MotionTracker.PitchRate / ReferenceRate, -1.6f, 1.6f);
			float magnitude = Mathf.Clamp01(Mathf.Sqrt(num4 * num4 + num5 * num5));
			Vector3 position = new Vector3((0f - num4) * 0.028f, 0f - (Mathf.Abs(num4) * 0.008f + num5 * 0.005f), (0f - (Mathf.Abs(num4) * 0.012f + magnitude * 0.004f)) * num2) * num * num2;
			Vector3 rotation = new Vector3(0f - (num5 * 2.1f + magnitude * 0.75f), (0f - num4) * 1.6f, (0f - num4) * 2.6f) * num * num3;
			AddMovementLean(ref position, ref rotation);
			AddStanceLean(ref position, ref rotation);
			float tipSway = Mathf.Clamp01(HandlingConfig.DeadzoneTipSway.Value) * (1f - _aimBlend) * num;
			if (tipSway > 0.001f)
			{
				Vector2 vector = DeadzoneSystem.Offset;
				rotation.z -= vector.x * 0.1f * tipSway;
				rotation.x += vector.y * 0.08f * tipSway;
				position.z += vector.x * 0.003f * tipSway;
				position.y += vector.y * 0.0025f * tipSway;
			}
			rotation.z += (LeanSystem.WeaponRoll() + MomentumSystem.WeaponRoll()) * Mathf.Lerp(1f, 0.5f, _aimBlend);
			float frequency = Mathf.Lerp(3.0f, 1.2f, (weight - 0.2f) / 1.8f);
			Spring(ref _position, ref _positionVelocity, position, frequency, deltaTime);
			Spring(ref _rotation, ref _rotationVelocity, rotation, frequency, deltaTime);
			UpdateWalk(deltaTime);
			UpdateBreath(deltaTime);
			UpdateShake(deltaTime, stats);
		}
	}

	public static void Apply(ProceduralWeaponAnimation animation)
	{
		PlayerSpring handsContainer = animation.HandsContainer;
		if (!(handsContainer == null) && !(handsContainer.WeaponRoot == null))
		{
			Vector3 value = _position + _walkPosition + _breathPosition + _shakePosition;
			Vector3 value2 = _rotation + _walkRotation;
			handsContainer.WeaponRoot.localPosition += Clamp(value, MaxPosition);
			handsContainer.WeaponRoot.localRotation *= Quaternion.Euler(Clamp(value2, MaxRotation));
		}
	}

	public static void Reset()
	{
		_position = Vector3.zero;
		_positionVelocity = Vector3.zero;
		_rotation = Vector3.zero;
		_rotationVelocity = Vector3.zero;
		_smoothedInput = Vector2.zero;
		_walkAmount = 0f;
		_walkPosition = Vector3.zero;
		_walkRotation = Vector3.zero;
		_breathPosition = Vector3.zero;
		_shakeAmount = 0f;
		_shakePosition = Vector3.zero;
		_aimBlend = 0f;
	}

	private static void AddMovementLean(ref Vector3 position, ref Vector3 rotation)
	{
		float value = Mathf.Clamp01(HandlingConfig.ArmsWalkBob.Value);
		// Use actual movement speed, not raw input — prevents the lean
		// from playing when pressing WASD but not actually moving.
		float speedNorm = Mathf.Clamp01(MotionTracker.NormalizedSpeed);
		float num = speedNorm * value * Mathf.Lerp(1f, 0.35f, _aimBlend);
		if (num > 0.001f)
		{
			Vector2 vector = _smoothedInput.normalized;
			position.x -= vector.x * 0.008f * num;
			position.z += vector.y * 0.005f * num;
			rotation.x += vector.y * 0.7f * num;
			rotation.z -= vector.x * 1.0f * num;
		}
	}

	private static void AddStanceLean(ref Vector3 position, ref Vector3 rotation)
	{
		float lean = MotionTracker.Lean;
		if (!(Mathf.Abs(lean) < 0.001f))
		{
			position.x += lean * 0.04f;
			position.z += Mathf.Abs(lean) * 0.015f;
			rotation.y += lean * 2.0f;
			rotation.z -= lean * 3.0f;
		}
	}

	private static void UpdateWalk(float deltaTime)
	{
		float value = Mathf.Clamp01(HandlingConfig.ArmsWalkBob.Value);
		float num = Mathf.Clamp01(MotionTracker.NormalizedSpeed);
		float b = num * value * Mathf.Lerp(1f, 0.3f, _aimBlend);
		_walkAmount = Mathf.Lerp(_walkAmount, b, 1f - Mathf.Exp((0f - deltaTime) * 3.5f));
		// Only advance the walk phase when actually moving — prevents
		// the wave from running while idle, which would cause a sudden
		// jump the moment the player starts moving.
		if (_walkAmount > 0.001f)
		{
			_walkPhase += deltaTime * MathF.PI * 2f * (1f + 0.8f * num);
			if (_walkPhase > MathF.PI * 2f)
			{
				_walkPhase -= MathF.PI * 2f;
			}
		}
		_walkPosition = new Vector3(Mathf.Sin(_walkPhase) * 0.003f, Mathf.Sin(_walkPhase * 2f) * 0.0025f, 0f) * _walkAmount;
		_walkRotation = new Vector3(Mathf.Cos(_walkPhase * 2f) * 0.4f, Mathf.Sin(_walkPhase) * 0.35f, Mathf.Sin(_walkPhase) * 0.7f) * _walkAmount;
	}

	private static void UpdateBreath(float deltaTime)
	{
		float value = Mathf.Clamp01(HandlingConfig.ArmsBreathing.Value);
		if (value <= 0f)
		{
			_breathPosition = Vector3.zero;
			return;
		}
		float num = Mathf.Clamp01(1f - Mathf.Min(HandlingState.Stamina01, HandlingState.Oxygen01));
		_breathPhase += deltaTime * MathF.PI * 2f * (0.22f + 0.35f * num);
		if (_breathPhase > MathF.PI * 2f)
		{
			_breathPhase -= MathF.PI * 2f;
		}
		float num2 = Mathf.Sin(_breathPhase) - 0.25f * Mathf.Sin(_breathPhase * 2f);
		float num3 = 0.0018f * value * (1f + 1.2f * num) * Mathf.Lerp(1f, 0.6f, _aimBlend);
		_breathPosition = new Vector3(num2 * num3 * 0.35f, num2 * num3, 0f);
	}

	private static void UpdateShake(float deltaTime, WeaponStats stats)
	{
		float value = Mathf.Clamp01(HandlingConfig.ArmsShake.Value);
		if (value <= 0f)
		{
			_shakePosition = Vector3.zero;
			return;
		}
		float num = Mathf.Clamp01(HandlingState.HandsStamina01);
		float b = value * (0.0002f + 0.0006f * _aimBlend) * (1f + 0.4f * stats.WeightProgress) * (1f + (1f - num) * 1.5f);
		_shakeAmount = Mathf.Lerp(_shakeAmount, b, 1f - Mathf.Exp((0f - deltaTime) * 4f));
		_shakeTime += deltaTime * (1.3f + 0.9f * (1f - num));
		_shakePosition = new Vector3((Mathf.PerlinNoise(_shakeTime, 0.5f) - 0.5f) * 2f, (Mathf.PerlinNoise(0.5f, _shakeTime) - 0.5f) * 2f, 0f) * _shakeAmount;
	}

	private static void Spring(ref Vector3 current, ref Vector3 velocity, Vector3 target, float frequency, float deltaTime)
	{
		float num = frequency * frequency;
		float num2 = 1.4f * frequency;
		velocity += ((target - current) * num - velocity * num2) * deltaTime;
		current += velocity * deltaTime;
	}

	private static Vector3 Clamp(Vector3 value, float limit)
	{
		return new Vector3(Mathf.Clamp(value.x, 0f - limit, limit), Mathf.Clamp(value.y, 0f - limit, limit), Mathf.Clamp(value.z, 0f - limit, limit));
	}
}
using EFT;
using IRLWeaponHandling.Configuration;
using IRLWeaponHandling.Core;
using UnityEngine;

namespace IRLWeaponHandling.Systems;

internal static class MomentumSystem
{
	private const float MaxDeltaTime = 1f / 15f;

	private const float ReferenceTurnRate = 180f;

	private const float MaxHeadPitch = 2f;

	private const float MaxHeadYaw = 1.5f;

	private const float MaxHeadRoll = 3f;

	private static Vector3 _lastPosition;

	private static Vector3 _lastVelocity;

	private static float _lastYawRate;

	private static bool _hasSample;

	private static Vector3 _headRotation;

	private static Vector3 _headVelocity;

	private static float _aimBlend;

	private static Vector3 _lastMoveTarget;

	private static Vector3 _stopTarget;

	private static float _stopProgress;

	public static Vector3 Modify(Vector3 headRotation)
	{
		if (_headRotation == Vector3.zero)
		{
			return headRotation;
		}
		headRotation.x += _headRotation.x;
		headRotation.y += _headRotation.y;
		headRotation.z += _headRotation.z;
		return headRotation;
	}

	public static float WeaponRoll()
	{
		return _headRotation.z;
	}

	public static void Tick(Player player, float deltaTime)
	{
		deltaTime = Mathf.Clamp(deltaTime, 0f, MaxDeltaTime);
		if (deltaTime <= 0f || !HandlingConfig.MomentumEnabled.Value || player == null || !player.IsYourPlayer)
		{
			Reset();
			return;
		}
		_aimBlend = Mathf.Lerp(_aimBlend, HandlingState.IsAiming ? 1f : 0f, 1f - Mathf.Exp((0f - deltaTime) * 5f));
		float hipMul = 1f - _aimBlend * 0.65f;
		float amount = Mathf.Clamp01(HandlingConfig.MomentumAmount.Value);
		float weightInfluence = Mathf.Clamp01(HandlingConfig.MomentumWeight.Value);
		PhysicalBase physical = player.Physical;
		float overweight = 0f;
		float inertia = 0f;
		if (physical != null)
		{
			overweight = Mathf.Clamp01(physical.Overweight);
			inertia = Mathf.Clamp01(physical.Inertia);
		}
		float baseWeight = Mathf.Clamp01(overweight * 0.7f + inertia * 0.4f);
		float totalScale = amount * (1f + baseWeight * weightInfluence) * hipMul;
		Vector3 position = player.Position;
		Vector3 velocity = _hasSample ? ((position - _lastPosition) / deltaTime) : Vector3.zero;
		Vector3 acceleration = _hasSample ? ((velocity - _lastVelocity) / deltaTime) : Vector3.zero;
		_lastPosition = position;
		_lastVelocity = velocity;
		_hasSample = true;
		acceleration = Vector3.ClampMagnitude(acceleration, 40f);
		float yaw = player.Rotation.x * Mathf.Deg2Rad;
		float sin = Mathf.Sin(yaw);
		float cos = Mathf.Cos(yaw);
		float forward = acceleration.x * sin + acceleration.z * cos;
		float right = acceleration.x * cos - acceleration.z * sin;
		float yawRate = Mathf.Clamp(MotionTracker.YawRate / ReferenceTurnRate, -1.6f, 1.6f);
		float yawAccel = (yawRate - _lastYawRate) / deltaTime;
		_lastYawRate = yawRate;
		float turn = 0f - (yawRate * 0.8f + Mathf.Clamp(yawAccel, -25f, 25f) * 0.035f);
		turn = Mathf.Clamp(turn, -2.0f, 2.0f);
		Vector3 moveTarget = new Vector3(Mathf.Clamp((0f - forward) * 0.05f, 0f - MaxHeadPitch, MaxHeadPitch), Mathf.Clamp(right * 0.02f, 0f - MaxHeadYaw, MaxHeadYaw), Mathf.Clamp(right * 0.04f, 0f - MaxHeadRoll, MaxHeadRoll));
		Vector3 turnTarget = new Vector3(0f, 0f, Mathf.Clamp(turn, 0f - MaxHeadRoll, MaxHeadRoll));
		float walkForward = velocity.x * sin + velocity.z * cos;
		float walkRight = velocity.x * cos - velocity.z * sin;
		Vector3 walkTarget = new Vector3(Mathf.Clamp(walkForward * HandlingConfig.MomentumWalkForwardLean.Value, 0f - MaxHeadPitch, MaxHeadPitch), 0f, Mathf.Clamp(walkRight * HandlingConfig.MomentumWalkSideLean.Value, 0f - MaxHeadRoll, MaxHeadRoll));
		bool isMoving = MotionTracker.IsMoving;
		float stopDelay = HandlingConfig.MomentumStopDelay.Value;
		if (isMoving || stopDelay <= 0f)
		{
			_lastMoveTarget = moveTarget;
			_stopProgress = 0f;
			_stopTarget = moveTarget;
		}
		else
		{
			_stopProgress = Mathf.Clamp01(_stopProgress + deltaTime / Mathf.Max(0.05f, stopDelay));
			_stopTarget = Vector3.Lerp(_lastMoveTarget, moveTarget, _stopProgress);
		}
		Vector3 target = (_stopTarget + turnTarget + walkTarget) * totalScale;
		float frequency = Mathf.Lerp(4.5f, 2.5f, baseWeight);
		Spring(ref _headRotation, ref _headVelocity, target, frequency, deltaTime);
	}

	public static void Reset()
	{
		_lastPosition = Vector3.zero;
		_lastVelocity = Vector3.zero;
		_lastYawRate = 0f;
		_hasSample = false;
		_headRotation = Vector3.zero;
		_headVelocity = Vector3.zero;
		_aimBlend = 0f;
		_lastMoveTarget = Vector3.zero;
		_stopTarget = Vector3.zero;
		_stopProgress = 0f;
	}

	private static void Spring(ref Vector3 current, ref Vector3 velocity, Vector3 target, float frequency, float deltaTime)
	{
		float num = frequency * frequency;
		float num2 = 1.4f * frequency;
		velocity += ((target - current) * num - velocity * num2) * deltaTime;
		current += velocity * deltaTime;
	}
}
using EFT;
using UnityEngine;

namespace IRLWeaponHandling.Core;

internal static class MotionTracker
{
	private const float MaxDeltaTime = 1f / 15f;

	private const float MaxRate = 720f;

	private const float FastSmoothing = 14f;

	private const float SlowSmoothing = 5f;

	private static Vector2 _lastRotation;

	private static Vector3 _lastPosition;

	private static bool _hasSample;

	private static float _yawRate;

	private static float _pitchRate;

	private static float _yawRateSlow;

	public static float YawRate => _yawRate;

	public static float PitchRate => _pitchRate;

	public static float YawRateSlow => _yawRateSlow;

	public static float TurnRate => new Vector2(_yawRate, _pitchRate).magnitude;

	public static bool IsMoving { get; private set; }

	public static float NormalizedSpeed { get; private set; }

	public static Vector2 InputDirection { get; private set; }

	public static float Lean { get; private set; }

	public static void Update(Player player, float deltaTime)
	{
		deltaTime = Mathf.Clamp(deltaTime, 0.0033333334f, 1f / 15f);
		Vector2 rotation = player.Rotation;
		Vector3 position = player.Position;
		MovementContext movementContext = player.MovementContext;
		NormalizedSpeed = ((movementContext == null) ? 0f : Mathf.Clamp01(movementContext.CharacterMovementSpeed / 0.6f));
		Lean = ((movementContext == null) ? 0f : Mathf.Clamp(movementContext.Tilt / 5f, -1f, 1f));
		InputDirection = player.InputDirection;
		if (!_hasSample)
		{
			_lastRotation = rotation;
			_lastPosition = position;
			_hasSample = true;
			return;
		}
		IsMoving = InputDirection.sqrMagnitude > 0.0001f || NormalizedSpeed > 0.05f;
		_lastPosition = position;
		float num = Mathf.DeltaAngle(_lastRotation.x, rotation.x);
		float num2 = rotation.y - _lastRotation.y;
		_lastRotation = rotation;
		if (Mathf.Abs(num) > 90f)
		{
			num = 0f;
		}
		if (Mathf.Abs(num2) > 90f)
		{
			num2 = 0f;
		}
		float target = Mathf.Clamp(num / deltaTime, -720f, 720f);
		float target2 = Mathf.Clamp(num2 / deltaTime, -720f, 720f);
		_yawRate = Smooth(_yawRate, target, deltaTime, 14f);
		_pitchRate = Smooth(_pitchRate, target2, deltaTime, 14f);
		_yawRateSlow = Smooth(_yawRateSlow, target, deltaTime, 5f);
	}

	public static void Reset(Player player)
	{
		_hasSample = false;
		_yawRate = 0f;
		_pitchRate = 0f;
		_yawRateSlow = 0f;
		IsMoving = false;
		NormalizedSpeed = 0f;
		InputDirection = Vector2.zero;
		Lean = 0f;
		if (player != null)
		{
			_lastRotation = player.Rotation;
			_lastPosition = player.Position;
			_hasSample = true;
		}
	}

	private static float Smooth(float current, float target, float deltaTime, float speed)
	{
		return Mathf.Lerp(current, target, 1f - Mathf.Exp((0f - deltaTime) * speed));
	}
}

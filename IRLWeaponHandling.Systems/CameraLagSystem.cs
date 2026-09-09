using EFT.Animations;
using IRLWeaponHandling.Configuration;
using IRLWeaponHandling.Core;
using UnityEngine;

namespace IRLWeaponHandling.Systems;

internal static class CameraLagSystem
{
	private static readonly float VanillaCameraSmoothOut = 6f;

	private static Vector2 _followed;

	public static Vector2 Offset => _followed;

	public static void Tick(float deltaTime)
	{
		if (!HandlingConfig.CameraLagEnabled.Value || !HandlingState.HasWeapon || deltaTime <= 0f)
		{
			_followed = Vector2.Lerp(_followed, Vector2.zero, Mathf.Clamp01(deltaTime * 8f));
			return;
		}
		ProceduralWeaponAnimation animation = HandlingState.Animation;
		PlayerSpring handsContainer = animation.HandsContainer;
		if (!(handsContainer == null))
		{
			Vector3 zero = Vector3.zero;
			if (handsContainer.SwaySpring != null)
			{
				zero += handsContainer.SwaySpring.Value;
			}
			if (handsContainer.HandsRotation != null)
			{
				zero += handsContainer.HandsRotation.GetRelative();
			}
			Vector2 b = new Vector2(zero.y, zero.x) * HandlingConfig.CameraSwayFollow.Value;
			float t = Mathf.Clamp01(deltaTime * HandlingConfig.CameraSwayFollowSpeed.Value);
			_followed = Vector2.Lerp(_followed, b, t);
			animation.CameraSmoothOut = VanillaCameraSmoothOut * HandlingConfig.CameraSettleSmoothing.Value;
		}
	}

	public static Vector3 Modify(Vector3 headRotation)
	{
		if (_followed == Vector2.zero)
		{
			return headRotation;
		}
		headRotation.x += _followed.y;
		headRotation.y += _followed.x;
		return headRotation;
	}

	public static void Reset(ProceduralWeaponAnimation animation)
	{
		_followed = Vector2.zero;
		if (animation != null)
		{
			animation.CameraSmoothOut = VanillaCameraSmoothOut;
		}
	}
}
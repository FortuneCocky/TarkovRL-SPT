using EFT;
using EFT.Animations;
using IRLWeaponHandling.Configuration;
using UnityEngine;

namespace IRLWeaponHandling.Core;

internal static class HandlingState
{
	public static Player Player { get; private set; }

	public static Player.FirearmController Firearm { get; private set; }

	public static ProceduralWeaponAnimation Animation { get; private set; }

	public static WeaponStats Stats { get; private set; }

	public static float HandsStamina01 { get; private set; } = 1f;


	public static float Stamina01 { get; private set; } = 1f;


	public static float Oxygen01 { get; private set; } = 1f;


	public static float MovementSpeed01 { get; private set; }

	public static bool IsSprinting { get; private set; }

	public static bool HasWeapon
	{
		get
		{
			if (Firearm != null)
			{
				return Animation != null;
			}
			return false;
		}
	}

	public static Vector3 HeadRotation { get; set; }

	public static bool IsAiming
	{
		get
		{
			if (Firearm != null)
			{
				return Firearm.IsAiming;
			}
			return false;
		}
	}

	public static bool Enabled
	{
		get
		{
			if (HandlingConfig.Enabled != null)
			{
				return HandlingConfig.Enabled.Value;
			}
			return false;
		}
	}

	public static void Update(Player player)
	{
		Player = player;
		Animation = player.ProceduralWeaponAnimation;
		Firearm = player.HandsController as Player.FirearmController;
		if (Firearm == null || Firearm.Weapon == null || !player.HasFirearmInHands())
		{
			Firearm = null;
			Stats = default(WeaponStats);
		}
		else if (!Stats.Matches(Firearm))
		{
			RefreshStats();
		}
		PhysicalBase physical = player.Physical;
		if (physical != null)
		{
			HandsStamina01 = physical.HandsStamina?.NormalValue ?? 1f;
			Stamina01 = physical.Stamina?.NormalValue ?? 1f;
			Oxygen01 = physical.Oxygen?.NormalValue ?? 1f;
		}
		MovementContext movementContext = player.MovementContext;
		if (movementContext != null)
		{
			float num = Mathf.Max(movementContext.MaxSpeed, 0.1f);
			MovementSpeed01 = Mathf.Clamp01(movementContext.CharacterMovementSpeed / num);
			IsSprinting = movementContext.IsSprintEnabled;
		}
	}

	public static void RefreshStats()
	{
		if (Firearm != null && Firearm.Weapon != null)
		{
			Stats = WeaponStats.Read(Firearm);
		}
	}

	public static void Clear()
	{
		Player = null;
		Firearm = null;
		Animation = null;
		Stats = default(WeaponStats);
		HandsStamina01 = 1f;
		Stamina01 = 1f;
		Oxygen01 = 1f;
		MovementSpeed01 = 0f;
		IsSprinting = false;
		HeadRotation = Vector3.zero;
	}
}

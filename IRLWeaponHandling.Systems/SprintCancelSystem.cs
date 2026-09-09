using EFT;
using IRLWeaponHandling.Configuration;
using IRLWeaponHandling.Core;
using UnityEngine;

namespace IRLWeaponHandling.Systems;

internal static class SprintCancelSystem
{
	public static void Tick(Player player)
	{
		if (!HandlingConfig.SprintCancelAds.Value || player == null || !player.IsYourPlayer || !HandlingState.HasWeapon)
		{
			return;
		}
		MovementContext movementContext = player.MovementContext;
		if (movementContext == null || !movementContext.IsSprintEnabled)
		{
			return;
		}
		if (Input.GetMouseButtonDown(1))
		{
			movementContext.EnableSprint(false);
			Player.FirearmController firearm = HandlingState.Firearm;
			if (firearm != null)
			{
				firearm.SetAim(true);
			}
		}
	}

	public static void Reset()
	{
	}
}
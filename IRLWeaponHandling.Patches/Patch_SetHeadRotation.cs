using System.Reflection;
using EFT;
using EFT.Animations;
using IRLWeaponHandling.Core;
using IRLWeaponHandling.Systems;
using SPT.Reflection.Patching;
using UnityEngine;

namespace IRLWeaponHandling.Patches;

internal class Patch_SetHeadRotation : ModulePatch
{
	protected override MethodBase GetTargetMethod()
	{
		return typeof(ProceduralWeaponAnimation).GetMethod("SetHeadRotation", BindingFlags.Instance | BindingFlags.Public);
	}

	[PatchPrefix]
	private static void Prefix(ProceduralWeaponAnimation __instance, ref Vector3 headRot)
	{
		if (!HandlingState.Enabled)
		{
			return;
		}
		Player player = HandlingManager.LocalPlayer();
		if (player == null || player.ProceduralWeaponAnimation != __instance)
		{
			return;
		}

		// Skip all camera modifications during free look — let vanilla handle it
		if (player.MouseLookControl)
		{
			HandlingState.HeadRotation = headRot;
			return;
		}

		// Skip deadzone/camera modifications during free-look return smoothing.
		// HandlingManager.Tick() sets HandlingState.HeadRotation directly
		// during the smoothing transition.
		if (HandlingManager.IsFreeLookReturnActive)
		{
			return;
		}

		// Apply weapon deadzone (Tarkov Real Life cone-clamp) first.
		// This is a direct camera clamp that works in all states.
		headRot = WeaponDeadzoneSystem.Modify(headRot);

		// Base rotation: ADS uses DeadzoneSystem, hip uses ProceduralHipSystem.
		// Both detach the camera from the raw mouse rotation.
		Vector3 baseRot = HandlingState.IsAiming
			? DeadzoneSystem.Modify(headRot)
			: ProceduralHipSystem.Modify(headRot);

		// Apply momentum, camera lag, and lean on top of the detached camera.
		headRot = LeanSystem.Modify(CameraLagSystem.Modify(MomentumSystem.Modify(baseRot)));
		HandlingState.HeadRotation = headRot;
	}
}
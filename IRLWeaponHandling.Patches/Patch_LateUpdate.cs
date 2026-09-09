using System.Reflection;
using EFT;
using IRLWeaponHandling.Core;
using SPT.Reflection.Patching;
using UnityEngine;

namespace IRLWeaponHandling.Patches;

internal class Patch_LateUpdate : ModulePatch
{
	protected override MethodBase GetTargetMethod()
	{
		return typeof(Player).GetMethod("LateUpdate", BindingFlags.Instance | BindingFlags.Public);
	}

	[PatchPostfix]
	private static void Postfix(Player __instance)
	{
		HandlingManager.Tick(__instance, Time.deltaTime);
	}
}

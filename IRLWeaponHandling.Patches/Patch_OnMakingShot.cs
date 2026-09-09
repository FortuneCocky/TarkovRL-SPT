using System.Reflection;
using EFT;
using IRLWeaponHandling.Core;
using IRLWeaponHandling.Systems;
using SPT.Reflection.Patching;

namespace IRLWeaponHandling.Patches;

internal class Patch_OnMakingShot : ModulePatch
{
	protected override MethodBase GetTargetMethod()
	{
		return typeof(Player).GetMethod("OnMakingShot", BindingFlags.Instance | BindingFlags.Public);
	}

	[PatchPostfix]
	private static void Postfix(Player __instance)
	{
		if (__instance.IsYourPlayer && HandlingState.Enabled)
		{
			ShotSystem.OnShot();
		}
	}
}

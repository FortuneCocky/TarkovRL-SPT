using System.Reflection;
using EFT;
using EFT.Animations;
using IRLWeaponHandling.Core;
using SPT.Reflection.Patching;

namespace IRLWeaponHandling.Patches;

internal class Patch_UpdateWeaponVariables : ModulePatch
{
	protected override MethodBase GetTargetMethod()
	{
		return typeof(ProceduralWeaponAnimation).GetMethod("UpdateWeaponVariables", BindingFlags.Instance | BindingFlags.Public);
	}

	[PatchPostfix]
	private static void Postfix(ProceduralWeaponAnimation __instance)
	{
		Player player = HandlingManager.LocalPlayer();
		if (!(player == null) && !(player.ProceduralWeaponAnimation != __instance))
		{
			HandlingManager.OnWeaponVariablesUpdated(player);
		}
	}
}

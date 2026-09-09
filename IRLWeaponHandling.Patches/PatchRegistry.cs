using System;
using System.Collections.Generic;
using IRLWeaponHandling.Utilities;
using SPT.Reflection.Patching;

namespace IRLWeaponHandling.Patches;

internal static class PatchRegistry
{
	public static void EnableAll()
	{
		foreach (ModulePatch item in new List<ModulePatch>
		{
			(ModulePatch)(object)new Patch_LateUpdate(),
			(ModulePatch)(object)new Patch_LerpCamera(),
			(ModulePatch)(object)new Patch_SetHeadRotation(),
			(ModulePatch)(object)new Patch_OnMakingShot(),
			(ModulePatch)(object)new Patch_UpdateWeaponVariables(),
			(ModulePatch)(object)new Patch_CalculateCameraPosition()
		})
		{
			try
			{
				item.Enable();
				Log.Verbose("Enabled patch " + ((object)item).GetType().Name);
			}
			catch (Exception arg)
			{
				Log.Error($"Failed to enable patch {((object)item).GetType().Name}: {arg}");
			}
		}
	}
}

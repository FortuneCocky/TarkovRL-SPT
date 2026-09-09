using System.Reflection;
using EFT;
using EFT.Animations;
using HarmonyLib;
using IRLWeaponHandling.Core;
using SPT.Reflection.Patching;
using UnityEngine;

namespace IRLWeaponHandling.Patches;

internal class Patch_LerpCamera : ModulePatch
{
    private static readonly FieldInfo _headRotationVecField = AccessTools.Field(typeof(ProceduralWeaponAnimation), "_headRotationVec");

    protected override MethodBase GetTargetMethod()
    {
        return typeof(ProceduralWeaponAnimation).GetMethod("LerpCamera", BindingFlags.Instance | BindingFlags.Public);
    }

    [PatchPostfix]
    private static void Postfix(ProceduralWeaponAnimation __instance)
    {
        Player player = HandlingManager.LocalPlayer();
        if (player == null || player.ProceduralWeaponAnimation != __instance)
        {
            return;
        }

        HandlingManager.Tick(player, Time.deltaTime);

        // Skip camera override during free look — let vanilla handle it
        if (player.MouseLookControl)
        {
            return;
        }

        ApplyCamera(__instance);
    }

    internal static void ApplyCamera(ProceduralWeaponAnimation __instance)
    {
        if (__instance.HandsContainer?.CameraTransform != null)
        {
            __instance.HandsContainer.CameraTransform.localRotation = Quaternion.Euler(HandlingState.HeadRotation);
            _headRotationVecField?.SetValue(__instance, HandlingState.HeadRotation);
        }
    }
}
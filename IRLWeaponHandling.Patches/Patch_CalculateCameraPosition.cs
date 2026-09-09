using System.Reflection;
using EFT;
using EFT.Animations;
using IRLWeaponHandling.Core;
using IRLWeaponHandling.Systems;
using SPT.Reflection.Patching;
using UnityEngine;

namespace IRLWeaponHandling.Patches;

internal class Patch_CalculateCameraPosition : ModulePatch
{
    private static int _lastFrame = -1;

    protected override MethodBase GetTargetMethod()
    {
        return typeof(ProceduralWeaponAnimation).GetMethod("CalculateCameraPosition", BindingFlags.Instance | BindingFlags.Public);
    }

    [PatchPostfix]
    private static void Postfix(ProceduralWeaponAnimation __instance)
    {
        if (_lastFrame == Time.frameCount)
        {
            return;
        }
        _lastFrame = Time.frameCount;

        Player player = HandlingManager.LocalPlayer();
        if (player == null || player.ProceduralWeaponAnimation != __instance)
        {
            return;
        }

        // Skip weapon offset during free look — let vanilla handle it
        if (player.MouseLookControl)
        {
            return;
        }

        if (__instance.HandsContainer?.CameraTransform != null)
        {
            __instance.HandsContainer.CameraTransform.localRotation = Quaternion.Euler(HandlingState.HeadRotation);
        }

        // Apply the procedural weapon offset (weapon rotation relative to camera).
        // In hip fire: uses ProceduralHipSystem.WeaponOffset.
        // In ADS: uses DeadzoneSystem.WeaponOffset so the weapon visibly moves
        //         inside the ADS deadzone cone while the camera stays locked.
        // Skip when in left stance (shoulder swap) so the weapon stays attached
        // to the hands instead of floating off due to the deadzone offset.
        if (__instance.HandsContainer?.WeaponRoot != null)
        {
            bool isLeftStance = false;
            try
            {
                isLeftStance = player.MovementContext?.LeftStanceController?.LeftStance ?? false;
            }
            catch { }

            if (!isLeftStance)
            {
                if (HandlingState.IsAiming)
                {
                    __instance.HandsContainer.WeaponRoot.localRotation = DeadzoneSystem.WeaponOffset * __instance.HandsContainer.WeaponRoot.localRotation;
                }
                else
                {
                    __instance.HandsContainer.WeaponRoot.localRotation = ProceduralHipSystem.WeaponOffset * SwaySystem.ProceduralSwayOffset * __instance.HandsContainer.WeaponRoot.localRotation;
                }
            }
        }
    }
}
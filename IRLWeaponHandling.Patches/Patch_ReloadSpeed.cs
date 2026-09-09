using System.Reflection;
using EFT;
using HarmonyLib;
using IRLWeaponHandling.Configuration;
using IRLWeaponHandling.Core;
using SPT.Reflection.Patching;
using UnityEngine;

namespace IRLWeaponHandling.Patches;

/// <summary>
/// Modifies reload speed based on weapon ergonomics (efficiency) and applies
/// a penalty when reloading while sprinting.
/// </summary>
internal class Patch_ReloadSpeed : ModulePatch
{
    private static FieldInfo _buffInfoField;
    private static FieldInfo _reloadSpeedField;
    private static FieldInfo _playerField;

    protected override MethodBase GetTargetMethod()
    {
        _buffInfoField = AccessTools.Field(typeof(Player.FirearmController), "_buffInfo");
        _playerField = AccessTools.Field(typeof(Player.FirearmController), "_player");

        // WeaponBuffsInfo is a nested type in SkillManager
        if (_buffInfoField != null)
        {
            var buffType = _buffInfoField.FieldType;
            _reloadSpeedField = AccessTools.Field(buffType, "ReloadSpeed");
        }

        return typeof(Player.FirearmController).GetMethod("SetAnimatorAndProceduralValues", BindingFlags.Instance | BindingFlags.Public);
    }

    [PatchPrefix]
    private static void Prefix(Player.FirearmController __instance)
    {
        if (!HandlingConfig.Enabled.Value || !HandlingConfig.ReloadEfficiencyEnabled.Value)
        {
            return;
        }

        if (_buffInfoField == null || _reloadSpeedField == null || _playerField == null)
        {
            return;
        }

        object buffInfo = _buffInfoField.GetValue(__instance);
        if (buffInfo == null)
        {
            return;
        }

        Player player = _playerField.GetValue(__instance) as Player;
        if (player == null || !player.IsYourPlayer)
        {
            return;
        }

        float baseReloadSpeed = (float)_reloadSpeedField.GetValue(buffInfo);
        float modifiedSpeed = baseReloadSpeed;

        // Scale by ergonomics (efficiency). Higher ergonomics = faster reload.
        // TotalErgonomics typically ranges 0-100. We map it to a multiplier.
        float ergo = __instance.TotalErgonomics;
        float ergoMultiplier = 1f + (ergo / 100f) * HandlingConfig.ReloadEfficiencyInfluence.Value;
        modifiedSpeed *= ergoMultiplier;

        // Sprint penalty: reloading while sprinting is slower.
        bool isSprinting = player.MovementContext?.IsSprintEnabled ?? false;
        if (isSprinting)
        {
            float sprintPenalty = Mathf.Clamp01(HandlingConfig.ReloadSprintPenalty.Value);
            modifiedSpeed *= (1f - sprintPenalty);
        }

        _reloadSpeedField.SetValue(buffInfo, modifiedSpeed);
    }
}
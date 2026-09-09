using System.Collections.Generic;
using Comfort.Common;
using EFT;
using EFT.HealthSystem;
using IRLWeaponHandling.Configuration;
using IRLWeaponHandling.Core;
using UnityEngine;

namespace IRLWeaponHandling.Systems;

/// <summary>
/// Realistic leg crippling for both players and bots.
///
/// When leg health drops below the crouch threshold:
/// - The actor is forced into crouch position
/// - Sprinting is disabled
///
/// When both legs are blacked out (0 HP):
/// - The actor is forced into prone (crawl) position
/// - Movement speed is heavily reduced
/// - Sprinting is disabled
/// - The actor cannot stand until at least one leg is healed
///
/// Painkillers temporarily mask the damage.
/// </summary>
internal static class LegCrippleSystem
{
    // Per-actor state — keyed by player instance
    private static readonly Dictionary<Player, CrippleState> _states = new();

    private class CrippleState
    {
        public bool WasCrouching;
        public bool WasForcedProne;
    }

    /// <summary>
    /// Tick the leg cripple system. Called every frame from HandlingManager.
    /// Processes the local player and all bots if enabled.
    /// </summary>
    public static void Tick(Player player, float dt)
    {
        if (!HandlingConfig.LegCrippleEnabled.Value)
        {
            // Restore everyone when disabled
            if (_states.Count > 0)
            {
                foreach (var kvp in _states)
                    RestoreMovement(kvp.Key, kvp.Value);
                _states.Clear();
            }
            return;
        }

        // Process the local player
        if (HandlingConfig.LegCrippleEnablePlayer.Value)
        {
            ProcessPlayer(player, dt);
        }

        // Process all bots
        if (HandlingConfig.LegCrippleEnableBots.Value)
        {
            GameWorld gw = Singleton<GameWorld>.Instance;
            if (gw != null)
            {
                var allPlayers = gw.AllAlivePlayersList;
                if (allPlayers != null)
                {
                    foreach (Player p in allPlayers)
                    {
                        if (p == null || p == player) continue;
                        if (p.IsYourPlayer) continue;
                        ProcessPlayer(p, dt);
                    }
                }
            }
        }

        // Clean up dead/removed actors
        if (_states.Count > 0)
        {
            var toRemove = new List<Player>();
            foreach (var kvp in _states)
            {
                if (kvp.Key == null || kvp.Key.HealthController == null || !kvp.Key.HealthController.IsAlive)
                {
                    toRemove.Add(kvp.Key);
                }
            }
            foreach (var p in toRemove)
            {
                _states.Remove(p);
            }
        }
    }

    private static void ProcessPlayer(Player player, float dt)
    {
        if (player == null) return;
        if (player.HealthController == null || !player.HealthController.IsAlive) return;
        if (player.MovementContext == null) return;

        if (!_states.TryGetValue(player, out var state))
        {
            state = new CrippleState();
            _states[player] = state;
        }

        bool leftLegBlacked = IsBodyPartDestroyed(player, EBodyPart.LeftLeg);
        bool rightLegBlacked = IsBodyPartDestroyed(player, EBodyPart.RightLeg);
        bool bothLegsBlacked = leftLegBlacked && rightLegBlacked;

        bool onPainkillers = IsOnPainkillers(player);

        if (onPainkillers)
        {
            // Painkillers mask everything — restore normal movement
            if (state.WasCrouching || state.WasForcedProne)
            {
                RestoreMovement(player, state);
            }
            return;
        }

        if (bothLegsBlacked)
        {
            // Both legs blacked — force prone/crawl
            if (!state.WasForcedProne)
            {
                ForceProne(player);
                state.WasForcedProne = true;
                state.WasCrouching = false;
            }
            else
            {
                MaintainProne(player);
            }
        }
        else if (leftLegBlacked || rightLegBlacked)
        {
            // One leg blacked — force crouch
            if (state.WasForcedProne)
            {
                // Was prone, now only one leg blacked — allow standing up
                state.WasForcedProne = false;
            }
            if (!state.WasCrouching)
            {
                ForceCrouch(player);
                state.WasCrouching = true;
            }
            else
            {
                MaintainCrouch(player);
            }
        }
        else
        {
            // No legs blacked — check low health crouch
            float threshold = HandlingConfig.LegCrippleCrouchHealthThreshold.Value;
            bool leftLowHealth = GetBodyPartHealthFraction(player, EBodyPart.LeftLeg) < threshold;
            bool rightLowHealth = GetBodyPartHealthFraction(player, EBodyPart.RightLeg) < threshold;

            if (leftLowHealth && rightLowHealth)
            {
                // Both legs low health — force crouch
                if (state.WasForcedProne)
                {
                    state.WasForcedProne = false;
                }
                if (!state.WasCrouching)
                {
                    ForceCrouch(player);
                    state.WasCrouching = true;
                }
                else
                {
                    MaintainCrouch(player);
                }
            }
            else
            {
                // Legs are fine — restore normal movement
                if (state.WasCrouching || state.WasForcedProne)
                {
                    RestoreMovement(player, state);
                }
            }
        }
    }

    /// <summary>
    /// Force the actor into crouch position.
    /// </summary>
    private static void ForceCrouch(Player player)
    {
        MovementContext mc = player.MovementContext;
        if (mc == null) return;

        try
        {
            // Set pose to crouch level (~0.5)
            mc.SetPoseLevel(0.5f, true);
        }
        catch { }

        try
        {
            mc.EnableSprint(false);
        }
        catch { }
    }

    private static void MaintainCrouch(Player player)
    {
        MovementContext mc = player.MovementContext;
        if (mc == null) return;

        try
        {
            // Keep in crouch if trying to stand
            if (mc.PoseLevel > 0.6f)
            {
                mc.SetPoseLevel(0.5f, true);
            }
        }
        catch { }

        try
        {
            if (mc.IsSprintEnabled)
            {
                mc.EnableSprint(false);
            }
        }
        catch { }
    }

    /// <summary>
    /// Force the actor into prone position and limit movement.
    /// </summary>
    private static void ForceProne(Player player)
    {
        MovementContext mc = player.MovementContext;
        if (mc == null) return;

        try
        {
            mc.IsInPronePose = true;
        }
        catch { }

        try
        {
            mc.EnableSprint(false);
        }
        catch { }

        try
        {
            float crawlSpeed = HandlingConfig.LegCrippleCrawlSpeed.Value;
            mc.SetCharacterMovementSpeed(crawlSpeed, true);
        }
        catch { }
    }

    /// <summary>
    /// Keep the actor in prone position while both legs are blacked.
    /// </summary>
    private static void MaintainProne(Player player)
    {
        MovementContext mc = player.MovementContext;
        if (mc == null) return;

        try
        {
            if (!mc.IsInPronePose)
            {
                mc.IsInPronePose = true;
            }
        }
        catch { }

        try
        {
            if (mc.IsSprintEnabled)
            {
                mc.EnableSprint(false);
            }
        }
        catch { }

        try
        {
            float crawlSpeed = HandlingConfig.LegCrippleCrawlSpeed.Value;
            if (mc.CharacterMovementSpeed > crawlSpeed * 1.1f)
            {
                mc.SetCharacterMovementSpeed(crawlSpeed, true);
            }
        }
        catch { }
    }

    /// <summary>
    /// Restore normal movement when legs are healed or painkillers are active.
    /// </summary>
    private static void RestoreMovement(Player player, CrippleState state)
    {
        // Don't force-stand — let the actor stand up naturally.
        // Just stop forcing crouch/prone and let vanilla take over.
        state.WasCrouching = false;
        state.WasForcedProne = false;
    }

    /// <summary>
    /// Check if a body part is destroyed (blacked out).
    /// </summary>
    private static bool IsBodyPartDestroyed(Player player, EBodyPart part)
    {
        try
        {
            var hc = player.HealthController as ActiveHealthController;
            if (hc != null)
            {
                return hc.IsBodyPartDestroyed(part);
            }
        }
        catch { }
        return false;
    }

    /// <summary>
    /// Get body part health as a fraction of max (0 = blacked, 1 = full).
    /// </summary>
    private static float GetBodyPartHealthFraction(Player player, EBodyPart part)
    {
        try
        {
            var hc = player.HealthController as ActiveHealthController;
            if (hc != null)
            {
                float current = hc.GetBodyPartHealth(part).Current;
                float max = hc.GetBodyPartHealth(part).Maximum;
                if (max > 0f)
                    return Mathf.Clamp01(current / max);
            }
        }
        catch { }
        return 1f;
    }

    /// <summary>
    /// Check if the player is on painkillers.
    /// </summary>
    private static bool IsOnPainkillers(Player player)
    {
        try
        {
            return player.MovementContext.PhysicalConditionIs(EPhysicalCondition.OnPainkillers);
        }
        catch { }
        return false;
    }

    public static void Reset()
    {
        _states.Clear();
    }
}

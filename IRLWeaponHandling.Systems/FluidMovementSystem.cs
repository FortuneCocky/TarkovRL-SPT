using EFT;
using IRLWeaponHandling.Configuration;
using IRLWeaponHandling.Core;
using UnityEngine;

namespace IRLWeaponHandling.Systems;

/// <summary>
/// Fluid player movement system. Adds natural, organic motion on top of
/// the existing momentum/lean/camera-lag systems:
///
///  - Walk bob: subtle vertical and horizontal camera bob synced to
///    movement speed, simulating footsteps and body weight shifting.
///  - Landing impact: camera dips down when the player lands from a fall,
///    with intensity proportional to fall speed/duration.
///  - Acceleration smoothing: the bob amplitude smoothly ramps up/down
///    with movement speed so starting and stopping feel fluid rather than
///    instant.
///  - Stride roll: a tiny camera roll synced to the bob cycle, so the
///    body sways side-to-side naturally while walking/running.
///  - Crouch dip: a small camera lower when entering crouch, smoothed.
///
/// All effects are additive on the head rotation (pitch/yaw/roll) and
/// fully configurable. They are reduced while aiming for stability.
/// </summary>
internal static class FluidMovementSystem
{
    private const float MaxDeltaTime = 1f / 15f;

    // Bob cycle state
    private static float _bobPhase;
    private static float _bobAmplitude;
    private static float _bobAmplitudeVel;

    // Landing impact state (spring)
    private static float _landingDip;
    private static float _landingDipVel;

    // Crouch smoothing
    private static float _crouchBlend;
    private static float _crouchBlendVel;

    // Previous grounded state for landing detection
    private static bool _wasGrounded;
    private static float _prevFreefallTime;

    // Current smoothed speed (for acceleration smoothing)
    private static float _smoothedSpeed;
    private static float _smoothedSpeedVel;

    // Aim blend — reduces all effects while aiming
    private static float _aimBlend;

    /// <summary>
    /// Apply the fluid movement offsets to the head rotation.
    /// Called from Patch_SetHeadRotation after all other systems.
    /// </summary>
    public static Vector3 Modify(Vector3 headRotation)
    {
        if (!HandlingConfig.FluidMovementEnabled.Value)
            return headRotation;

        // Pitch: walk bob (vertical) + landing dip (downward)
        headRotation.x += _bobAmplitude * _bobPitchCurrent + _landingDip + _crouchBlend;

        // Yaw: walk bob (horizontal sway)
        headRotation.y += _bobAmplitude * _bobYawCurrent;

        // Roll: stride roll (side-to-side body sway)
        headRotation.z += _bobAmplitude * _bobRollCurrent;

        return headRotation;
    }

    // Current-cycle bob values (computed in Tick, applied in Modify)
    private static float _bobPitchCurrent;
    private static float _bobYawCurrent;
    private static float _bobRollCurrent;

    public static void Tick(Player player, float deltaTime)
    {
        deltaTime = Mathf.Clamp(deltaTime, 0f, MaxDeltaTime);
        if (deltaTime <= 0f || player == null || !player.IsYourPlayer)
        {
            Reset();
            return;
        }

        if (!HandlingConfig.FluidMovementEnabled.Value)
        {
            Reset();
            return;
        }

        // Aim blend — smoothly reduce effects while aiming
        float aimTarget = HandlingState.IsAiming ? 1f : 0f;
        _aimBlend = Mathf.Lerp(_aimBlend, aimTarget, 1f - Mathf.Exp(-deltaTime * 6f));
        float aimReduce = 1f - _aimBlend * Mathf.Clamp01(HandlingConfig.FluidAimReduction.Value);

        MovementContext mc = player.MovementContext;
        if (mc == null)
        {
            Reset();
            return;
        }

        // --- Movement speed smoothing (acceleration/deceleration) ---
        float rawSpeed = mc.CharacterMovementSpeed;
        float maxSpeed = Mathf.Max(mc.MaxSpeed, 0.1f);
        float speedNorm = Mathf.Clamp01(rawSpeed / maxSpeed);

        // Dead zone: ignore tiny idle drift so the bob doesn't play
        // when the player is standing still.
        if (speedNorm < 0.05f)
            speedNorm = 0f;

        // Smooth the speed so bob ramps up/down fluidly
        float speedSmooth = Mathf.Max(0.5f, HandlingConfig.FluidAccelSmooth.Value);
        Spring(ref _smoothedSpeed, ref _smoothedSpeedVel, speedNorm, speedSmooth, deltaTime);

        // --- Walk bob ---
        // Bob amplitude smoothly follows smoothed speed
        const float BobAmount = 1f;
        float bobAmount = BobAmount * aimReduce;
        float targetAmp = _smoothedSpeed * bobAmount;
        float ampSmooth = Mathf.Max(1f, HandlingConfig.FluidBobRampSpeed.Value);
        Spring(ref _bobAmplitude, ref _bobAmplitudeVel, targetAmp, ampSmooth, deltaTime);

        // Only advance the bob phase when there is meaningful amplitude.
        // This prevents the wave from "running" while idle, which would
        // cause a sudden jump the moment the player starts moving.
        if (_bobAmplitude > 0.001f)
        {
            float baseFreq = 6f;
            float sprintFreqMul = HandlingState.IsSprinting ? 1.4f : 1f;
            // Frequency scales from baseFreq at full speed down to ~0.7x
            // at low speed — no constant base term, so it stops with the player.
            float freq = baseFreq * sprintFreqMul * (0.7f + _smoothedSpeed * 0.5f);

            _bobPhase += freq * deltaTime;
            if (_bobPhase > Mathf.PI * 2f)
                _bobPhase -= Mathf.PI * 2f;
        }

        // Compute bob wave values
        // Vertical bob: sine wave (up/down)
        // Horizontal sway: cosine wave (left/right)
        // Roll: cosine wave synced to horizontal sway
        float pitchScale = HandlingConfig.FluidBobPitch.Value;
        float yawScale = HandlingConfig.FluidBobYaw.Value;
        float rollScale = HandlingConfig.FluidBobRoll.Value;

        _bobPitchCurrent = Mathf.Sin(_bobPhase * 2f) * pitchScale;
        _bobYawCurrent = Mathf.Cos(_bobPhase) * yawScale;
        _bobRollCurrent = Mathf.Cos(_bobPhase) * rollScale;

        // --- Landing impact ---
        bool isGrounded = mc.IsGrounded;
        float freefallTime = mc.FreefallTime;

        if (isGrounded && !_wasGrounded && _prevFreefallTime > 0.15f)
        {
            // Just landed — compute impact strength from freefall duration
            float impactStrength = Mathf.Clamp01(_prevFreefallTime / 1.5f);
            float dipAmount = impactStrength * Mathf.Clamp01(HandlingConfig.FluidLandingDip.Value) * aimReduce;
            // Apply impulse to the landing dip spring
            _landingDipVel -= dipAmount * 8f;
        }

        _wasGrounded = isGrounded;
        _prevFreefallTime = isGrounded ? 0f : freefallTime;

        // Spring the landing dip back to zero
        float landingFreq = Mathf.Max(2f, HandlingConfig.FluidLandingRecovery.Value);
        Spring(ref _landingDip, ref _landingDipVel, 0f, landingFreq, deltaTime);

        // --- Crouch dip ---
        // PoseLevel: 1 = standing, ~0.5 = crouch, 0 = prone.
        // We detect crouch by checking if pose level is below 0.75.
        float poseLevel = 1f;
        try
        {
            poseLevel = mc.PoseLevel;
        }
        catch { }

        // Crouch factor: 0 when standing (poseLevel >= 0.75), 1 when fully crouched (poseLevel <= 0.4)
        float crouchFactor = Mathf.InverseLerp(0.75f, 0.4f, poseLevel);
        float crouchTarget = crouchFactor * Mathf.Clamp01(HandlingConfig.FluidCrouchDip.Value) * aimReduce;
        float crouchFreq = Mathf.Max(1f, HandlingConfig.FluidCrouchSpeed.Value);
        Spring(ref _crouchBlend, ref _crouchBlendVel, crouchTarget, crouchFreq, deltaTime);
    }

    public static void Reset()
    {
        _bobPhase = 0f;
        _bobAmplitude = 0f;
        _bobAmplitudeVel = 0f;
        _landingDip = 0f;
        _landingDipVel = 0f;
        _crouchBlend = 0f;
        _crouchBlendVel = 0f;
        _wasGrounded = true;
        _prevFreefallTime = 0f;
        _smoothedSpeed = 0f;
        _smoothedSpeedVel = 0f;
        _aimBlend = 0f;
        _bobPitchCurrent = 0f;
        _bobYawCurrent = 0f;
        _bobRollCurrent = 0f;
    }

    /// <summary>
    /// Critically-damped spring for smooth motion.
    /// </summary>
    private static void Spring(ref float current, ref float velocity, float target, float frequency, float deltaTime)
    {
        float k = frequency * frequency;
        float d = 1.4f * frequency;
        velocity += ((target - current) * k - velocity * d) * deltaTime;
        current += velocity * deltaTime;
    }
}

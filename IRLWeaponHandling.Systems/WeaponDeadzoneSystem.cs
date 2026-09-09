using IRLWeaponHandling.Configuration;
using IRLWeaponHandling.Core;
using UnityEngine;

namespace IRLWeaponHandling.Systems;

/// <summary>
/// Weapon deadzone ported from Tarkov Real Life (TarkovIRL).
/// Maintains a persistent camera rotation that stays locked inside a cone.
/// The camera only follows the mouse once the desired rotation exceeds
/// the deadzone angle. This is a simpler, direct cone-clamp approach that
/// works in all states (hip and ADS), complementing or replacing the
/// existing procedural deadzone systems.
/// </summary>
internal static class WeaponDeadzoneSystem
{
    private static Quaternion _current = Quaternion.identity;
    private static bool _initialized;

    // Sprint blend: 0 = full deadzone, 1 = full sprint (no deadzone).
    // Smoothly transitions so the camera doesn't snap when entering/leaving sprint.
    private static float _sprintBlend;
    private static float _sprintVelocity;

    /// <summary>
    /// Apply the weapon deadzone cone-clamp to the raw head rotation.
    /// Returns the clamped rotation, or the input if inactive.
    /// </summary>
    public static Vector3 Modify(Vector3 headRot)
    {
        if (!HandlingConfig.WeaponDeadzoneEnabled.Value || !HandlingState.HasWeapon)
        {
            _initialized = false;
            _sprintBlend = 0f;
            _sprintVelocity = 0f;
            return headRot;
        }

        // Smoothly blend sprint state so the deadzone fades out during sprint
        // and re-engages smoothly when sprint ends. This prevents the camera
        // from snapping to a stale _current position after sprinting.
        float sprintTarget = HandlingState.IsSprinting ? 1f : 0f;
        float dt = Mathf.Min(Time.unscaledDeltaTime, 0.05f);
        SpringSprint(ref _sprintBlend, ref _sprintVelocity, sprintTarget,
            HandlingConfig.SprintTransitionSpeed.Value, dt);

        // During full sprint, don't clamp — let vanilla handle the camera.
        // Also reset _initialized so when sprint ends, the cone re-initializes
        // to the current head rotation instead of using a stale _current.
        if (_sprintBlend > 0.95f)
        {
            _initialized = false;
            return headRot;
        }

        Quaternion desired = Quaternion.Euler(headRot);
        if (!_initialized)
        {
            _current = desired;
            _initialized = true;
            return headRot;
        }

        float deadzone = Mathf.Max(0.001f, HandlingConfig.WeaponDeadzoneMulti.Value * 10f);
        float speed = HandlingConfig.WeaponDeadzoneFollowSpeed.Value;
        float angle = Quaternion.Angle(_current, desired);

        if (angle > deadzone)
        {
            // Clamp the desired rotation to the cone boundary
            float t = deadzone / angle;
            Quaternion boundary = Quaternion.Slerp(desired, _current, t);

            // During hard turns (large angle), snap faster so the camera
            // keeps up with the mouse instead of slowly Slerping.
            float catchUpBoost = Mathf.Clamp01((angle - deadzone) / 30f);
            float effectiveSpeed = speed * (1f + catchUpBoost * 3f);
            _current = Quaternion.Slerp(_current, boundary, effectiveSpeed * dt);

            // Hard snap: if the angle is way beyond the cone, don't Slerp at all
            float maxAngle = deadzone * 2.5f;
            if (angle > maxAngle)
            {
                float snapT = (angle - maxAngle) / Mathf.Max(angle, 0.001f);
                _current = Quaternion.Slerp(_current, boundary, snapT);
            }
        }

        // Blend between the clamped camera and the raw head rotation based on
        // sprint blend. Only blend yaw and pitch — never roll — to prevent
        // the camera from rolling during the sprint transition.
        Vector3 clamped = _current.eulerAngles;
        Vector3 result = headRot;
        result.x = Mathf.LerpAngle(clamped.x, headRot.x, _sprintBlend);
        result.y = Mathf.LerpAngle(clamped.y, headRot.y, _sprintBlend);
        return result;
    }

    /// <summary>
    /// Reset the persistent camera state. Called when leaving a raid,
    /// losing the weapon, or when the system is disabled.
    /// </summary>
    public static void Reset()
    {
        _current = Quaternion.identity;
        _initialized = false;
        _sprintBlend = 0f;
        _sprintVelocity = 0f;
    }

    private static void SpringSprint(ref float current, ref float velocity, float target, float freq, float dt)
    {
        float k = freq * freq;
        float d = 1.4f * freq;
        velocity += ((target - current) * k - velocity * d) * dt;
        current += velocity * dt;
        current = Mathf.Clamp01(current);
    }
}

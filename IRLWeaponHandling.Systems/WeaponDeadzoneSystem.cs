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

    /// <summary>
    /// Apply the weapon deadzone cone-clamp to the raw head rotation.
    /// Returns the clamped rotation, or the input if inactive.
    /// </summary>
    public static Vector3 Modify(Vector3 headRot)
    {
        if (!HandlingConfig.WeaponDeadzoneEnabled.Value || !HandlingState.HasWeapon)
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
        float dt = Mathf.Min(Time.unscaledDeltaTime, 0.05f);
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

        return _current.eulerAngles;
    }

    /// <summary>
    /// Reset the persistent camera state. Called when leaving a raid,
    /// losing the weapon, or when the system is disabled.
    /// </summary>
    public static void Reset()
    {
        _current = Quaternion.identity;
        _initialized = false;
    }
}
